using UnityEngine;

public class FollowerPickup : MonoBehaviour
{
    [SerializeField] private GameObject _followerPrefab;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        // Spawn the follower and register it with the chain
        GameObject followerObj = Instantiate(_followerPrefab, transform.position, Quaternion.identity);
        FollowerMovement follower = followerObj.GetComponent<FollowerMovement>();

        if (follower != null)
            ChainManager.Instance.AddFollower(follower);

        Destroy(gameObject);
    }
}
