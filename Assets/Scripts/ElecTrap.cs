using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Placed by ElectFollower. Waits on the ground for an enemy to step into its trigger radius.
// On trigger: shocks all enemies in _damageRadius (deals 1 damage, stuns yellow for _stunDuration).
// If nothing triggers it within _lifetime seconds, it disappears silently with no effect.
// Never triggers on the player or followers — Enemy tag check only.
public class ElecTrap : MonoBehaviour
{
    [Header("Radii")]
    [Tooltip("Radius of the trigger collider — what sets the trap off. Should be slightly smaller than damage radius.")]
    [SerializeField] private float _triggerRadius = 1.5f;

    [Tooltip("Radius of the shock area — can be wider than the trigger to catch nearby enemies too.")]
    [SerializeField] private float _damageRadius = 3f;

    [Header("Trap Settings")]
    [Tooltip("Seconds before the trap disappears on its own if never triggered.")]
    [SerializeField] private float _lifetime = 10f;

    [Tooltip("Damage dealt to each enemy in the shock radius.")]
    [SerializeField] private int _damage = 1;

    [Tooltip("Seconds enemies are stunned and tinted yellow after the shock.")]
    [SerializeField] private float _stunDuration = 1f;

    [SerializeField] private GameObject _electricityParticlePrefab;

    private ElectFollower _owner;
    private bool _triggered;
    private Collider _trapCollider;
    private Material _stunMaterial;

    // Sync trigger radius to the SphereCollider whenever values change in the Inspector
    private void OnValidate()
    {
        SphereCollider sphere = GetComponent<SphereCollider>();
        if (sphere != null) sphere.radius = _triggerRadius;
    }

    // Called by ElectFollower immediately after spawning this trap
    public void Initialize(ElectFollower owner)
    {
        _owner = owner;
        _trapCollider = GetComponent<Collider>();

        // Apply the serialized trigger radius to the collider at runtime too
        SphereCollider sphere = _trapCollider as SphereCollider;
        if (sphere != null) sphere.radius = _triggerRadius;

        _stunMaterial = new Material(Shader.Find("Unlit/Color")) { color = new Color(1f, 0.85f, 0f) };

        StartCoroutine(LifetimeRoutine());
    }

    private void OnDestroy()
    {
        if (_stunMaterial != null) Destroy(_stunMaterial);
    }

    private IEnumerator LifetimeRoutine()
    {
        yield return new WaitForSeconds(_lifetime);

        // Only deactivate silently if nothing already triggered us
        if (!_triggered)
        {
            _owner?.OnTrapDeactivated(wasTriggered: false);
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_triggered) return;
        if (!other.CompareTag("Enemy")) return;
        StartCoroutine(TriggerRoutine());
    }

    private IEnumerator TriggerRoutine()
    {
        _triggered = true;

        // Disable collider immediately so nothing re-triggers while the stun plays out
        if (_trapCollider != null) _trapCollider.enabled = false;

        // Notify owner right away so the post-trigger cooldown starts ticking
        _owner?.OnTrapDeactivated(wasTriggered: true);

        // Spawn electricity particle at trap position
        if (_electricityParticlePrefab != null)
        {
            GameObject fx = Instantiate(_electricityParticlePrefab, transform.position, Quaternion.identity);
            ParticleSystem ps = fx.GetComponent<ParticleSystem>();
            float destroyDelay = ps != null
                ? ps.main.duration + ps.main.startLifetime.constantMax
                : 3f;
            Destroy(fx, destroyDelay);
        }

        // Find all enemies in the wider damage radius
        Collider[] hits = Physics.OverlapSphere(transform.position, _damageRadius);

        // HashSet prevents double-damaging enemies that have multiple colliders
        HashSet<IDamageable> damaged = new HashSet<IDamageable>();

        // Store stun state so we can restore it after _stunDuration
        var stunTargets = new List<(Renderer[] renderers, Material[][] originalMaterials, MonoBehaviour movement)>();

        foreach (Collider hit in hits)
        {
            if (!hit.CompareTag("Enemy")) continue;

            // Damage
            IDamageable damageable = hit.GetComponentInParent<IDamageable>();
            if (damageable != null && damaged.Add(damageable))
                damageable.TakeDamage(_damage);

            // Find the movement component to freeze — check all known enemy movement types
            GameObject root = hit.transform.root.gameObject;
            MonoBehaviour movement = (MonoBehaviour)root.GetComponentInChildren<EnemyMovement>()
                ?? (MonoBehaviour)root.GetComponentInChildren<TailChaserMovement>()
                ?? (MonoBehaviour)root.GetComponentInChildren<HackerMovement>();

            if (movement == null || !movement.enabled) continue;
            movement.enabled = false;

            // Swap all renderers to yellow stun color
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
            Material[][] originalMaterials = new Material[renderers.Length][];
            for (int i = 0; i < renderers.Length; i++)
            {
                originalMaterials[i] = renderers[i].sharedMaterials;
                Material[] yellowSlots = new Material[originalMaterials[i].Length];
                for (int j = 0; j < yellowSlots.Length; j++) yellowSlots[j] = _stunMaterial;
                renderers[i].materials = yellowSlots;
            }

            stunTargets.Add((renderers, originalMaterials, movement));
        }

        yield return new WaitForSeconds(_stunDuration);

        // Restore movement and original materials on all stunned enemies
        foreach (var (renderers, originalMaterials, movement) in stunTargets)
        {
            if (movement != null) movement.enabled = true;
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                    renderers[i].sharedMaterials = originalMaterials[i];
            }
        }

        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        // Yellow = damage radius, cyan = trigger radius
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _damageRadius);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, _triggerRadius);
    }
}
