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
        _playerTransform = FindPlayer();
        UpdateTarget();
    }

    private void Update()
    {
        if (_playerTransform == null) _playerTransform = FindPlayer();

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
        Transform player = _playerTransform != null ? _playerTransform : FindPlayer();

        if (ChainManager.Instance == null || !ChainManager.Instance.HasFollowers)
        {
            _target = player;
            return;
        }

        Transform tail = ChainManager.Instance.GetTailFollower();
        _target = tail != null ? tail : player;
    }

    private Transform FindPlayer()
    {
        var move = Object.FindAnyObjectByType<PlayerMovement>();
        if (move != null) return move.transform;
        var obj = GameObject.FindWithTag("Player");
        return obj != null ? obj.transform : null;
    }

    private void FaceTarget()
    {
        if (_directionToTarget != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(_directionToTarget);
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
