using UnityEngine;

// Follower auto-attack: periodically finds the nearest enemy within _attackRange via EnemyRegistry,
// faces them, and fires on cooldown. Rotates toward movement direction when no target is in range.
public class FolloweAttack : MonoBehaviour
{
    [SerializeField] private float _attackRange = 10f;
    [SerializeField] private float _fireRate = 1f;
    [SerializeField] private float _bulletSpeed = 20f;
    [SerializeField] private float _bulletLifetime = 3f;
    [SerializeField] private float _targetRefreshInterval = 0.2f;

    [SerializeField] private Transform _firePoint;
    [SerializeField] private GameObject _bulletPrefab;

    private Transform _enemyTarget;
    private float _fireCooldown;
    private float _targetRefreshTimer;

    public bool HasTarget => _enemyTarget != null;

    void Start()
    {
        _fireCooldown = 0f;
    }

    void Update()
    {
        // Refresh target on a timer — EnemyRegistry walk is cheap but no need to do it every frame
        _targetRefreshTimer -= Time.deltaTime;
        if (_targetRefreshTimer <= 0f)
        {
            FindNearestTarget();
            _targetRefreshTimer = _targetRefreshInterval;
        }

        if (_enemyTarget != null)
        {
            FaceTarget();
            HandleFiring();
        }
    }

    private void FaceTarget()
    {
        Vector3 directionToEnemy = _enemyTarget.position - transform.position;
        directionToEnemy.y = 0f;
        if (directionToEnemy.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(directionToEnemy);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 10f);
        }
    }

    private void FindNearestTarget()
    {
        // EnemyRegistry replaces FindGameObjectsWithTag — no allocation, no scene scan
        float rangeSq = _attackRange * _attackRange;
        Transform nearest = EnemyRegistry.GetNearest(transform.position, rangeSq);

        _enemyTarget = nearest; // null if nothing in range
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

        Vector3 targetPos = _enemyTarget.position;
        if (_enemyTarget.TryGetComponent<Collider>(out var enemyCollider))
            targetPos = enemyCollider.bounds.center;
        else
            targetPos += Vector3.up * 1f;

        Vector3 spawnPos = _firePoint != null
            ? _firePoint.position
            : (transform.position + transform.forward * 0.5f + Vector3.up * 0.8f);

        Vector3 fireDir = (targetPos - spawnPos).normalized;
        if (fireDir == Vector3.zero) fireDir = transform.forward;

        BulletPool.Instance.Get(
            _bulletPrefab,
            spawnPos,
            Quaternion.LookRotation(fireDir),
            fireDir * _bulletSpeed,
            _bulletLifetime,
            gameObject
        );
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector3 center = _firePoint != null ? _firePoint.position : transform.position;
        Gizmos.DrawWireSphere(center, _attackRange);
    }
}
