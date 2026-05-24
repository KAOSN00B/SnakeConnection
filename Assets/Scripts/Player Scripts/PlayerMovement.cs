using UnityEngine;

// Handles WASD movement and mouse-aim rotation for the player.
// Input is read every Update(); Rigidbody velocity is applied every FixedUpdate() so physics stays stable.
// Runs at execution order -10 so AimAtMouse() sets the player's Y rotation before
// PlayerFeelController reads it for the body tilt. SpeedMultiplier is set externally by PlayerFeelController.
[DefaultExecutionOrder(-10)] // Must run before PlayerFeelController so AimAtMouse sets Y before the tilt reads it
public class PlayerMovement : MonoBehaviour
{
    // Movement speed exposed in the Inspector — no drag/damping, velocity is set directly
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

    // Raw WASD input values (-1, 0, or 1 per axis)
    private float _horizontalInput;
    private float _verticalInput;

    // The direction the player is moving, normalized so diagonal isn't faster
    private Vector3 _moveDirection;

    // Cached Rigidbody — grabbed once in Awake so we don't call GetComponent every frame
    private Rigidbody _rb;
    // Camera.main does a FindObjectByTag scan every call; cache it once
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

    // ---- SPIN DIAGNOSTICS: remove after profiling ----
    // Logs a summary every 2 seconds — no per-frame spam.
    // Watch for: high max frame time during spinning, rotation writes much higher than frame count.
    private int   _diagRotationWriteCount;
    private float _diagMaxAngleChangePerFrame;
    private float _diagMinFrameTime = float.MaxValue;
    private float _diagMaxFrameTime;
    private float _diagElapsed;
    private Vector3 _diagPreviousAimDirection;
    private bool  _diagHasPreviousAimDirection;
    private const float DiagnosticLogInterval = 2f;
    // ---- END DIAGNOSTICS ----

    private static readonly int MoveXHash = Animator.StringToHash("MoveX");
    private static readonly int MoveZHash = Animator.StringToHash("MoveZ");

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
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
        // Input and aiming run every frame for responsiveness
        GatherInput();
        AimAtMouse();
        UpdateAnimation();
        LogSpinDiagnostics();
    }

    private void UpdateAnimation()
    {
        if (_animator == null) return;

        // Only push speed to the Animator when it changes — Animator.speed is not free
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
        // Rotation applied here via MoveRotation — the correct physics channel.
        // This lets PhysX anticipate the move and update contact pairs efficiently
        // instead of treating it as a surprise teleport that forces a full contact rebuild.
        _rb.MoveRotation(_desiredAimRotation);
        ApplyMovement();
    }

    private void GatherInput()
    {
        // GetAxisRaw returns exactly -1, 0, or 1 — no Unity smoothing/acceleration.
        // This gives tight, immediate response to key presses.
        _horizontalInput = Input.GetAxisRaw("Horizontal"); // A/D or Left/Right
        _verticalInput = Input.GetAxisRaw("Vertical");     // W/S or Up/Down

        Vector3 input = new Vector3(_horizontalInput, 0f, _verticalInput);

        // Only update direction when there is input — this preserves the last known
        // direction so the player doesn't snap back to facing forward when keys are released
        if (input != Vector3.zero)
        {
            // Normalize so moving diagonally (W+D) isn't faster than moving straight
            _moveDirection = input.normalized;
        }
    }

    private void ApplyMovement()
    {
        // Build the target XZ velocity from direction + speed, scaled by PlayerFeelController's multiplier
        Vector3 targetVelocity = _moveDirection * _moveSpeed * SpeedMultiplier;

        // Set velocity directly instead of using AddForce.
        // We preserve the current Y velocity so gravity still applies normally.
        // Overwriting X and Z gives instant, responsive movement with no sliding.
        _rb.linearVelocity = new Vector3(targetVelocity.x, _rb.linearVelocity.y, targetVelocity.z);
    }

    private void AimAtMouse()
    {
        // Right stick takes priority over mouse when it's pushed past the deadzone
        float rightX = Input.GetAxisRaw("RightStickX");
        float rightY = Input.GetAxisRaw("RightStickY");

        if (new Vector2(rightX, rightY).magnitude > _controllerDeadzone)
        {
            // Right stick Y is inverted on most controllers — negate it so up = forward
            Vector3 aimDirection = new Vector3(rightX, 0f, -rightY).normalized;
            StoreAimRotation(aimDirection);
            return;
        }

        // Plane.Raycast is pure math — no physics cost.
        Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
        Plane groundPlane = new Plane(Vector3.up, transform.position);

        if (groundPlane.Raycast(ray, out float distance))
        {
            Vector3 targetPoint = ray.GetPoint(distance);
            Vector3 lookDirection = targetPoint - transform.position;
            lookDirection.y = 0f;

            if (lookDirection.sqrMagnitude > 0.01f)
                StoreAimRotation(lookDirection.normalized);
        }
    }

    // Stores the desired rotation for FixedUpdate to apply via MoveRotation.
    // Also tracks diagnostic data — remove this method and call _desiredAimRotation directly
    // once profiling is done.
    private void StoreAimRotation(Vector3 aimDirection)
    {
        _desiredAimRotation = Quaternion.LookRotation(aimDirection);

        _diagRotationWriteCount++;

        if (_diagHasPreviousAimDirection)
        {
            float angleChange = Vector3.Angle(_diagPreviousAimDirection, aimDirection);
            if (angleChange > _diagMaxAngleChangePerFrame)
                _diagMaxAngleChangePerFrame = angleChange;
        }

        _diagPreviousAimDirection    = aimDirection;
        _diagHasPreviousAimDirection = true;
    }

    // Logs a summary every DiagnosticLogInterval seconds.
    // Key things to watch:
    //   Rotation writes  — should be roughly equal to frame count (60 per second = ~120 per 2s).
    //                       Much higher means something is calling WriteAimRotation extra times.
    //   Max angle/frame  — large values (>10°) confirm fast spinning is the active scenario.
    //   Max frame time   — spikes here during spinning point to a per-frame cost in this path.
    //                       Compare spinning vs idle to isolate the cause.
    private void LogSpinDiagnostics()
    {
        _diagMinFrameTime = Mathf.Min(_diagMinFrameTime, Time.deltaTime);
        _diagMaxFrameTime = Mathf.Max(_diagMaxFrameTime, Time.deltaTime);
        _diagElapsed += Time.deltaTime;

        if (_diagElapsed < DiagnosticLogInterval) return;

        Debug.Log(
            $"[SpinDiag] Rotation writes: {_diagRotationWriteCount} over {_diagElapsed:F2}s | " +
            $"Max spin/frame: {_diagMaxAngleChangePerFrame:F2}° | " +
            $"Frame time — min: {_diagMinFrameTime * 1000f:F2}ms  max: {_diagMaxFrameTime * 1000f:F2}ms"
        );

        _diagRotationWriteCount      = 0;
        _diagMaxAngleChangePerFrame  = 0f;
        _diagElapsed                 = 0f;
        _diagMinFrameTime            = float.MaxValue;
        _diagMaxFrameTime            = 0f;
    }
}
