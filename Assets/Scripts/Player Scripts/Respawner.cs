using UnityEngine;

public class Respawner : MonoBehaviour
{
    public Transform respawnPoint;

    public static Respawner Instance;

    private void Awake()
    {
        Instance = this;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Rigidbody rb = other.GetComponent<Rigidbody>();
            // Zero velocity before teleporting so player doesn't carry momentum into the respawn
            if (rb != null) rb.linearVelocity = Vector3.zero;
            other.transform.position = respawnPoint.position;
        }
    }
}
