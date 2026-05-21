using UnityEngine;

[DefaultExecutionOrder(-10)] // Must run before PlayerFeelController so AimAtMouse sets Y before the tilt reads it
public class PlayerMovement : MonoBehaviour
{
    // Movement speed exposed in the Inspector — no drag/damping, velocity is set directly
    [SerializeField] private float _moveSpeed;

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

    private static readonly int MoveXHash = Animator.StringToHash("MoveX");
    private static readonly int MoveZHash = Animator.StringToHash("MoveZ");

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _mainCamera = Camera.main;
        _animator = GetComponentInChildren<Animator>();
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
    }

    private void UpdateAnimation()
    {
        if (_animator == null) return;

        _animator.speed = SpeedMultiplier;

        // Calculate movement relative to facing direction for strafing
        Vector3 localMove = transform.InverseTransformDirection(_moveDirection);
        _animator.SetFloat(MoveXHash, localMove.x);
        _animator.SetFloat(MoveZHash, localMove.z);
    }

    void FixedUpdate()
    {
        // Movement is applied in FixedUpdate because it touches the Rigidbody.
        // Physics runs on a fixed timestep, so velocity changes here stay stable.
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
        Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
        Plane groundPlane = new Plane(Vector3.up, transform.position);

        if (groundPlane.Raycast(ray, out float distance))
        {
            Vector3 targetPoint = ray.GetPoint(distance);
            Vector3 lookDir = targetPoint - transform.position;
            lookDir.y = 0f;

            if (lookDir.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.LookRotation(lookDir);
        }
    }
}
