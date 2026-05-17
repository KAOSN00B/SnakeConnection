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

    void Start()
    {
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
            _playerTransform = player.transform;
    }

    void Update()
    {
        UpdateTarget();
        if (_target == null) return;

        _directionToTarget = (_target.position - transform.position).normalized;
        MoveTowardTarget();
        FaceTarget();
    }

    private void UpdateTarget()
    {
        // Chase the nearest follower if any exist, otherwise go for the player
        if (ChainManager.Instance != null && ChainManager.Instance.HasFollowers)
            _target = ChainManager.Instance.GetNearestFollower(transform.position);
        else
            _target = _playerTransform;
    }

    private void MoveTowardTarget()
    {
        transform.position += _directionToTarget * _speed * Time.deltaTime;
    }

    private void FaceTarget()
    {
        // Rotate to face the target on the Y axis
        Vector3 flatDir = _directionToTarget;
        flatDir.y = 0f;
        if (flatDir != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(flatDir);
    }

    private void OnCollisionStay(Collision collision)
    {
        ProcessContact(collision.gameObject);
    }

    private void OnTriggerStay(Collider other)
    {
        ProcessContact(other.gameObject);
    }

    private void ProcessContact(GameObject target)
    {
        // Damage both the player and followers on contact
        if (!target.CompareTag("Player") && !target.CompareTag("Follower"))
            return;

        if (Time.time < _nextAttackTime)
            return;

        IDamageable damageable = target.GetComponent<IDamageable>();
        if (damageable != null)
        {
            damageable.TakeDamage(_damage);
            _nextAttackTime = Time.time + _attackCooldown;
        }
    }
    }
