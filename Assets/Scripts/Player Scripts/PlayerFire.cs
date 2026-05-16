using UnityEngine;

public class PlayerFire : MonoBehaviour
{
    [SerializeField] private Transform _firePoint;
    [SerializeField] private GameObject _bulletPrefab;

    [Header("Bullet Settings")]
    [SerializeField] private float _bulletSpeed = 10f;
    [SerializeField] private float _fireRate = 0.5f;
    [SerializeField] private float _nextFireTime = 1.0f;
    [SerializeField] private float _bulletLifetime = 2f;

    private Collider _playerCollider;

    void Start()
    {
        _playerCollider = GetComponent<Collider>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Mouse0) && Time.time >= _nextFireTime)
        {
            _nextFireTime = Time.time + _fireRate;
            Fire();

        }
    }

    private void Fire()
    {
        // Use firePoint if assigned, otherwise fallback to player position
        Vector3 spawnPos = _firePoint != null ? _firePoint.position : transform.position;
        Quaternion spawnRot = _firePoint != null ? _firePoint.rotation : transform.rotation;

        GameObject bullet = Instantiate(_bulletPrefab, spawnPos, spawnRot);
        
        // Ignore collision between bullet and player so they don't push each other
        Collider bulletCollider = bullet.GetComponent<Collider>();
        if (bulletCollider != null && _playerCollider != null)
        {
            Physics.IgnoreCollision(bulletCollider, _playerCollider);
        }

        Rigidbody rb = bullet.GetComponent<Rigidbody>();
        if (rb != null)
        {
            // Use the firePoint's forward if available
            Vector3 fireDir = _firePoint != null ? _firePoint.forward : transform.forward;
            rb.linearVelocity = fireDir * _bulletSpeed;
        }
        
        Destroy(bullet, _bulletLifetime);
    }
}
