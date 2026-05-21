using System.Collections;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Enemy Types")]
    [SerializeField] private GameObject[] _enemyPrefabs;

    [Header("Spawn Square")]
    [SerializeField] private float _spawnHalfX = 15f;   // east / west walls
    [SerializeField] private float _spawnHalfZ = 15f;   // north / south walls

    [Header("Enemy Cap")]
    [Tooltip("How many enemies are allowed alive at game start.")]
    [SerializeField] private int _initialEnemyLimit = 5;
    [Tooltip("Hard ceiling — the cap will never exceed this even late game.")]
    [SerializeField] private int _maximumEnemyLimit = 35;
    [Tooltip("How many extra enemies are added each interval.")]
    [SerializeField] private int _enemyLimitIncreaseAmount = 5;
    [Tooltip("Seconds between each cap increase.")]
    [SerializeField] private float _enemyLimitIncreaseInterval = 20f;

    [Header("Spawn Timing")]
    [SerializeField] private float _spawnInterval = 3f;
    [SerializeField] private float _minimumSpawnInterval = 0.5f;
    [SerializeField] private float _spawnIntervalDecreaseRate = 0.05f;

    [Header("Enemy Unlock Times")]
    [SerializeField] private float _enemyType1UnlockTime = 30f;
    [SerializeField] private float _enemyType2UnlockTime = 60f;

    [Header("Spawn Warning")]
    [Tooltip("Optional particle prefab that plays at the spawn point before the enemy appears. Leave empty to spawn instantly.")]
    [SerializeField] private GameObject _spawnWarningParticlePrefab;
    [Tooltip("How many seconds the warning plays before the enemy actually appears.")]
    [SerializeField] private float _spawnWarningDuration = 3f;

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

        // Skip the spawn entirely if we're already at the current cap.
        // This prevents runaway enemy counts from tanking the frame rate while
        // still allowing the cap to grow gradually as the session goes on.
        if (EnemyRegistry.Count >= GetCurrentEnemyLimit()) return;

        // Re-find player if the cached reference was lost
        if (_playerTransform == null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null) _playerTransform = player.transform;
        }

        int maxEnemyIndex = GetHighestUnlockedEnemyIndex();
        int randomEnemyIndex = Random.Range(0, maxEnemyIndex + 1);
        Vector3 spawnPos = GetSpawnPosition();

        // If a warning particle is assigned, telegraph the spawn first then delay the enemy.
        // Otherwise spawn immediately so the game works without any particle set up.
        if (_spawnWarningParticlePrefab != null)
            StartCoroutine(SpawnWithWarning(spawnPos, randomEnemyIndex));
        else
            Instantiate(_enemyPrefabs[randomEnemyIndex], spawnPos, Quaternion.identity);
    }

    // Shows a warning particle at the spawn point, waits, then drops the enemy in.
    // Gives the player a chance to react before the enemy appears.
    private IEnumerator SpawnWithWarning(Vector3 spawnPos, int enemyIndex)
    {
        GameObject warning = Instantiate(_spawnWarningParticlePrefab, spawnPos, Quaternion.identity);
        // Auto-destroy the particle so it doesn't linger if its duration is shorter than _spawnWarningDuration
        Destroy(warning, _spawnWarningDuration);

        yield return new WaitForSeconds(_spawnWarningDuration);

        Instantiate(_enemyPrefabs[enemyIndex], spawnPos, Quaternion.identity);
    }

    // Calculates how many enemies are allowed alive right now.
    // Starts low for early-game fairness, increases every interval, hard-capped at maximum.
    // This keeps escalation controlled and frame rate protected even in long sessions.
    private int GetCurrentEnemyLimit()
    {
        int increaseSteps = Mathf.FloorToInt(_gameTimer / _enemyLimitIncreaseInterval);
        int currentLimit = _initialEnemyLimit + increaseSteps * _enemyLimitIncreaseAmount;
        return Mathf.Min(currentLimit, _maximumEnemyLimit);
    }

    private int GetHighestUnlockedEnemyIndex()
    {
        if (_gameTimer >= _enemyType2UnlockTime && _enemyPrefabs.Length > 2) return 2;
        if (_gameTimer >= _enemyType1UnlockTime && _enemyPrefabs.Length > 1) return 1;
        return 0;
    }

    private Vector3 GetSpawnPosition()
    {
        // Fixed square centered on the spawner — place the spawner at the map center.
        Vector3 center = transform.position;

        int side = Random.Range(0, 4);

        float x, z;
        switch (side)
        {
            case 0:  x = Random.Range(-_spawnHalfX, _spawnHalfX); z =  _spawnHalfZ; break; // North
            case 1:  x = Random.Range(-_spawnHalfX, _spawnHalfX); z = -_spawnHalfZ; break; // South
            case 2:  x = -_spawnHalfX; z = Random.Range(-_spawnHalfZ, _spawnHalfZ); break; // West
            default: x =  _spawnHalfX; z = Random.Range(-_spawnHalfZ, _spawnHalfZ); break; // East
        }

        Vector3 spawnPos = new Vector3(center.x + x, transform.position.y, center.z + z);

        return spawnPos;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector3 o = transform.position;
        Vector3 tl = o + new Vector3(-_spawnHalfX, 0f,  _spawnHalfZ);
        Vector3 tr = o + new Vector3( _spawnHalfX, 0f,  _spawnHalfZ);
        Vector3 br = o + new Vector3( _spawnHalfX, 0f, -_spawnHalfZ);
        Vector3 bl = o + new Vector3(-_spawnHalfX, 0f, -_spawnHalfZ);
        Gizmos.DrawLine(tl, tr);
        Gizmos.DrawLine(tr, br);
        Gizmos.DrawLine(br, bl);
        Gizmos.DrawLine(bl, tl);
    }
}
