using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Placed by ElectFollower when an enemy gets close.
// Polls with OverlapSphere every 0.1s instead of using OnTriggerEnter — more reliable
// since enemies move via transform.position and may not fire trigger callbacks.
// On trigger: shocks all enemies in _damageRadius, stuns them yellow for _stunDuration,
// deals 1 damage, then destroys itself.
// Disappears silently after _lifetime seconds if nothing triggers it.
public class ElecTrap : MonoBehaviour
{
    [Header("Radii")]
    [Tooltip("How close an enemy must be to set off the trap.")]
    [SerializeField] private float _triggerRadius = 1.5f;

    [Tooltip("Radius of the shock area — wider than the trigger to catch nearby enemies too.")]
    [SerializeField] private float _damageRadius = 3f;

    [Header("Trap Settings")]
    [Tooltip("Seconds before the trap disappears on its own if never triggered.")]
    [SerializeField] private float _lifetime = 10f;

    [Tooltip("Damage dealt to each enemy in the shock radius.")]
    [SerializeField] private int _damage = 1;

    [Tooltip("Seconds enemies are stunned and flashing yellow after the shock.")]
    [SerializeField] private float _stunDuration = 1f;

    [Tooltip("How fast enemies flash during the stun — lower = faster flashing.")]
    [SerializeField] private float _flashInterval = 0.1f;

    [SerializeField] private GameObject _electricityParticlePrefab;
    [Tooltip("Fallback destroy time for the particle if it has no ParticleSystem or uses a curve lifetime.")]
    [SerializeField] private float _fallbackParticleDestroyDelay = 5f;

    [Header("Audio")]
    [SerializeField] private AudioClip _zapSound;

    private ElectFollower _owner;
    private bool _triggered;
    private Material _stunMaterial;

    // Internal helper to track stun state per enemy
    private class StunData
    {
        public Renderer[] Renderers;
        public Material[][] OriginalMaterials;
        public Material[][] YellowMaterials;
        public MonoBehaviour Movement;
        public Animator Animator;
        public HitFlash[] HitFlashes;
        public float OriginalAnimatorSpeed;
    }

    public void Initialize(ElectFollower owner)
    {
        _owner = owner;
        _stunMaterial = new Material(Shader.Find("Unlit/Color")) { color = new Color(1f, 0.85f, 0f) };
        StartCoroutine(LifetimeRoutine());
        StartCoroutine(ScanForEnemies());
    }

    private void OnDestroy()
    {
        if (_stunMaterial != null) Destroy(_stunMaterial);
    }

    private IEnumerator LifetimeRoutine()
    {
        yield return new WaitForSeconds(_lifetime);

        if (!_triggered)
        {
            _owner?.OnTrapDeactivated(wasTriggered: false);
            Destroy(gameObject);
        }
    }

    // Polls for enemies every 0.1s — avoids relying on OnTriggerEnter which needs Rigidbodies
    private IEnumerator ScanForEnemies()
    {
        WaitForSeconds scanInterval = new WaitForSeconds(0.1f);

        while (!_triggered)
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, _triggerRadius);
            foreach (Collider hit in hits)
            {
                if (hit.CompareTag("Enemy"))
                {
                    StartCoroutine(TriggerRoutine());
                    yield break;
                }
            }
            yield return scanInterval;
        }
    }

    private IEnumerator TriggerRoutine()
    {
        _triggered = true;

        if (_zapSound != null)
            AudioSource.PlayClipAtPoint(_zapSound, transform.position);

        // Notify owner right away so the post-trigger cooldown starts
        _owner?.OnTrapDeactivated(wasTriggered: true);

        // Disappear immediately visually and physically, like a bomb
        if (TryGetComponent<Renderer>(out var r)) r.enabled = false;
        if (TryGetComponent<Collider>(out var c)) c.enabled = false;
        foreach (Transform child in transform) child.gameObject.SetActive(false);

        // Spawn electricity particle
        if (_electricityParticlePrefab != null)
        {
            GameObject fx = Instantiate(_electricityParticlePrefab, transform.position, Quaternion.identity);
            
            // Force play all particle systems in the prefab (including children)
            foreach (var ps in fx.GetComponentsInChildren<ParticleSystem>())
            {
                ps.Play(true);
            }

            float destroyDelay = _fallbackParticleDestroyDelay;
            ParticleSystem mainPs = fx.GetComponent<ParticleSystem>();
            if (mainPs != null)
                destroyDelay = mainPs.main.duration + mainPs.main.startLifetime.constantMax;
            Destroy(fx, destroyDelay);
        }

        // Find all enemies in the wider damage radius
        Collider[] hits = Physics.OverlapSphere(transform.position, _damageRadius);
        HashSet<IDamageable> damaged = new HashSet<IDamageable>();
        List<StunData> stunTargets = new List<StunData>();

        foreach (Collider hit in hits)
        {
            if (!hit.CompareTag("Enemy")) continue;

            GameObject root = hit.transform.root.gameObject;
            
            // 1. Handle HitFlashes BEFORE damage to prevent white flashes
            HitFlash[] hfs = root.GetComponentsInChildren<HitFlash>();
            foreach (var hf in hfs)
            {
                // If HitFlash was already flashing, StopAllCoroutines() leaves it white.
                // We MUST disable it so it doesn't fight our stun flash.
                hf.StopAllCoroutines();
                hf.enabled = false;
            }

            // Damage — HashSet deduplicates enemies with multiple colliders
            IDamageable damageable = hit.GetComponentInParent<IDamageable>();
            if (damageable != null && damaged.Add(damageable))
                damageable.TakeDamage(_damage);

            // 2. Find and disable movement/attack component
            MonoBehaviour movement = (MonoBehaviour)root.GetComponentInChildren<EnemyMovement>()
                ?? (MonoBehaviour)root.GetComponentInChildren<TailChaserMovement>()
                ?? (MonoBehaviour)root.GetComponentInChildren<HackerMovement>();

            if (movement == null) continue; // Not an active enemy type we can stun
            movement.enabled = false;

            // Pause animations
            Animator anim = root.GetComponentInChildren<Animator>();
            float oldSpeed = 1f;
            if (anim != null)
            {
                oldSpeed = anim.speed;
                anim.speed = 0f;
            }

            // Capture renderers and materials
            Renderer[] enemyRenderers = root.GetComponentsInChildren<Renderer>();
            Material[][] originalMats = new Material[enemyRenderers.Length][];
            Material[][] yellowMats   = new Material[enemyRenderers.Length][];
            
            for (int i = 0; i < enemyRenderers.Length; i++)
            {
                // We use sharedMaterials to get the "true" base materials.
                // Re-assigning them clears any existing instance materials (like the white one).
                originalMats[i] = enemyRenderers[i].sharedMaterials;
                enemyRenderers[i].sharedMaterials = originalMats[i]; 

                yellowMats[i] = new Material[originalMats[i].Length];
                for (int j = 0; j < yellowMats[i].Length; j++) 
                    yellowMats[i][j] = _stunMaterial;
            }

            stunTargets.Add(new StunData {
                Renderers = enemyRenderers,
                OriginalMaterials = originalMats,
                YellowMaterials = yellowMats,
                Movement = movement,
                Animator = anim,
                HitFlashes = hfs,
                OriginalAnimatorSpeed = oldSpeed
            });
        }

        // Flash loop
        float elapsed = 0f;
        bool showingYellow = false;
        WaitForSeconds flashWait = new WaitForSeconds(_flashInterval);

        while (elapsed < _stunDuration)
        {
            showingYellow = !showingYellow;
            foreach (var target in stunTargets)
            {
                for (int i = 0; i < target.Renderers.Length; i++)
                {
                    if (target.Renderers[i] == null) continue;
                    target.Renderers[i].sharedMaterials = showingYellow ? target.YellowMaterials[i] : target.OriginalMaterials[i];
                }
            }
            yield return flashWait;
            elapsed += _flashInterval;
        }

        // Restore everything
        foreach (var target in stunTargets)
        {
            if (target.Movement != null) target.Movement.enabled = true;
            if (target.Animator != null) target.Animator.speed = target.OriginalAnimatorSpeed;

            for (int i = 0; i < target.Renderers.Length; i++)
            {
                if (target.Renderers[i] != null)
                    target.Renderers[i].sharedMaterials = target.OriginalMaterials[i];
            }

            // Restore HitFlash last
            foreach (var hf in target.HitFlashes)
            {
                if (hf != null) hf.enabled = true;
            }
        }

        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _damageRadius);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, _triggerRadius);
    }
}


