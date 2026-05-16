using UnityEngine;

public class MoveCamera : MonoBehaviour
{
    [SerializeField] private Transform cameraPosition;
    [SerializeField] private Vector3 offset;
    [SerializeField] private Vector3 cameraRotation;
    [SerializeField] private Transform orientation;

    void Update()
    {
        transform.position = cameraPosition.position + offset;
        transform.rotation = Quaternion.Euler(cameraRotation);

        if (orientation != null)
        {
            orientation.rotation = Quaternion.Euler(0, transform.eulerAngles.y, 0);
        }
    }
}
//Offset defaults Y 8.5, z 10.5 Camera Rotation OffsetX 43