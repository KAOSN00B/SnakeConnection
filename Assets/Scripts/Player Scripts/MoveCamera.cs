using System.Collections;
using UnityEngine;

public class MoveCamera : MonoBehaviour
{
    [SerializeField] private Transform cameraPosition;
    [SerializeField] private Vector3 offset;
    [SerializeField] private Vector3 cameraRotation;
    [SerializeField] private Transform orientation;

    // Additive offset applied on top of normal follow position during a shake
    private Vector3 _shakeOffset;

    // Set each frame by PlayerFeelController: forward look-ahead nudge + zoom-out delta
    private Vector3 _dynamicOffset;

    void Update()
    {
        FollowTarget();
        UpdateOrientation();
    }

    private void FollowTarget()
    {
        // _shakeOffset is zero normally; _dynamicOffset is set by PlayerFeelController
        transform.position = cameraPosition.position + offset + _dynamicOffset + _shakeOffset;
        transform.rotation = Quaternion.Euler(cameraRotation);
    }

    // Called every frame by PlayerFeelController to apply forward look-ahead + gradual zoom-out.
    // The value is a DELTA added on top of the serialized 'offset' above, so tweaking offset
    // in the Inspector still works as the home position.
    public void SetDynamicOffset(Vector3 delta)
    {
        _dynamicOffset = delta;
    }

    private void UpdateOrientation()
    {
        if (orientation != null)
            orientation.rotation = Quaternion.Euler(0, transform.eulerAngles.y, 0);
    }

    public void AddShake(float duration, float magnitude)
    {
        StartCoroutine(ShakeRoutine(duration, magnitude));
    }

    // Immediately cancels all active shakes — call this before freezing Time.timeScale
    // so ShakeRoutine doesn't loop forever waiting on Time.deltaTime to advance
    public void StopShake()
    {
        StopAllCoroutines();
        _shakeOffset = Vector3.zero;
    }

    private IEnumerator ShakeRoutine(float duration, float magnitude)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            // Random XZ offset — no Y so camera doesn't bob vertically
            _shakeOffset = new Vector3(
                Random.Range(-1f, 1f),
                0f,
                Random.Range(-1f, 1f)
            ) * magnitude;

            elapsed += Time.deltaTime;
            yield return null;
        }
        _shakeOffset = Vector3.zero;
    }
}
//Offset defaults Y 8.5, z 10.5 Camera Rotation OffsetX 43
