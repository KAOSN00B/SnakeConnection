using UnityEngine;

public class PlayerFire : MonoBehaviour
{
    [SerializeField] private Transform _firePoint;
    [SerializeField] private GameObject _bulletPrefab;

    [Header("Bullet Settings")]
    [SerializeField] private float _bulletSpeed = 10f;
    [SerializeField] private float _fireRate = 0.5f;
    [SerializeField] private float _bulletLifetime = 2f;

    private float _nextFireTime = 0f;

    void Update()
    {
        HandleFireInput();
    }

    private void HandleFireInput()
    {
        if (Input.GetKey(KeyCode.Mouse0) && Time.time >= _nextFireTime)
        {
            _nextFireTime = Time.time + _fireRate;
            Fire();
        }
    }

    private void Fire()
    {
        Vector3 spawnPos = _firePoint != null ? _firePoint.position : transform.position;
        Quaternion spawnRot = _firePoint != null ? _firePoint.rotation : transform.rotation;
        Vector3 fireDir = GetFireDirection();

        BulletPool.Instance.Get(
            _bulletPrefab,
            spawnPos,
            spawnRot,
            fireDir * _bulletSpeed,
            _bulletLifetime,
            gameObject
        );
    }

    private Vector3 GetFireDirection()
    {
        Transform fireTransform = _firePoint != null ? _firePoint : transform;

        // Fire exactly where the laser points, flattened to XZ so height differences
        // between the firePoint and enemies never pull bullets up or down
        Vector3 dir = fireTransform.forward;
        dir.y = 0f;
        return dir.normalized;
    }
}
