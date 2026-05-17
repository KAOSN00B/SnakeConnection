using UnityEngine;

public class MoveCamera : MonoBehaviour
{
    [SerializeField] private Transform cameraPosition;
    [SerializeField] private Vector3 offset;
    [SerializeField] private Vector3 cameraRotation;
    [SerializeField] private Transform orientation;

    void Update()
    {
        FollowTarget();
        UpdateOrientation();
    }

    private void FollowTarget()
    {
        transform.position = cameraPosition.position + offset;
        transform.rotation = Quaternion.Euler(cameraRotation);
    }

    private void UpdateOrientation()
    {
        if (orientation != null)
        {
            // Keep orientation horizontal so WASD aligns with screen directions
            orientation.rotation = Quaternion.Euler(0, transform.eulerAngles.y, 0);
        }
    }
}
//Offset defaults Y 8.5, z 10.5 Camera Rotation OffsetX 43