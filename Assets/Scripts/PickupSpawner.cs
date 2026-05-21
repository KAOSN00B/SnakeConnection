using System.Collections;
using UnityEngine;

// Periodically spawns pickup prefabs (followers, power-ups, etc.) on the square perimeter.
// Set the half-extents to match EnemySpawner so pickups appear within the same boundary.
// Attach to the same spawner GameObject as EnemySpawner, or a separate one at map center.
public class PickupSpawner : MonoBehaviour
{
    [Header("Pickup Types")]
    [Tooltip("Drag any pickup prefabs here — follower pickups, power-ups, etc. One is chosen at random each spawn.")]
    [SerializeField] private GameObject[] _pickupPrefabs;

    [Header("Spawn Area")]
    // Keep these values in sync with EnemySpawner's _spawnHalfX / _spawnHalfZ
    [SerializeField] private float _spawnHalfX = 15f;
    [SerializeField] private float _spawnHalfZ = 15f;
    [Tooltip("Adjust this if pickups spawn above or below the map surface.")]
    [SerializeField] private float _spawnYOffset = 0f;

    [Header("Spawn Timing")]
    [Tooltip("Seconds between each pickup spawn.")]
    [SerializeField] private float _spawnInterval = 10f;

    [Header("Spawn Warning")]
    [Tooltip("Optional particle prefab that plays at the spawn point before the pickup appears. Leave empty to spawn instantly.")]
    [SerializeField] private GameObject _spawnWarningParticlePrefab;
    [Tooltip("How many seconds the warning plays before the pickup actually appears.")]
    [SerializeField] private float _spawnWarningDuration = 3f;

    private float _spawnTimer;

    private void Update()
    {
        _spawnTimer += Time.deltaTime;
        if (_spawnTimer >= _spawnInterval)
        {
            SpawnPickup();
            _spawnTimer = 0f;
        }
    }

    private void SpawnPickup()
    {
        if (_pickupPrefabs.Length == 0) return;

        int randomPickupIndex = Random.Range(0, _pickupPrefabs.Length);
        Vector3 spawnPos = GetSpawnPosition();

        if (_spawnWarningParticlePrefab != null)
            StartCoroutine(SpawnWithWarning(spawnPos, randomPickupIndex));
        else
            Instantiate(_pickupPrefabs[randomPickupIndex], spawnPos, Quaternion.identity);
    }

    private IEnumerator SpawnWithWarning(Vector3 spawnPos, int pickupIndex)
    {
        GameObject warning = Instantiate(_spawnWarningParticlePrefab, spawnPos, Quaternion.identity);
        Destroy(warning, _spawnWarningDuration);

        yield return new WaitForSeconds(_spawnWarningDuration);

        Instantiate(_pickupPrefabs[pickupIndex], spawnPos, Quaternion.identity);
    }

    private Vector3 GetSpawnPosition()
    {
        // Same perimeter logic as EnemySpawner — picks one of four sides then randomizes along it
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

        return new Vector3(center.x + x, transform.position.y + _spawnYOffset, center.z + z);
    }

    // Cyan gizmo so it's visually distinct from EnemySpawner's yellow rectangle
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Vector3 center      = transform.position;
        Vector3 topLeft     = center + new Vector3(-_spawnHalfX, 0f,  _spawnHalfZ);
        Vector3 topRight    = center + new Vector3( _spawnHalfX, 0f,  _spawnHalfZ);
        Vector3 bottomRight = center + new Vector3( _spawnHalfX, 0f, -_spawnHalfZ);
        Vector3 bottomLeft  = center + new Vector3(-_spawnHalfX, 0f, -_spawnHalfZ);
        Gizmos.DrawLine(topLeft,     topRight);
        Gizmos.DrawLine(topRight,    bottomRight);
        Gizmos.DrawLine(bottomRight, bottomLeft);
        Gizmos.DrawLine(bottomLeft,  topLeft);
    }
}
