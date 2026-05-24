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

    [Header("Separation")]
    [Tooltip("Enemies within this radius push each other apart.")]
    [SerializeField] private float _separationRadius = 1.5f;
    [Tooltip("How strongly nearby enemies are pushed apart. 1–2 is subtle; 3+ is very spread out.")]
    [SerializeField] private float _separationStrength = 1.5f;
    [Tooltip("How quickly the enemy turns toward its desired direction. Lower = wider, smoother arc.")]
    [SerializeField] private float _turnSpeed = 6f;

    private static readonly Collider[] _separationBuffer = new Collider[16];
    private Vector3 _smoothedMoveDir;

    private Transform _playerTransform;
    private Transform _target;
    private float _nextAttackTime;
    private Vector3 _directionToTarget;
    private float _targetRefreshTimer;
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

        Vector3 flatDirection = _target.position - transform.position;
        flatDirection.y = 0f;
        _directionToTarget = flatDirection.normalized;

        FaceTarget();
    }

    private void FixedUpdate()
    {
        if (_target == null) return;

        Vector3 desired = _directionToTarget + CalculateSeparation() * _separationStrength;
        desired.y = 0f;
        if (desired.sqrMagnitude < 0.01f) desired = _directionToTarget;
        else desired = desired.normalized;

        _smoothedMoveDir = Vector3.Slerp(_smoothedMoveDir, desired, Time.fixedDeltaTime * _turnSpeed);

        transform.position += _smoothedMoveDir * _speed * Time.fixedDeltaTime;
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
