using UnityEngine;

public class BouncePad : MonoBehaviour
{
    [SerializeField] private float rotateXAxis = 0.0f;
    [SerializeField] private float rotateYAxis = 0.0f;
    [SerializeField] private float rotateZAxis = 0.0f;

    public float launchForce = 50f; // High force for launch
    public float upwardModifier = 2f; // Extra lift

    private void Update()
    {
        // Multiply by deltaTime so spin speed is frame-rate independent
        transform.Rotate(rotateXAxis * Time.deltaTime, rotateYAxis * Time.deltaTime, rotateZAxis * Time.deltaTime);
    }
    private void OnCollisionEnter(Collision collision)
    {
        // Check if we hit the player
        if (collision.gameObject.CompareTag("Player"))
        {
            Rigidbody playerRb = collision.gameObject.GetComponent<Rigidbody>();

            if (playerRb != null)
            {
                Vector3 launchDir = collision.transform.position - transform.position;

                // remove vertical component
                launchDir.y = 0f;

                // normalize AFTER flattening
                launchDir = launchDir.normalized;


                // Apply impulse force
                playerRb.AddForce(launchDir * launchForce, ForceMode.Impulse);


            }
        }
    }


}
