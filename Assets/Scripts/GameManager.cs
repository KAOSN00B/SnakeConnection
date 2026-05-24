using UnityEngine;

// Singleton that configures Physics layer collision rules on Start.
// All friendly-fire rules are centralized here rather than scattered across
// individual movement or attack scripts.
public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Physics Layers")]
    [SerializeField] private string _playerLayer = "Player";
    [SerializeField] private string _followerLayer = "Follower";
    [SerializeField] private string _enemyLayer = "Enemy";
    [SerializeField] private string _playerBulletLayer = "PlayerBullet";
    [SerializeField] private string _enemyBulletLayer = "EnemyBullet";
    [SerializeField] private string _kidnappedFollowerLayer = "KidnappedFollower";

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        SetupLayerCollisions();
    }

    private void SetupLayerCollisions()
    {
        int player            = LayerMask.NameToLayer(_playerLayer);
        int follower          = LayerMask.NameToLayer(_followerLayer);
        int enemy             = LayerMask.NameToLayer(_enemyLayer);
        int playerBullet      = LayerMask.NameToLayer(_playerBulletLayer);
        int enemyBullet       = LayerMask.NameToLayer(_enemyBulletLayer);
        int kidnappedFollower = LayerMask.NameToLayer(_kidnappedFollowerLayer);

        // Chain units don't push each other
        Ignore(player, follower);
        Ignore(follower, follower);

        // Player bullets don't hit friendly units
        Ignore(playerBullet, player);
        Ignore(playerBullet, follower);

        // Enemy bullets don't hit enemies
        Ignore(enemyBullet, enemy);

        // Kidnapped followers are untouchable — no bullets or enemies can damage them while captured
        Ignore(playerBullet, kidnappedFollower);
        Ignore(enemyBullet,  kidnappedFollower);
        Ignore(enemy,        kidnappedFollower);
    }

    // Skips the call silently if either layer doesn't exist in the project yet
    private void Ignore(int layerA, int layerB)
    {
        if (layerA == -1 || layerB == -1) return;
        Physics.IgnoreLayerCollision(layerA, layerB, true);
    }
}
