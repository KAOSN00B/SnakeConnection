using UnityEngine;

public class EnemyMovement : MonoBehaviour
{
    [SerializeField] private float _speed = 5f;
    [SerializeField] private int _damage = 1;
    [SerializeField] private float _attackCooldown = 1f;

    private Transform _playerTransform;
    private Transform _target;
    private float _nextAttackTime;
    private Vector3 _directionToTarget;
    private Rigidbody _rb;
    private float _lastTargetLog;

    void Start()
    {
        _rb = GetComponent<Rigidbody>();
        FindPlayer();
    }

    private void FindPlayer()
    {
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
            _playerTransform = player.transform;
    }

    void Update()
    {
        UpdateTarget();
        
        if (_target == null) 
        {
            _directionToTarget = Vector3.zero;
            return;
        }

        Vector3 flat = _target.position - transform.position;
        flat.y = 0f;
        if (flat.sqrMagnitude > 0.01f)
            _directionToTarget = flat.normalized;
        else
            _directionToTarget = Vector3.zero;

        FaceTarget();
        
        if (Time.time > _lastTargetLog + 5f)
        {
            Debug.Log($"[{gameObject.name}] Chasing: " + (_target != null ? _target.name : "NULL"));
            _lastTargetLog = Time.time;
        }
    }

    void FixedUpdate()
    {
        MoveTowardTarget();
    }

    private void UpdateTarget()
    {
        if (_playerTransform == null) FindPlayer();

        Transform nearestFollower = null;
        if (ChainManager.Instance != null && ChainManager.Instance.HasFollowers)
        {
            nearestFollower = ChainManager.Instance.GetNearestFollower(transform.position);
        }

        if (nearestFollower != null)
            _target = nearestFollower;
        else
            _target = _playerTransform;
    }

    private void MoveTowardTarget()
    {
        if (_directionToTarget == Vector3.zero && (_rb == null || _rb.isKinematic)) return;

        if (_rb != null)
        {
            if (_rb.isKinematic)
            {
                transform.position += _directionToTarget * _speed * Time.fixedDeltaTime;
            }
            else
            {
                Vector3 vel = _directionToTarget * _speed;
                vel.y = _rb.linearVelocity.y;
                _rb.linearVelocity = vel;
            }
        }
        else
        {
            transform.position += _directionToTarget * _speed * Time.fixedDeltaTime;
        }
    }

    private void FaceTarget()
    {
        if (_directionToTarget != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(_directionToTarget);
    }

    private void OnCollisionStay(Collision collision) { ProcessContact(collision.gameObject); }
    private void OnTriggerStay(Collider other) { ProcessContact(other.gameObject); }

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