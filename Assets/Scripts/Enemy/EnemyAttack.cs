using UnityEngine;

// Ranged enemy AI: finds the nearest Player or Follower within attackRange, faces them,
// and fires at the rate set by fireRate. Target is refreshed on a timer rather than every
// frame to avoid an OverlapSphere call every Update tick.
public class EnemyAttack : MonoBehaviour
{
    [SerializeField] private float attackRange = 10f;
    [SerializeField] private float fireRate = 1f;
    [SerializeField] private float bulletSpeed = 12f;
    [SerializeField] private float bulletLifetime = 3f;
    [SerializeField] private float _targetRefreshInterval = 0.2f;

    [SerializeField] private Transform _firePoint;
    [SerializeField] private GameObject _bulletPrefab;
    private Animator _animator;

    // Static buffer shared across all EnemyAttack instances — NonAlloc writes into this
    private static readonly Collider[] _overlapBuffer = new Collider[32];

    private Transform _target;
    private float _fireCooldown;
    private float _targetRefreshTimer;

    void Start()
    {
        _fireCooldown = 1f / fireRate;
        _animator = GetComponentInChildren<Animator>();
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
        float nearestSquaredDistance = float.MaxValue;
        float squaredRange = attackRange * attackRange;

        for (int i = 0; i < count; i++)
        {
            Collider candidate = _overlapBuffer[i];
            if (!candidate.CompareTag("Player") && !candidate.CompareTag("Follower")) continue;

            // Squared distance — avoids a sqrt per candidate
            float squaredDistance = (transform.position - candidate.transform.position).sqrMagnitude;
            if (squaredDistance < nearestSquaredDistance && squaredDistance <= squaredRange)
            {
                nearestSquaredDistance = squaredDistance;
                nearest = candidate.transform;
            }
        }

        _target = nearest;
    }

    private void FaceTarget()
    {
        Vector3 directionToTarget = _target.position - transform.position;
        directionToTarget.y = 0f;
        if (directionToTarget != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(directionToTarget);
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
