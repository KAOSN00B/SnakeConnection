using UnityEngine;

// Enemy that ignores combat entirely — its only goal is to steal a follower and escape.
//
// Approach phase: moves toward the tail follower. If no followers exist it targets the player
// but the grab will never trigger on the player, so it just circles harmlessly.
//
// Flee phase: runs away from the player using a combination of two steering forces:
//   1. Flee force  — direction directly away from the player
//   2. Wall force  — a raycast detects nearby walls and pushes the hacker perpendicular to them
// The two forces are added together so the hacker naturally slides along walls instead of getting stuck.
//
// The KidnappedFollower property is read by HackerDeathFeedback on death to trigger a rescue.
public class HackerMovement : MonoBehaviour
{
    [Header("Movement")]
    [Tooltip("Speed during the approach phase — slow enough for the player to notice.")]
    [SerializeField] private float _approachSpeed = 6f;

    [Tooltip("Speed during the flee phase — fast enough to create urgency.")]
    [SerializeField] private float _fleeSpeed = 8f;

    [Tooltip("Distance from the tail follower at which the grab triggers.")]
    [SerializeField] private float _grabRadius = 1.5f;

    [Header("Wall Avoidance")]
    [Tooltip("How far ahead to raycast for walls during the flee phase.")]
    [SerializeField] private float _wallDetectionRadius = 2f;

    [Tooltip("How strongly the wall normal pushes the hacker sideways. Higher = tighter wall-sliding.")]
    [SerializeField] private float _wallAvoidanceStrength = 2f;

    [Header("Movement Smoothing")]
    [Tooltip("How fast the hacker rotates. Lower values make it turn more like the player/follower.")]
    [SerializeField] private float _rotationSpeed = 8f;

    [Header("Targeting")]
    [Tooltip("Seconds between each tail target refresh. Lower = more responsive but more expensive.")]
    [SerializeField] private float _targetRefreshInterval = 0.2f;

    [Header("Score")]
    [SerializeField] private int _scoreWorth = 100;
    public int ScoreWorth => _scoreWorth;

    [Header("Player Contact Damage")]
    [Tooltip("Damage dealt to the player on contact.")]
    [SerializeField] private int _contactDamage = 1;
    [Tooltip("Seconds before the hacker can deal contact damage to the player again.")]
    [SerializeField] private float _attackCooldown = 1f;

    // Read by HackerDeathFeedback on death to trigger a rescue if the follower is still alive
    public KidnappedFollower KidnappedFollower { get; private set; }

    private enum HackerPhase { Approach, Flee }
    private HackerPhase _currentPhase = HackerPhase.Approach;

    private Transform _tailTarget;
    private Transform _playerTransform;

    // Calculated in Update, applied in FixedUpdate — same pattern as TailChaserMovement
    private Vector3 _currentMoveDirection;
    private float _targetRefreshTimer;
    private float _nextAttackTime;
    private Rigidbody _rb;
    private Quaternion _desiredRotation;

    private void OnEnable()  => EnemyRegistry.Register(transform);
    private void OnDisable() => EnemyRegistry.Unregister(transform);

    private void Start()
    {
        _playerTransform = FindPlayer();
        RefreshTailTarget();
        _rb = transform.root.GetComponent<Rigidbody>();
        if (_rb == null)
            _rb = transform.root.gameObject.AddComponent<Rigidbody>();
        _rb.isKinematic = true;
        _desiredRotation = transform.root.rotation;
    }

    private void Update()
    {
        if (_playerTransform == null) _playerTransform = FindPlayer();

        // Only refresh the tail target during approach — once fleeing we don't need a target
        _targetRefreshTimer -= Time.deltaTime;
        if (_targetRefreshTimer <= 0f)
        {
            if (_currentPhase == HackerPhase.Approach)
                RefreshTailTarget();
            _targetRefreshTimer = _targetRefreshInterval;
        }

        // Calculate move direction each frame so FixedUpdate always has a fresh value to apply
        _currentMoveDirection = _currentPhase == HackerPhase.Approach
            ? CalculateApproachDirection()
            : CalculateFleeDirection();

        FaceDirection(_currentMoveDirection);
    }

    private void FixedUpdate()
    {
        if (_currentMoveDirection == Vector3.zero) return;

        float speed = _currentPhase == HackerPhase.Approach ? _approachSpeed : _fleeSpeed;

        // MovePosition/MoveRotation tell PhysX the move is intentional — avoids contact pair rebuilds.
        _rb.MovePosition(transform.root.position + _currentMoveDirection * speed * Time.fixedDeltaTime);
        _rb.MoveRotation(_desiredRotation);

        // Only check for a grab during the approach — once fleeing the grab is already done
        if (_currentPhase == HackerPhase.Approach)
            CheckGrab();
    }

    // Called by KidnappedFollower when the 5-second timer expires.
    // Resets the hacker to hunt the next tail immediately.
    public void OnKidnappedFollowerLost()
    {
        KidnappedFollower = null;
        _currentPhase = HackerPhase.Approach;
        RefreshTailTarget();
    }

    private void CheckGrab()
    {
        if (_tailTarget == null) return;

        // Never grab the player — hacker only steals followers
        if (_tailTarget.CompareTag("Player")) return;

        // Use a 2D distance check (ignoring Y) to handle cases where models have different pivots
        Vector3 hackerFlatPosition   = new Vector3(transform.position.x, 0f, transform.position.z);
        Vector3 followerFlatPosition = new Vector3(_tailTarget.position.x, 0f, _tailTarget.position.z);

        float distanceToTail = Vector3.Distance(hackerFlatPosition, followerFlatPosition);
        if (distanceToTail > _grabRadius) return;

        // Close enough — initiate the kidnap sequence. Check children too.
        KidnappedFollower kidnappedFollower = _tailTarget.GetComponentInChildren<KidnappedFollower>();
        if (kidnappedFollower == null) return;

        // If another hacker already grabbed this follower, don't run away.
        // Instead, clear target and find a new one.
        if (kidnappedFollower.IsKidnapped)
        {
            _tailTarget = null;
            RefreshTailTarget();
            return;
        }

        KidnappedFollower = kidnappedFollower;
        KidnappedFollower.GetKidnapped(this);

        // Switch to flee immediately — job done, now escape
        _currentPhase = HackerPhase.Flee;
        _tailTarget = null;
    }

    private Vector3 CalculateApproachDirection()
    {
        if (_tailTarget == null) return Vector3.zero;

        Vector3 directionToTail = _tailTarget.position - transform.position;
        directionToTail.y = 0f;
        if (directionToTail.sqrMagnitude < 0.01f) return Vector3.zero;
        return directionToTail.normalized;
    }

    private Vector3 CalculateFleeDirection()
    {
        if (_playerTransform == null) return transform.forward;

        // Primary steering force: move directly away from the player
        Vector3 fleeDirection = transform.position - _playerTransform.position;
        fleeDirection.y = 0f;
        if (fleeDirection.sqrMagnitude < 0.01f) fleeDirection = transform.forward;
        fleeDirection = fleeDirection.normalized;

        // Wall avoidance force: cast a ray in the current flee direction.
        // If a wall is detected, the wall's normal pushes the hacker sideways.
        // This causes it to naturally slide along the wall rather than running straight into it.
        Vector3 wallAvoidanceForce = Vector3.zero;
        if (Physics.Raycast(transform.position, fleeDirection, out RaycastHit wallHit, _wallDetectionRadius))
        {
            wallAvoidanceForce = wallHit.normal * _wallAvoidanceStrength;
            wallAvoidanceForce.y = 0f;
        }

        // Combine both forces and normalize so the final speed stays consistent regardless of wall angle
        Vector3 combinedDirection = (fleeDirection + wallAvoidanceForce).normalized;
        return combinedDirection;
    }

    private void RefreshTailTarget()
    {
        // Target the tail follower — the most exposed and farthest from the player
        if (ChainManager.Instance != null && ChainManager.Instance.HasFollowers)
            _tailTarget = ChainManager.Instance.GetTailFollower();
        else
            _tailTarget = _playerTransform != null ? _playerTransform : FindPlayer();
    }

    private void FaceDirection(Vector3 direction)
    {
        if (direction.sqrMagnitude > 0.01f)
        {
            Quaternion targetRot = Quaternion.LookRotation(direction);
            _desiredRotation = Quaternion.Slerp(transform.root.rotation, targetRot, Time.deltaTime * _rotationSpeed);
        }
    }

    // The hacker bumps the player if they get too close — it's trying to escape, not fight,
    // but running into it still hurts. Followers are intentionally excluded here since the
    // hacker steals them rather than damaging them.
    private void OnCollisionStay(Collision collision) => ProcessPlayerContact(collision.gameObject);
    private void OnTriggerStay(Collider other) => ProcessPlayerContact(other.gameObject);

    private void ProcessPlayerContact(GameObject hitObject)
    {
        // Walk up the hierarchy in case the collider is on a child object
        IDamageable damageable = hitObject.GetComponentInParent<IDamageable>();
        if (damageable == null) return;

        // Check the tag on the root object where Health/IDamageable usually lives,
        // or on the hit object itself as a fallback.
        GameObject damageableObject = ((MonoBehaviour)damageable).gameObject;
        if (!damageableObject.CompareTag("Player") && !hitObject.CompareTag("Player")) return;

        if (Time.time < _nextAttackTime) return;
        damageable.TakeDamage(_contactDamage);
        _nextAttackTime = Time.time + _attackCooldown;
    }

    private Transform FindPlayer()
    {
        var playerMovement = Object.FindAnyObjectByType<PlayerMovement>();
        if (playerMovement != null) return playerMovement.transform;
        var playerObject = GameObject.FindWithTag("Player");
        return playerObject != null ? playerObject.transform : null;
    }
}
