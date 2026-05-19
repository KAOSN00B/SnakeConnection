using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Enemy Types")]
    [SerializeField] private GameObject[] _enemyPrefabs;

    [Header("Spawn Ring")]
    [SerializeField] private float _minSpawnRadius = 15f;
    [SerializeField] private float _maxSpawnRadius = 25f;

    [Header("Spawn Timing")]
    [SerializeField] private float _spawnInterval = 3f;
    [SerializeField] private float _minimumSpawnInterval = 0.5f;
    [SerializeField] private float _spawnIntervalDecreaseRate = 0.05f;

    [Header("Enemy Unlock Times")]
    [SerializeField] private float _enemyType1UnlockTime = 30f;
    [SerializeField] private float _enemyType2UnlockTime = 60f;

    private float _gameTimer;
    private float _spawnTimer;
    private Transform _playerTransform;

    private void Start()
    {
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null) _playerTransform = player.transform;
    }

    private void Update()
    {
        _gameTimer += Time.deltaTime;
        _spawnTimer += Time.deltaTime;

        if (_spawnTimer >= _spawnInterval)
        {
            SpawnEnemy();
            _spawnTimer = 0f;
        }

        // Gradually increase spawn rate over time, clamped to minimum
        _spawnInterval -= _spawnIntervalDecreaseRate * Time.deltaTime;
        _spawnInterval = Mathf.Max(_spawnInterval, _minimumSpawnInterval);
    }

    private void SpawnEnemy()
    {
        if (_enemyPrefabs.Length == 0) return;

        // Re-find player if the cached reference was lost
        if (_playerTransform == null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null) _playerTransform = player.transform;
        }

        int maxEnemyIndex = GetHighestUnlockedEnemyIndex();
        int randomEnemyIndex = Random.Range(0, maxEnemyIndex + 1);

        Instantiate(_enemyPrefabs[randomEnemyIndex], GetSpawnRingPosition(), Quaternion.identity);
    }

    private int GetHighestUnlockedEnemyIndex()
    {
        if (_gameTimer >= _enemyType2UnlockTime && _enemyPrefabs.Length > 2) return 2;
        if (_gameTimer >= _enemyType1UnlockTime && _enemyPrefabs.Length > 1) return 1;
        return 0;
    }

    private Vector3 GetSpawnRingPosition()
    {
        // Spawn relative to player so pressure is always present regardless of where they've moved
        Vector3 center = _playerTransform != null ? _playerTransform.position : transform.position;

        float angle = Random.Range(0f, Mathf.PI * 2f);
        float radius = Random.Range(_minSpawnRadius, _maxSpawnRadius);

        return new Vector3(
            center.x + Mathf.Cos(angle) * radius,
            transform.position.y,
            center.z + Mathf.Sin(angle) * radius
        );
    }

    private void OnDrawGizmosSelected()
    {
        // Draw inner and outer spawn ring so you can visualize the spawn band in the editor
        Gizmos.color = Color.yellow;
        DrawGizmoRing(_minSpawnRadius);
        Gizmos.color = Color.red;
        DrawGizmoRing(_maxSpawnRadius);
    }

    private void DrawGizmoRing(float radius)
    {
        int segments = 32;
        for (int i = 0; i < segments; i++)
        {
            float a1 = (i / (float)segments) * Mathf.PI * 2f;
            float a2 = ((i + 1) / (float)segments) * Mathf.PI * 2f;
            Vector3 p1 = transform.position + new Vector3(Mathf.Cos(a1) * radius, 0f, Mathf.Sin(a1) * radius);
            Vector3 p2 = transform.position + new Vector3(Mathf.Cos(a2) * radius, 0f, Mathf.Sin(a2) * radius);
            Gizmos.DrawLine(p1, p2);
        }
    }
}
