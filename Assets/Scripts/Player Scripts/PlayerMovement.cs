using UnityEngine;

// Handles WASD movement and mouse-aim rotation for the player.
// Input is read every Update(); Rigidbody velocity is applied every FixedUpdate() so physics stays stable.
// Runs at execution order -10 so AimAtMouse() sets the player's Y rotation before
// PlayerFeelController reads it for the body tilt. SpeedMultiplier is set externally by PlayerFeelController.
[DefaultExecutionOrder(-10)]
public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float _moveSpeed;

    [Tooltip("Right stick must exceed this magnitude before controller aiming activates.")]
    [SerializeField] private float _controllerDeadzone = 0.2f;

    [Header("Animation Performance")]
    [Tooltip("Smallest change in MoveX/MoveZ blend values before Animator.SetFloat is called. " +
             "When mouse aim rotates the player, local movement direction changes every frame even if " +
             "the player is moving in the same world direction — throttling avoids constant Animator churn.")]
    [SerializeField] private float _animationParameterChangeThreshold = 0.02f;
    [Tooltip("Smallest SpeedMultiplier change before Animator.speed is updated.")]
    [SerializeField] private float _animatorSpeedChangeThreshold = 0.01f;

    // Set by PlayerFeelController each frame — 1.0 normally, ramps to 1.3 over 90 seconds,
    // briefly spikes higher on sharp direction changes
    public float SpeedMultiplier { get; set; } = 1f;

    private float _horizontalInput;
    private float _verticalInput;
    private Vector3 _moveDirection;

    private Rigidbody _rb;
    private Camera _mainCamera;
    private Animator _animator;

    // Desired rotation calculated in Update, applied via MoveRotation in FixedUpdate.
    // Setting transform.rotation directly on a non-kinematic Rigidbody is a physics teleport —
    // PhysX rebuilds all contact pairs for the moved collider every physics step, causing
    // cascading frame spikes when spinning fast. MoveRotation tells PhysX the rotation
    // is intentional, so it handles contacts efficiently instead of rebuilding from scratch.
    private Quaternion _desiredAimRotation;

    // Last written animation values — used to skip SetFloat calls when values barely changed
    private float _lastMoveX;
    private float _lastMoveZ;
    private float _lastAnimatorSpeed;

    private static readonly int MoveXHash = Animator.StringToHash("MoveX");
    private static readonly int MoveZHash = Animator.StringToHash("MoveZ");

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        // Force non-kinematic — PlayerFeelController sets isKinematic = true on death,
        // and if the scene loads while that state is saved the player can't move.
        _rb.isKinematic = false;
        _mainCamera = Camera.main;
        _animator = GetComponentInChildren<Animator>();
        _desiredAimRotation = transform.rotation;
    }

    void Start()
    {
        // Freeze X and Z rotation so physics collisions can't tip the player over.
        // Y rotation stays free because AimAtMouse() rotates the player to face the cursor.
        _rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }

    void Update()
    {
        GatherInput();
        AimAtMouse();
        UpdateAnimation();
    }

    private void UpdateAnimation()
    {
        if (_animator == null) return;

        if (Mathf.Abs(SpeedMultiplier - _lastAnimatorSpeed) > _animatorSpeedChangeThreshold)
        {
            _animator.speed = SpeedMultiplier;
            _lastAnimatorSpeed = SpeedMultiplier;
        }

        // InverseTransformDirection recalculates local movement every frame.
        // When mouse aim rotates the player, local direction changes even if the player is
        // moving in the same world direction — skip SetFloat when the change is too small
        // to visibly affect animation blending.
        Vector3 localMove = transform.InverseTransformDirection(_moveDirection);

        if (Mathf.Abs(localMove.x - _lastMoveX) > _animationParameterChangeThreshold)
        {
            _animator.SetFloat(MoveXHash, localMove.x);
            _lastMoveX = localMove.x;
        }

        if (Mathf.Abs(localMove.z - _lastMoveZ) > _animationParameterChangeThreshold)
        {
            _animator.SetFloat(MoveZHash, localMove.z);
            _lastMoveZ = localMove.z;
        }
    }

    void FixedUpdate()
    {
        _rb.MoveRotation(_desiredAimRotation);
        // Zero angular velocity so enemy/follower collisions can't spin the player —
        // only MoveRotation should control Y rotation.
        _rb.angularVelocity = Vector3.zero;
        ApplyMovement();
    }

    private void GatherInput()
    {
        _horizontalInput = Input.GetAxisRaw("Horizontal");
        _verticalInput   = Input.GetAxisRaw("Vertical");

        Vector3 input = new Vector3(_horizontalInput, 0f, _verticalInput);

        // Only update direction when there is input — this preserves the last known
        // direction so the player doesn't snap back to facing forward when keys are released
        if (input != Vector3.zero)
            _moveDirection = input.normalized;
    }

    private void ApplyMovement()
    {
        Vector3 targetVelocity = _moveDirection * _moveSpeed * SpeedMultiplier;

        // Set velocity directly instead of using AddForce.
        // We preserve the current Y velocity so gravity still applies normally.
        _rb.linearVelocity = new Vector3(targetVelocity.x, _rb.linearVelocity.y, targetVelocity.z);
    }

    private void AimAtMouse()
    {
        // Right stick takes priority over mouse when it's pushed past the deadzone
        float rightX = Input.GetAxisRaw("RightStickX");
        float rightY = Input.GetAxisRaw("RightStickY");

        if (new Vector2(rightX, rightY).magnitude > _controllerDeadzone)
        {
            Vector3 aimDirection = new Vector3(rightX, 0f, -rightY).normalized;
            _desiredAimRotation = Quaternion.LookRotation(aimDirection);
            return;
        }

        // Plane.Raycast is pure math — no physics cost.
        Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
        Plane groundPlane = new Plane(Vector3.up, transform.position);

        if (groundPlane.Raycast(ray, out float distance))
        {
            Vector3 targetPoint   = ray.GetPoint(distance);
            Vector3 lookDirection = targetPoint - transform.position;
            lookDirection.y = 0f;

            if (lookDirection.sqrMagnitude > 0.01f)
                _desiredAimRotation = Quaternion.LookRotation(lookDirection.normalized);
        }
    }
}
