using System.Collections.Generic;
using UnityEngine;

// Lobbed grenade that follows a simulated parabolic arc to a target position.
// Detonates on landing with AoE damage that hits everything in the blast radius —
// player, followers, and enemies alike.
// Spawned and launched by BomberAttack; call Launch() immediately after Instantiate.
public class GrenadeProjectile : MonoBehaviour
{
    [Header("Flight")]
    [Tooltip("How long the grenade takes to reach the target position.")]
    [SerializeField] private float _flightTime = 1f;

    [Tooltip("How high the arc peaks above the straight-line path. Keep in sync with BomberAttack._arcHeight.")]
    [SerializeField] private float _arcHeight = 3f;

    [Tooltip("How fast the grenade tumbles during flight — purely cosmetic.")]
    [SerializeField] private float _tumbleSpeed = 360f;

    [Header("Explosion")]
    [Tooltip("Radius of the AoE blast. Hits the player and followers too — position carefully.")]
    [SerializeField] private float _blastRadius = 4f;

    [Tooltip("Damage dealt to everything caught in the blast.")]
    [SerializeField] private int _damage = 2;

    [Header("Explosion Feedback")]
    [SerializeField] private GameObject _explosionParticlePrefab;

    [Tooltip("Fallback destroy time for the explosion particle if it has no ParticleSystem component.")]
    [SerializeField] private float _explosionParticleDestroyDelay = 3f;

    private Vector3 _startPosition;
    private Vector3 _targetPosition;
    private float _elapsed;
    private bool _hasLanded;

    // Called by BomberAttack immediately after Instantiate.
    public void Launch(Vector3 targetPosition)
    {
        _startPosition  = transform.position;
        _targetPosition = targetPosition;
        _elapsed        = 0f;
        _hasLanded      = false;
    }

    private void Update()
    {
        if (_hasLanded) return;

        _elapsed += Time.deltaTime;

        // Raw 0→1 progress through the flight duration
        float rawT = Mathf.Clamp01(_elapsed / _flightTime);

        // Ease-in: square the raw progress so the grenade starts slow and
        // accelerates toward the target — mimics the pull of gravity on descent.
        float easedT = rawT * rawT;

        // Straight-line lerp for X/Z with easing, plus a sine hump for Y.
        // Sin(easedT * PI) is 0 at launch, peaks mid-flight, and returns to 0 on landing.
        float arcedX = Mathf.Lerp(_startPosition.x, _targetPosition.x, easedT);
        float arcedZ = Mathf.Lerp(_startPosition.z, _targetPosition.z, easedT);
        float arcedY = Mathf.Lerp(_startPosition.y, _targetPosition.y, easedT)
                     + _arcHeight * Mathf.Sin(easedT * Mathf.PI);

        transform.position = new Vector3(arcedX, arcedY, arcedZ);

        // Tumble forward along the arc for a natural in-flight look
        transform.Rotate(Vector3.right, _tumbleSpeed * Time.deltaTime, Space.Self);

        if (rawT >= 1f)
        {
            _hasLanded         = true;
            transform.position = _targetPosition;
            Explode();
        }
    }

    private void Explode()
    {
        // HashSet prevents dealing damage twice to the same target if it has multiple colliders
        HashSet<IDamageable> alreadyDamaged = new HashSet<IDamageable>();

        Collider[] hitColliders = Physics.OverlapSphere(transform.position, _blastRadius);
        foreach (Collider hitCollider in hitColliders)
        {
            IDamageable damageable = hitCollider.GetComponentInParent<IDamageable>();
            if (damageable == null || alreadyDamaged.Contains(damageable)) continue;

            alreadyDamaged.Add(damageable);
            damageable.TakeDamage(_damage);
        }

        if (_explosionParticlePrefab != null)
        {
            GameObject explosionEffect = Instantiate(_explosionParticlePrefab, transform.position, Quaternion.identity);

            float destroyDelay = _explosionParticleDestroyDelay;
            ParticleSystem explosionParticles = explosionEffect.GetComponent<ParticleSystem>();
            if (explosionParticles != null)
                destroyDelay = explosionParticles.main.duration + explosionParticles.main.startLifetime.constantMax;

            Destroy(explosionEffect, destroyDelay);
        }

        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.4f, 0f, 0.25f);
        Gizmos.DrawSphere(transform.position, _blastRadius);
        Gizmos.color = new Color(1f, 0.4f, 0f, 1f);
        Gizmos.DrawWireSphere(transform.position, _blastRadius);
    }
}
