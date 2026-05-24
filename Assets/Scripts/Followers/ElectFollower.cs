using UnityEngine;

// Attach to the Sparker follower prefab.
// Watches for enemies within _detectionRadius. When one gets close, drops an ElecTrap
// at its own feet. Only one trap is active per follower at a time.
public class ElectFollower : MonoBehaviour
{
    [SerializeField] private GameObject _trapPrefab;

    [Tooltip("How close an enemy must be before this follower drops a trap.")]
    [SerializeField] private float _detectionRadius = 5f;

    [Tooltip("Seconds to wait before dropping another trap after one was triggered.")]
    [SerializeField] private float _postTriggerCooldown = 2f;

    [Tooltip("Seconds to wait before dropping another trap after one expired naturally.")]
    [SerializeField] private float _expiredCooldown = 5f;

    [Header("Audio")]
    [SerializeField] private AudioClip _trapPlaceSound;

    private GameObject _activeTrap;
    private float _cooldownTimer;

    private void Update()
    {
        // Block placement while a trap is already on the field
        if (_activeTrap != null) return;

        _cooldownTimer -= Time.deltaTime;
        if (_cooldownTimer > 0f) return;

        // Only drop a trap when an enemy is actually nearby
        float detectionRadiusSq = _detectionRadius * _detectionRadius;
        if (EnemyRegistry.GetNearest(transform.position, detectionRadiusSq) != null)
            PlaceTrap();
    }

    private void PlaceTrap()
    {
        if (_trapPrefab == null) return;

        if (_trapPlaceSound != null)
            AudioSource.PlayClipAtPoint(_trapPlaceSound, transform.position);

        _activeTrap = Instantiate(_trapPrefab, transform.position, Quaternion.identity);

        ElecTrap trap = _activeTrap.GetComponent<ElecTrap>();
        if (trap != null)
            trap.Initialize(this);
    }

    // Called by ElecTrap when it deactivates — triggered by an enemy or expired naturally
    public void OnTrapDeactivated(bool wasTriggered)
    {
        _activeTrap = null;
        _cooldownTimer = wasTriggered ? _postTriggerCooldown : _expiredCooldown;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, _detectionRadius);
    }
}
