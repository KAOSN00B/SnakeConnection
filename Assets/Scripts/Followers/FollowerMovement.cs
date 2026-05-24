using UnityEngine;

public class FollowerMovement : MonoBehaviour
{
    [SerializeField] private float _rotationSpeed = 10f;

    // Index into the position history — set by ChainManager on spawn
    private int _historyIndex;

    [Header("Contact Damage")]
    [Tooltip("Radius used to check for overlapping enemies each physics tick.")]
    [SerializeField] private float _contactRadius = 0.6f;
    [Tooltip("Seconds before this follower can take contact damage again.")]
    [SerializeField] private float _contactDamageCooldown = 2f;

    // Shared buffer across all followers — NonAlloc avoids a new array allocation each check
    private static readonly Collider[] _contactBuffer = new Collider[8];

    [SerializeField] private float _snapThreshold = 0.5f;
    [SerializeField] private float _returnSpeed = 10f;
    [Tooltip("Max speed when a follower is far behind — scales up with distance.")]
    [SerializeField] private float _catchUpSpeed = 18f;
    [Tooltip("Distance at which full catch-up speed kicks in.")]
    [SerializeField] private float _catchUpDistance = 3f;

    // Cached once in Awake — avoids TryGetComponent every FixedUpdate
    private Animator _animator;
    private PlayerMovement _player;
    private Health _health;
    private float _nextContactTime;

    private static readonly int MoveXHash = Animator.StringToHash("MoveX");
    private static readonly int MoveZHash = Animator.StringToHash("MoveZ");

    private void Awake()
    {
        _animator = GetComponentInChildren<Animator>();
        _player = Object.FindAnyObjectByType<PlayerMovement>();
        _health = GetComponent<Health>();
    }

    private void Start()
    {
        if (ChainManager.Instance != null)
            ChainManager.Instance.AddFollower(this);
    }

    // Called by ChainManager immediately after the follower is registered
    public void Init(int chainIndex, int pointsPerFollower)
    {
        _historyIndex = chainIndex * pointsPerFollower;
    }

    private void OnDestroy()
    {
        // Notify ChainManager so it can remove this follower and re-index the rest
        ChainManager.Instance?.RemoveFollower(this);
    }

    private void FixedUpdate()
    {
        if (ChainManager.Instance == null) return;

        Vector3 lastPos = transform.position;
        UpdatePosition();
        UpdateAnimation(lastPos);
        CheckEnemyContact();
    }

    // OverlapSphere is more reliable than OnTriggerStay here because enemies move via
    // transform.position with no Rigidbody, making them static colliders that Unity's
    // trigger system doesn't detect consistently against kinematic Rigidbodies.
    private void CheckEnemyContact()
    {
        if (_health == null || Time.time < _nextContactTime) return;

        int count = Physics.OverlapSphereNonAlloc(transform.position, _contactRadius, _contactBuffer);
        for (int i = 0; i < count; i++)
        {
            if (_contactBuffer[i].CompareTag("Enemy"))
            {
                _health.TakeDamage(1);
                _nextContactTime = Time.time + _contactDamageCooldown;
                break;
            }
        }
    }

    private void UpdateAnimation(Vector3 lastPos)
    {
        if (_animator == null) return;

        // Scale animator speed with player speed escalation
        float multiplier = (_player != null) ? _player.SpeedMultiplier : 1f;
        _animator.speed = multiplier;

        // Calculate actual move direction this frame
        Vector3 moveDelta = transform.position - lastPos;
        if (moveDelta.sqrMagnitude > 0.0001f)
        {
            Vector3 moveDir = moveDelta.normalized;
            Vector3 localMove = transform.InverseTransformDirection(moveDir);
            _animator.SetFloat(MoveXHash, localMove.x);
            _animator.SetFloat(MoveZHash, localMove.z);
        }
        else
        {
            _animator.SetFloat(MoveXHash, 0f);
            _animator.SetFloat(MoveZHash, 0f);
        }
    }

    private void UpdatePosition()
    {
        Vector3 targetPos = ChainManager.Instance.GetHistoryPosition(_historyIndex);

        // Only drive X/Z — Y is left to the Rigidbody and floor collider, same as the player
        float horizontalDistance = new Vector2(
            transform.position.x - targetPos.x,
            transform.position.z - targetPos.z).magnitude;

        Vector3 horizontalTarget = new Vector3(targetPos.x, transform.position.y, targetPos.z);
        Vector3 positionBeforeMove = transform.position;

        if (horizontalDistance > _snapThreshold)
        {
            // Scale speed with distance — further behind = faster catch-up, capped at _catchUpSpeed
            float catchUpFactor = Mathf.Clamp01(horizontalDistance / _catchUpDistance);
            float speed = Mathf.Lerp(_returnSpeed, _catchUpSpeed, catchUpFactor);
            transform.position = Vector3.MoveTowards(transform.position, horizontalTarget, speed * Time.fixedDeltaTime);
        }
        else
            transform.position = horizontalTarget;

        // Rotate based on actual movement delta — using targetPos direction gives zero on snap,
        // which locked followers to one facing direction
        FaceMovementDirection(transform.position - positionBeforeMove);
    }

    private void FaceMovementDirection(Vector3 moveDelta)
    {
        moveDelta.y = 0f;
        if (moveDelta.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(moveDelta);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.fixedDeltaTime * _rotationSpeed);
        }
    }
}
