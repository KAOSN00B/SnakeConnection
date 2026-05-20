using UnityEngine;

public class EnemyMovement : MonoBehaviour
{
    [SerializeField] private float _speed = 7f;
    [SerializeField] private int _damage = 1;
    [SerializeField] private float _attackCooldown = 1f;

    [SerializeField] private float _targetRefreshInterval = 0.2f;

    private Transform _playerTransform;
    private Transform _target;
    private float _nextAttackTime;
    private float _targetRefreshTimer;
    private Animator _animator;

    private void OnEnable()  => EnemyRegistry.Register(transform);
    private void OnDisable() => EnemyRegistry.Unregister(transform);

    private void Start()
    {
        _playerTransform = FindPlayer();
        UpdateTarget();
        _animator = GetComponentInChildren<Animator>();
        if (_animator != null)
        {
            _animator.speed = _speed / 2.5f;
        }
    }

    private void FixedUpdate()
    {
        if (_playerTransform == null) _playerTransform = FindPlayer();

        // Refresh target on a timer instead of every tick — saves a full follower list walk
        _targetRefreshTimer -= Time.fixedDeltaTime;
        if (_targetRefreshTimer <= 0f)
        {
            UpdateTarget();
            _targetRefreshTimer = _targetRefreshInterval;
        }
        if (_target == null) return;

        // Compute direction fresh every physics tick — no dependency on Update having run first
        Vector3 flat = _target.position - transform.position;
        flat.y = 0f;

        if (flat.sqrMagnitude < 0.01f) return;

        Vector3 dir = flat.normalized;
        transform.position += dir * _speed * Time.fixedDeltaTime;
        transform.rotation = Quaternion.LookRotation(dir);

        if (_animator != null)
        {
            _animator.speed = _speed / 2.5f;
        }
        }

    private void UpdateTarget()
    {
        if (ChainManager.Instance != null && ChainManager.Instance.HasFollowers)
            _target = ChainManager.Instance.GetNearestFollower(transform.position);
        else
            _target = _playerTransform != null ? _playerTransform : FindPlayer();
    }

    private Transform FindPlayer()
    {
        var move = Object.FindAnyObjectByType<PlayerMovement>();
        if (move != null) return move.transform;
        var obj = GameObject.FindWithTag("Player");
        return obj != null ? obj.transform : null;
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
