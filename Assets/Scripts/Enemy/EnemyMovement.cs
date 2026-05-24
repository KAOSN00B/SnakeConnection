using UnityEngine;

// Basic melee enemy AI: moves toward the nearest Follower, or the Player if no followers exist.
// Registers with EnemyRegistry on enable so FollowerAttack scripts can find it efficiently.
// Target is refreshed on a timer to avoid a full follower list walk every physics tick.
public class EnemyMovement : MonoBehaviour
{
    [SerializeField] private float _speed = 7f;
    [SerializeField] private int _damage = 1;
    [SerializeField] private float _attackCooldown = 1f;
    [SerializeField] private int _scoreWorth = 50;
    public int ScoreWorth => _scoreWorth;

    [SerializeField] private float _targetRefreshInterval = 0.2f;

    [Header("Separation")]
    [Tooltip("Enemies within this radius push each other apart.")]
    [SerializeField] private float _separationRadius = 1.5f;
    [Tooltip("How strongly nearby enemies are pushed apart. 1–2 is subtle; 3+ is very spread out.")]
    [SerializeField] private float _separationStrength = 1.5f;
    [Tooltip("How quickly the enemy turns toward its desired direction. Lower = wider, smoother arc; higher = snappier.")]
    [SerializeField] private float _turnSpeed = 6f;

    private static readonly Collider[] _separationBuffer = new Collider[16];
    private Vector3 _smoothedMoveDir;

    private Transform _playerTransform;
    private Transform _target;
    private float _nextAttackTime;
    private float _targetRefreshTimer;
    private Animator _animator;
    private Rigidbody _rb;

    private void OnEnable()  => EnemyRegistry.Register(transform);
    private void OnDisable() => EnemyRegistry.Unregister(transform);

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        if (_rb == null)
            _rb = gameObject.AddComponent<Rigidbody>();
        _rb.isKinematic = true;
    }

    private void Start()
    {
        _playerTransform = FindPlayer();
        UpdateTarget();
        _smoothedMoveDir = transform.forward;
        _animator = GetComponentInChildren<Animator>();
        if (_animator != null)
            _animator.speed = _speed / 2.5f;
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

        // Blend chase direction with separation, then smoothly rotate toward it so
        // enemies curve around each other instead of snapping — prevents the visual bump
        Vector3 desired = flatDirection.normalized + CalculateSeparation() * _separationStrength;
        desired.y = 0f;
        if (desired.sqrMagnitude < 0.01f) desired = flatDirection.normalized;
        else desired = desired.normalized;

        _smoothedMoveDir = Vector3.Slerp(_smoothedMoveDir, desired, Time.fixedDeltaTime * _turnSpeed);

        transform.position += _smoothedMoveDir * _speed * Time.fixedDeltaTime;
        transform.rotation  = Quaternion.LookRotation(_smoothedMoveDir);
    }

    private void UpdateTarget()
    {
        if (ChainManager.Instance != null && ChainManager.Instance.HasFollowers)
            _target = ChainManager.Instance.GetNearestFollower(transform.position);
        else
            _target = _playerTransform != null ? _playerTransform : FindPlayer();
    }

    private Vector3 CalculateSeparation()
    {
        Vector3 separation = Vector3.zero;
        int count = Physics.OverlapSphereNonAlloc(transform.position, _separationRadius, _separationBuffer);
        for (int i = 0; i < count; i++)
        {
            if (_separationBuffer[i].gameObject == gameObject) continue;
            if (!_separationBuffer[i].CompareTag("Enemy")) continue;

            Vector3 away = transform.position - _separationBuffer[i].transform.position;
            away.y = 0f;
            float dist = away.magnitude;
            if (dist < 0.001f) continue;
            // Inverse distance: closer enemies exert a stronger push
            separation += away.normalized / Mathf.Max(dist, 0.1f);
        }
        return separation;
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
