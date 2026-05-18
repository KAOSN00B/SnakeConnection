using UnityEngine;

public class FolloweAttack : MonoBehaviour
{
    [SerializeField] private float _attackRange = 10f;
    [SerializeField] private float _fireRate = 1f;
    [SerializeField] private float _bulletSpeed = 20f;
    [SerializeField] private float _bulletLifetime = 3f;

    [SerializeField] private Transform _firePoint;
    [SerializeField] private GameObject _bulletPrefab;

    private Transform _enemyTarget;
    private float _fireCooldown;

    public bool HasTarget => _enemyTarget != null;

    void Start()
    {
        _fireCooldown = 0f;
    }

    void Update()
    {
        FindNearestTarget();
        if (_enemyTarget != null)
        {
            FaceTarget();
            HandleFiring();
        }
    }

    private void FaceTarget()
    {
        Vector3 dir = _enemyTarget.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 10f);
        }
    }

    private void FindNearestTarget()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");

        float shortestDistance = Mathf.Infinity;
        GameObject nearestEnemy = null;

        foreach (GameObject enemy in enemies)
        {
            float distanceToEnemy = Vector3.Distance(transform.position, enemy.transform.position);

            if (distanceToEnemy < shortestDistance)
            {
                shortestDistance = distanceToEnemy;
                nearestEnemy = enemy;
            }
        }

        if (nearestEnemy != null && shortestDistance <= _attackRange)
        {
            _enemyTarget = nearestEnemy.transform;
        }
        else
        {
            _enemyTarget = null;
        }
    }

    private void HandleFiring()
    {
        _fireCooldown -= Time.deltaTime;
        if (_fireCooldown <= 0f)
        {
            Shoot();
            _fireCooldown = 1f / _fireRate;
        }
    }

    private void Shoot()
    {
        if (_bulletPrefab == null || _enemyTarget == null) return;

        // Aim at the center of the enemy's collider for much better accuracy
        Vector3 targetPos = _enemyTarget.position;
        if (_enemyTarget.TryGetComponent<Collider>(out var col))
        {
            targetPos = col.bounds.center;
        }
        else
        {
            targetPos += Vector3.up * 1f; // Fallback: aim 1 unit up from feet
        }

        // Use FirePoint if assigned, otherwise use transform position with a small offset
        Vector3 spawnPos = _firePoint != null ? _firePoint.position : (transform.position + transform.forward * 0.5f + Vector3.up * 0.8f);
        
        // Calculate direction to target center
        Vector3 fireDir = (targetPos - spawnPos).normalized;
        if (fireDir == Vector3.zero) fireDir = transform.forward;

        // Instantiate bullet looking toward the target
        GameObject bullet = Instantiate(_bulletPrefab, spawnPos, Quaternion.LookRotation(fireDir));
        
        Bullet bScript = bullet.GetComponent<Bullet>();
        if (bScript != null) bScript.SetOwner(gameObject);

        Rigidbody rb = bullet.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = fireDir * _bulletSpeed;
        }

        Destroy(bullet, _bulletLifetime);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector3 center = _firePoint != null ? _firePoint.position : transform.position;
        Gizmos.DrawWireSphere(center, _attackRange);
    }
}