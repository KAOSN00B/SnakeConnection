using UnityEngine;

public class FollowerMovement : MonoBehaviour
{
    [SerializeField] private float _rotationSpeed = 10f;

    private void Start()
    {
        if (ChainManager.Instance != null)
        {
            ChainManager.Instance.AddFollower(this);
        }
    }

    // Index into the position history — set by ChainManager on spawn
    private int _historyIndex;

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
