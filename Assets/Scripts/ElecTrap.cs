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

    private ElectFollower _owner;
    private bool _triggered;
    private Material _stunMaterial;

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

        // Notify owner right away so the post-trigger cooldown starts
        _owner?.OnTrapDeactivated(wasTriggered: true);

        // Spawn electricity particle
        if (_electricityParticlePrefab != null)
        {
            GameObject fx = Instantiate(_electricityParticlePrefab, transform.position, Quaternion.identity);
            float destroyDelay = _fallbackParticleDestroyDelay;
            ParticleSystem ps = fx.GetComponent<ParticleSystem>();
            if (ps != null)
                destroyDelay = ps.main.duration + ps.main.startLifetime.constantMax;
            Destroy(fx, destroyDelay);
        }

        // Find all enemies in the wider damage radius
        Collider[] hits = Physics.OverlapSphere(transform.position, _damageRadius);

        HashSet<IDamageable> damaged = new HashSet<IDamageable>();
        var stunTargets = new List<(Renderer[] renderers, Material[][] originalMaterials, Material[][] yellowMaterials, MonoBehaviour movement)>();

        foreach (Collider hit in hits)
        {
            if (!hit.CompareTag("Enemy")) continue;

            // Damage — HashSet deduplicates enemies with multiple colliders
            IDamageable damageable = hit.GetComponentInParent<IDamageable>();
            if (damageable != null && damaged.Add(damageable))
                damageable.TakeDamage(_damage);

            // Find and disable movement component
            GameObject root = hit.transform.root.gameObject;
            MonoBehaviour movement = (MonoBehaviour)root.GetComponentInChildren<EnemyMovement>()
                ?? (MonoBehaviour)root.GetComponentInChildren<TailChaserMovement>()
                ?? (MonoBehaviour)root.GetComponentInChildren<HackerMovement>();

            if (movement == null || !movement.enabled) continue;
            movement.enabled = false;

            // Pre-build both material arrays so we can toggle quickly during the flash loop
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
            Material[][] originalMaterials = new Material[renderers.Length][];
            Material[][] yellowMaterials   = new Material[renderers.Length][];
            for (int i = 0; i < renderers.Length; i++)
            {
                originalMaterials[i] = renderers[i].sharedMaterials;
                yellowMaterials[i]   = new Material[originalMaterials[i].Length];
                for (int j = 0; j < yellowMaterials[i].Length; j++)
                    yellowMaterials[i][j] = _stunMaterial;
            }

            stunTargets.Add((renderers, originalMaterials, yellowMaterials, movement));
        }

        // Flash enemies yellow for the stun duration
        float elapsed = 0f;
        bool showingYellow = false;
        WaitForSeconds flashWait = new WaitForSeconds(_flashInterval);

        while (elapsed < _stunDuration)
        {
            showingYellow = !showingYellow;
            foreach (var (renderers, originalMaterials, yellowMaterials, _) in stunTargets)
            {
                for (int i = 0; i < renderers.Length; i++)
                {
                    if (renderers[i] != null)
                        renderers[i].materials = showingYellow ? yellowMaterials[i] : originalMaterials[i];
                }
            }
            yield return flashWait;
            elapsed += _flashInterval;
        }

        // Restore movement and original materials on all stunned enemies.
        // Using .materials (not .sharedMaterials) so we override any white instances
        // that HitFlash may have left behind — both end up at the same visual result.
        foreach (var (renderers, originalMaterials, yellowMaterials, movement) in stunTargets)
        {
            if (movement != null) movement.enabled = true;
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                    renderers[i].materials = originalMaterials[i];
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
