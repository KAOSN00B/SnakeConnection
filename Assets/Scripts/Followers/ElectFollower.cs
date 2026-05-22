using UnityEngine;

// Attach to the Sparker follower prefab.
// Places one ElecTrap at its own feet on a cooldown. Only one trap is active per
// follower at a time — placing another is blocked until the current trap disappears
// or triggers. After triggering, a shorter post-trigger cooldown applies.
public class ElectFollower : MonoBehaviour
{
    [SerializeField] private GameObject _trapPrefab;

    [Tooltip("Seconds between placing a new trap (normal cycle).")]
    [SerializeField] private float _placementCooldown = 5f;

    [Tooltip("Seconds to wait before placing a new trap after one was triggered.")]
    [SerializeField] private float _postTriggerCooldown = 2f;

    private GameObject _activeTrap;
    private float _cooldownTimer;

    private void Update()
    {
        // Block placement while a trap is still alive on the field
        if (_activeTrap != null) return;

        _cooldownTimer -= Time.deltaTime;
        if (_cooldownTimer <= 0f)
            PlaceTrap();
    }

    private void PlaceTrap()
    {
        if (_trapPrefab == null) return;

        _activeTrap = Instantiate(_trapPrefab, transform.position, Quaternion.identity);

        ElecTrap trap = _activeTrap.GetComponent<ElecTrap>();
        if (trap != null)
            trap.Initialize(this);
    }

    // Called by ElecTrap when it deactivates — either triggered by an enemy or expired naturally
    public void OnTrapDeactivated(bool wasTriggered)
    {
        _activeTrap = null;
        _cooldownTimer = wasTriggered ? _postTriggerCooldown : _placementCooldown;
    }
}
