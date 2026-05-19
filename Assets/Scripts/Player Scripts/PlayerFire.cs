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
    [Tooltip("0 = laser direction only, 1 = full snap to enemy. Keep low (0.25-0.4) for subtle magnetism.")]
    [SerializeField] [Range(0f, 1f)] private float _aimAssistStrength = 0.3f;

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

        Bullet bulletScript = bullet.GetComponent<Bullet>();
        if (bulletScript != null)
        {
            bulletScript.SetOwner(gameObject);
        }

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

        // Flatten the laser forward onto XZ — enemies can be at a different Y than the firePoint,
        // and we never want the bullet to travel upward or downward
        Vector3 laserDir = fireTransform.forward;
        laserDir.y = 0f;
        laserDir.Normalize();

        Collider[] hits = Physics.OverlapSphere(fireTransform.position, _autoAimRange, _enemyLayer);

        Transform bestTarget = null;
        float bestAngle = _autoAimAngle;

        foreach (Collider hit in hits)
        {
            // Flatten enemy direction too — height difference must not pull bullets upward
            Vector3 toEnemy = hit.transform.position - fireTransform.position;
            toEnemy.y = 0f;
            if (toEnemy.sqrMagnitude < 0.001f) continue;

            float angle = Vector3.Angle(laserDir, toEnemy.normalized);
            if (angle < bestAngle)
            {
                bestAngle  = angle;
                bestTarget = hit.transform;
            }
        }

        if (bestTarget == null)
            return laserDir;

        // Blend laser direction with enemy direction rather than replacing it.
        // At 0.3 the bullet travels 70% laser / 30% toward the enemy — feels like
        // magnetism rather than snapping, and stays visually close to the laser.
        Vector3 toTarget = bestTarget.position - fireTransform.position;
        toTarget.y = 0f;
        toTarget.Normalize();

        return Vector3.Slerp(laserDir, toTarget, _aimAssistStrength).normalized;
    }


}
