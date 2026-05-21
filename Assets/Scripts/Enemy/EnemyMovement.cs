using UnityEngine;

// Basic melee enemy AI: moves toward the nearest Follower, or the Player if no followers exist.
// Registers with EnemyRegistry on enable so FollowerAttack scripts can find it efficiently.
// Target is refreshed on a timer to avoid a full follower list walk every physics tick.
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
        Vector3 flatDirection = _target.position - transform.position;
        flatDirection.y = 0f;

        if (flatDirection.sqrMagnitude < 0.01f) return;

        Vector3 moveDirection = flatDirection.normalized;
        transform.position += moveDirection * _speed * Time.fixedDeltaTime;
        transform.rotation = Quaternion.LookRotation(moveDirection);
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

    private void ProcessContact(GameObject hitObject)
    {
        // Walk up the hierarchy — the collider is often on a child object, not the root
        // where the Health component and tag actually live
        IDamageable damageable = hitObject.GetComponentInParent<IDamageable>();
        if (damageable == null) return;

        GameObject damageableObject = ((MonoBehaviour)damageable).gameObject;
        if (!damageableObject.CompareTag("Player") && !damageableObject.CompareTag("Follower")) return;

        if (Time.time < _nextAttackTime) return;
        damageable.TakeDamage(_damage);
        _nextAttackTime = Time.time + _attackCooldown;
    }
}
