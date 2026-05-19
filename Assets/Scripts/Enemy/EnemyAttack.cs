using UnityEngine;

public class EnemyAttack : MonoBehaviour
{
    [SerializeField] private float attackRange = 10f;
    [SerializeField] private float fireRate = 1f;
    [SerializeField] private float bulletSpeed = 12f;
    [SerializeField] private float bulletLifetime = 3f;
    [SerializeField] private float _targetRefreshInterval = 0.2f;

    [SerializeField] private Transform _firePoint;
    [SerializeField] private GameObject _bulletPrefab;

    // Static buffer shared across all EnemyAttack instances — NonAlloc writes into this
    private static readonly Collider[] _overlapBuffer = new Collider[32];

    private Transform _target;
    private float _fireCooldown;
    private float _targetRefreshTimer;

    void Start()
    {
        _fireCooldown = 1f / fireRate;
    }

    void Update()
    {
        _targetRefreshTimer -= Time.deltaTime;
        if (_targetRefreshTimer <= 0f)
        {
            FindNearestTarget();
            _targetRefreshTimer = _targetRefreshInterval;
        }

        if (_target == null) return;
        FaceTarget();
        HandleFiring();
    }

    private void FindNearestTarget()
    {
        // NonAlloc: writes into the shared buffer instead of allocating a new array each call
        int count = Physics.OverlapSphereNonAlloc(transform.position, attackRange, _overlapBuffer);

        Transform nearest = null;
        float nearestSq = float.MaxValue;
        float rangeSq = attackRange * attackRange;

        for (int i = 0; i < count; i++)
        {
            Collider hit = _overlapBuffer[i];
            if (!hit.CompareTag("Player") && !hit.CompareTag("Follower")) continue;

            // Squared distance — avoids a sqrt per candidate
            float sq = (transform.position - hit.transform.position).sqrMagnitude;
            if (sq < nearestSq && sq <= rangeSq)
            {
                nearestSq = sq;
                nearest = hit.transform;
            }
        }

        _target = nearest;
    }

    private void FaceTarget()
    {
        Vector3 dir = _target.position - transform.position;
        dir.y = 0f;
        if (dir != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(dir);
    }

    private void HandleFiring()
    {
        _fireCooldown -= Time.deltaTime;
        if (_fireCooldown <= 0f)
        {
            Shoot();
            _fireCooldown = 1f / fireRate;
        }
    }

    private void Shoot()
    {
        if (_firePoint == null || _bulletPrefab == null) return;

        // No owner passed — enemy bullets must be able to hit Player and Follower
        BulletPool.Instance.Get(
            _bulletPrefab,
            _firePoint.position,
            _firePoint.rotation,
            _firePoint.forward * bulletSpeed,
            bulletLifetime
        );
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector3 center = _firePoint != null ? _firePoint.position : transform.position;
        Gizmos.DrawWireSphere(center, attackRange);
    }
}
