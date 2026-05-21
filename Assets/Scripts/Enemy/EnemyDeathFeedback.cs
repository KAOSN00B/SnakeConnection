using UnityEngine;

// Attach to any enemy prefab alongside Health.
// Spawns a particle effect at the enemy's position when it is destroyed.
public class EnemyDeathFeedback : MonoBehaviour
{
    [SerializeField] private GameObject _deathParticlePrefab;
    [Tooltip("Fallback destroy time if the particle has no ParticleSystem component.")]
    [SerializeField] private float _fallbackDestroyDelay = 5f;

    private void OnDestroy()
    {
        // Don't spawn particles during scene unload — OnDestroy fires for every
        // object when the scene reloads and we don't want stray particles
        if (!gameObject.scene.isLoaded) return;
        if (_deathParticlePrefab == null) return;

        GameObject particle = Instantiate(_deathParticlePrefab, transform.position, Quaternion.identity);

        // Read the particle duration so we know exactly when to destroy the GameObject.
        // duration = how long the emitter runs, startLifetime = how long each particle lives after emit.
        // We need both so particles that outlive the emitter are still cleaned up.
        float destroyDelay = _fallbackDestroyDelay;
        ParticleSystem ps = particle.GetComponent<ParticleSystem>();
        if (ps != null)
            destroyDelay = ps.main.duration + ps.main.startLifetime.constantMax;

        Destroy(particle, destroyDelay);
    }
}
