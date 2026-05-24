using UnityEngine;

// Attach to a pickup object in the scene. When the Player walks through it,
// the follower prefab is spawned and registered with ChainManager to join the chain.
// The pickup then destroys itself so it cannot be collected a second time.
public class FollowerPickup : MonoBehaviour
{
    [SerializeField] private GameObject _followerPrefab;

    [Header("Audio")]
    [SerializeField] private AudioClip _pickupSound;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (_pickupSound != null)
            AudioSource.PlayClipAtPoint(_pickupSound, transform.position);

        // Spawn the follower and register it with the chain
        GameObject followerObj = Instantiate(_followerPrefab, transform.position, Quaternion.identity);
        FollowerMovement follower = followerObj.GetComponent<FollowerMovement>();

        if (follower != null)
            ChainManager.Instance.AddFollower(follower);

        Destroy(gameObject);
    }
}
