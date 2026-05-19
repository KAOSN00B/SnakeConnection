using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Enemy Types")]
    [SerializeField] private GameObject[] _enemyPrefabs;

    [Header("Spawn Square")]
    [SerializeField] private float _spawnHalfX = 15f;   // east / west walls
    [SerializeField] private float _spawnHalfZ = 15f;   // north / south walls

    [Header("Debug")]
    [SerializeField] private bool _debugSpawns = true;

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

        Instantiate(_enemyPrefabs[randomEnemyIndex], GetSpawnPosition(), Quaternion.identity);
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
        string sideName;
        switch (side)
        {
            case 0:  x = Random.Range(-_spawnHalfX, _spawnHalfX); z =  _spawnHalfZ; sideName = "North"; break;
            case 1:  x = Random.Range(-_spawnHalfX, _spawnHalfX); z = -_spawnHalfZ; sideName = "South"; break;
            case 2:  x = -_spawnHalfX; z = Random.Range(-_spawnHalfZ, _spawnHalfZ); sideName = "West";  break;
            default: x =  _spawnHalfX; z = Random.Range(-_spawnHalfZ, _spawnHalfZ); sideName = "East";  break;
        }

        Vector3 spawnPos = new Vector3(center.x + x, transform.position.y, center.z + z);

        if (_debugSpawns)
            Debug.Log($"[EnemySpawner] Center: {center} | Side: {sideName} | SpawnPos: {spawnPos}");

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
