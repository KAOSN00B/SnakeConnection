using UnityEngine;

// Attach to any enemy prefab alongside Health.
// Spawns a particle effect at the enemy's position when it is destroyed.
public class EnemyDeathFeedback : MonoBehaviour
{
    [SerializeField] private GameObject _deathParticlePrefab;
    [Tooltip("Fallback destroy time if the particle has no ParticleSystem component.")]
    [SerializeField] private float _fallbackDestroyDelay = 5f;

    [Header("Audio")]
    [SerializeField] private AudioClip _deathSound;
    [Tooltip("Minimum pitch for the randomized death sound — lower = deeper.")]
    [SerializeField] private float _minPitch = 0.6f;
    [Tooltip("Maximum pitch for the randomized death sound.")]
    [SerializeField] private float _maxPitch = 0.85f;
    [SerializeField] [Range(0f, 1f)] private float _deathVolume = 0.5f;

    private void OnDestroy()
    {
        Debug.Log($"[EnemyDeathFeedback] OnDestroy fired on {gameObject.name} — scene loaded: {gameObject.scene.isLoaded} — clip: {_deathSound}");

        // Don't spawn particles during scene unload — OnDestroy fires for every
        // object when the scene reloads and we don't want stray particles
        if (!gameObject.scene.isLoaded) return;

        if (_deathSound != null)
        {
            GameObject tempAudio = new GameObject("EnemyDeathSound");
            tempAudio.transform.position = transform.position;
            AudioSource source = tempAudio.AddComponent<AudioSource>();
            source.clip = _deathSound;
            source.pitch = Random.Range(_minPitch, _maxPitch);
            source.volume = _deathVolume;
            source.spatialBlend = 0f;
            source.Play();
            Destroy(tempAudio, _deathSound.length / source.pitch + 0.1f);
        }

        EnemyMovement enemyMovement = GetComponent<EnemyMovement>() ?? GetComponentInParent<EnemyMovement>() ?? GetComponentInChildren<EnemyMovement>();
        if (enemyMovement != null)
            ScoreManager.Instance?.AddScore(enemyMovement.ScoreWorth);
        else
        {
            // Fallback to EnemyScoreValue if movement isn't found
            EnemyScoreValue scoreVal = GetComponent<EnemyScoreValue>() ?? GetComponentInParent<EnemyScoreValue>() ?? GetComponentInChildren<EnemyScoreValue>();
            if (scoreVal != null)
                ScoreManager.Instance?.AddScore(scoreVal.ScoreWorth);
        }

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
