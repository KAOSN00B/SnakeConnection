using UnityEngine;

// Enemy type that ignores the player and hunts the tail of the chain.
// Falls back to the player only when no followers exist.
// Assign this instead of EnemyMovement on your tail-chaser prefab.
public class TailChaserMovement : MonoBehaviour
{
    [SerializeField] private float _speed = 7f;
    [SerializeField] private int _damage = 1;
    [SerializeField] private float _attackCooldown = 1f;

    [SerializeField] private float _targetRefreshInterval = 0.2f;

    private Transform _playerTransform;
    private Transform _target;
    private float _nextAttackTime;
    private Vector3 _directionToTarget;
    private float _targetRefreshTimer;

    private void OnEnable()  => EnemyRegistry.Register(transform);
    private void OnDisable() => EnemyRegistry.Unregister(transform);

    private void Start()
    {
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null) _playerTransform = player.transform;
    }

    private void Update()
    {
        _targetRefreshTimer -= Time.deltaTime;
        if (_targetRefreshTimer <= 0f)
        {
            UpdateTarget();
            _targetRefreshTimer = _targetRefreshInterval;
        }
        if (_target == null) return;

        Vector3 flat = _target.position - transform.position;
        flat.y = 0f;
        _directionToTarget = flat.normalized;

        FaceTarget();
    }

    private void FixedUpdate()
    {
        if (_target == null) return;
        transform.position += _directionToTarget * _speed * Time.fixedDeltaTime;
    }

    private void UpdateTarget()
    {
        if (ChainManager.Instance == null || !ChainManager.Instance.HasFollowers)
        {
            _target = _playerTransform;
            return;
        }

        // Always go for the tail — the furthest, most exposed follower
        Transform tail = ChainManager.Instance.GetTailFollower();
        _target = tail != null ? tail : _playerTransform;
    }

    private void FaceTarget()
    {
        if (_directionToTarget != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(_directionToTarget);
    }

    private void OnCollisionStay(Collision collision) => ProcessContact(collision.gameObject);
    private void OnTriggerStay(Collider other) => ProcessContact(other.gameObject);

    private void ProcessContact(GameObject target)
    {
        if (!target.CompareTag("Player") && !target.CompareTag("Follower")) return;
        if (Time.time < _nextAttackTime) return;

        IDamageable damageable = target.GetComponent<IDamageable>();
        if (damageable != null)
        {
            damageable.TakeDamage(_damage);
            _nextAttackTime = Time.time + _attackCooldown;
        }
    }
}
