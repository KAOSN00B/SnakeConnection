using System.Collections;
using UnityEngine;

// Periodically spawns pickup prefabs (followers, power-ups, etc.) on the square perimeter.
// Set the half-extents to match EnemySpawner so pickups appear within the same boundary.
// Attach to the same spawner GameObject as EnemySpawner, or a separate one at map center.
public class PickupSpawner : MonoBehaviour
{
    [Header("Pickup Types")]
    [Tooltip("Drag any pickup prefabs here — follower pickups, power-ups, etc. One is chosen per spawn.")]
    [SerializeField] private GameObject[] _pickupPrefabs;

    [Tooltip("Relative spawn weight for each prefab (same order as above). " +
             "Higher = more common. Values are ratios, not percentages — they don't need to add up to 100. " +
             "Example: 70 / 45 / 30 makes index 0 most common and index 2 least. " +
             "Leave empty to spawn all types with equal chance.")]
    [SerializeField] private float[] _spawnWeights;

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

        int randomPickupIndex = PickWeightedIndex();
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

    private int PickWeightedIndex()
    {
        // Fall back to uniform random if weights aren't set or don't match the prefab count
        if (_spawnWeights == null || _spawnWeights.Length != _pickupPrefabs.Length)
            return Random.Range(0, _pickupPrefabs.Length);

        // Sum all weights to find the total range for the random roll
        float total = 0f;
        foreach (float w in _spawnWeights)
            total += Mathf.Max(w, 0f);

        if (total <= 0f)
            return Random.Range(0, _pickupPrefabs.Length);

        // Roll a number across the total, then walk the array until we find which bucket it lands in
        float roll = Random.Range(0f, total);
        float cumulative = 0f;
        for (int i = 0; i < _spawnWeights.Length; i++)
        {
            cumulative += Mathf.Max(_spawnWeights[i], 0f);
            if (roll < cumulative)
                return i;
        }

        return _pickupPrefabs.Length - 1;
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
