using UnityEngine;

public class PlayerFire : MonoBehaviour
{
    [SerializeField] private Transform _firePoint;
    [SerializeField] private GameObject _bulletPrefab;
    [SerializeField] private LayerMask _enemyLayer;

    [Header("Bullet Settings")]
    [SerializeField] private float _bulletSpeed = 10f;
    [SerializeField] private float _fireRate = 0.5f;
    [SerializeField] private float _bulletLifetime = 2f;
    [SerializeField] private float _autoAimRange = 10f;
    [SerializeField] private float _autoAimAngle = 25f;

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
        // Use firePoint if assigned, otherwise fallback to player position
        Vector3 spawnPos = _firePoint != null ? _firePoint.position : transform.position;
        Quaternion spawnRot = _firePoint != null ? _firePoint.rotation : transform.rotation;

        GameObject bullet = Instantiate(_bulletPrefab, spawnPos, spawnRot);

        Rigidbody rb = bullet.GetComponent<Rigidbody>();

        if (rb != null)
        {
            Vector3 fireDir = GetFireDirection();
            rb.linearVelocity = fireDir * _bulletSpeed;
        }

        Destroy(bullet, _bulletLifetime);
    }

    private Vector3 GetFireDirection()
    {
        Transform fireTransform = _firePoint != null ? _firePoint : transform;

        Vector3 defaultDirection = fireTransform.forward;

        Collider[] hits = Physics.OverlapSphere(
            fireTransform.position,
            _autoAimRange,
            _enemyLayer
        );

        Transform bestTarget = null;
        float bestAngle = _autoAimAngle;

        foreach (Collider hit in hits)
        {
            Vector3 directionToEnemy =
                (hit.transform.position - fireTransform.position).normalized;

            float angle = Vector3.Angle(defaultDirection, directionToEnemy);

            if (angle < bestAngle)
            {
                bestAngle = angle;
                bestTarget = hit.transform;
            }
        }

        if (bestTarget != null)
        {
            return (bestTarget.position - fireTransform.position).normalized;
        }

        return defaultDirection;
    }


}
