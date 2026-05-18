using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    // Movement speed exposed in the Inspector — no drag/damping, velocity is set directly
    [SerializeField] private float _moveSpeed;

    // Raw WASD input values (-1, 0, or 1 per axis)
    private float _horizontalInput;
    private float _verticalInput;

    // The direction the player is moving, normalized so diagonal isn't faster
    private Vector3 _moveDirection;

    // Cached Rigidbody — grabbed once in Awake so we don't call GetComponent every frame
    private Rigidbody _rb;

    private void Awake()
    {
        // Cache the Rigidbody on the same GameObject this script is attached to
        _rb = GetComponent<Rigidbody>();
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
        // Build the target XZ velocity from direction + speed
        Vector3 targetVelocity = _moveDirection * _moveSpeed;

        // Set velocity directly instead of using AddForce.
        // We preserve the current Y velocity so gravity still applies normally.
        // Overwriting X and Z gives instant, responsive movement with no sliding.
        _rb.linearVelocity = new Vector3(targetVelocity.x, _rb.linearVelocity.y, targetVelocity.z);
    }

    private void AimAtMouse()
    {
        // Cast a ray from the camera through the mouse cursor position in screen space
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        // Create an invisible horizontal plane at the player's Y position.
        // This is what the ray intersects to find the world point the mouse is hovering over.
        Plane groundPlane = new Plane(Vector3.up, transform.position);

        if (groundPlane.Raycast(ray, out float distance))
        {
            // Get the exact world position the mouse is pointing at on the ground plane
            Vector3 targetPoint = ray.GetPoint(distance);

            // Direction from the player to the mouse world position
            Vector3 lookDir = targetPoint - transform.position;

            // Zero out Y so the player only rotates horizontally — no tilting up/down
            lookDir.y = 0f;

            // sqrMagnitude avoids a square root and is faster than magnitude.
            // The 0.01f threshold prevents jittery rotation when the mouse is almost on the player.
            if (lookDir.sqrMagnitude > 0.01f)
            {
                // Rotate the player so its forward (Z) axis points toward the mouse
                transform.rotation = Quaternion.LookRotation(lookDir);
            }
        }
    }
}
