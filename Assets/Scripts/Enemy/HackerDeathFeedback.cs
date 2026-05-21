using UnityEngine;

// Attach to the hacker prefab alongside HackerMovement and Health.
// On death: rescues the kidnapped follower if the timer has not yet expired, then spawns a death particle.
// Uses the same particle cleanup pattern as EnemyDeathFeedback.
public class HackerDeathFeedback : MonoBehaviour
{
    [SerializeField] private GameObject _deathParticlePrefab;

    [Tooltip("Fallback destroy time for the death particle if it has no ParticleSystem component.")]
    [SerializeField] private float _fallbackDestroyDelay = 5f;

    private HackerMovement _hacker;

    private void Awake()
    {
        // Cache the hacker component early. Using OnDestroy to find components
        // is unreliable because the hierarchy might already be partially dismantled.
        _hacker = GetComponentInParent<HackerMovement>();
        if (_hacker == null) _hacker = GetComponentInChildren<HackerMovement>();
    }

    private void OnDestroy()
    {
        // Don't do anything during scene unload — OnDestroy fires for every object on reload
        if (!gameObject.scene.isLoaded) return;

        // Trigger the rescue if the hacker was still holding someone
        if (_hacker != null && _hacker.KidnappedFollower != null)
        {
            _hacker.KidnappedFollower.Rescue();
        }

        // Spawn the death particle — same pattern as EnemyDeathFeedback
        if (_deathParticlePrefab == null) return;

        GameObject deathParticle = Instantiate(_deathParticlePrefab, transform.position, Quaternion.identity);

        // Read the particle duration so we know exactly when to clean it up
        float destroyDelay = _fallbackDestroyDelay;
        ParticleSystem particleSystem = deathParticle.GetComponent<ParticleSystem>();
        if (particleSystem != null)
            destroyDelay = particleSystem.main.duration + particleSystem.main.startLifetime.constantMax;

        Destroy(deathParticle, destroyDelay);
    }
}
