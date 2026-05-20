using UnityEngine;

public class FollowerMovement : MonoBehaviour
{
    [SerializeField] private float _rotationSpeed = 10f;

    // Index into the position history — set by ChainManager on spawn
    private int _historyIndex;

    // Cached once in Awake — avoids TryGetComponent every FixedUpdate
    private FolloweAttack _attack;
    private Animator _animator;
    private PlayerMovement _player;

    private void Awake()
    {
        _attack = GetComponent<FolloweAttack>();
        _animator = GetComponentInChildren<Animator>();
        _player = Object.FindAnyObjectByType<PlayerMovement>();
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
        UpdatePosition();

        if (_animator != null)
        {
            // If we can't find the player or they don't have a multiplier, default to 1.
            float speed = (_player != null) ? _player.SpeedMultiplier : 1f;
            // Baseline 1.0 speed for followers, scaled by the game's speed escalation.
            _animator.speed = speed;
        }
    }

    private void UpdatePosition()
    {
        Vector3 targetPos = ChainManager.Instance.GetHistoryPosition(_historyIndex);
        FaceMovementDirection(targetPos);
        // Snap to the history point — smoothness comes from the dense recording, not interpolation
        transform.position = targetPos;
    }

    private void FaceMovementDirection(Vector3 targetPos)
    {
        // Don't rotate to face movement if we have an enemy target
        if (_attack != null && _attack.HasTarget) return;

        // Rotate to face the direction of travel before moving
        Vector3 moveDir = targetPos - transform.position;
        if (moveDir.sqrMagnitude > 0.001f)
        {
            moveDir.y = 0f;
            Quaternion targetRot = Quaternion.LookRotation(moveDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.fixedDeltaTime * _rotationSpeed);
        }
    }
}
