using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Physics Layers")]
    [SerializeField] private string _playerLayer = "Player";
    [SerializeField] private string _followerLayer = "Follower";
    [SerializeField] private string _enemyLayer = "Enemy";
    [SerializeField] private string _playerBulletLayer = "PlayerBullet";
    [SerializeField] private string _enemyBulletLayer = "EnemyBullet";

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
        int player = LayerMask.NameToLayer(_playerLayer);
        int follower = LayerMask.NameToLayer(_followerLayer);
        int enemy = LayerMask.NameToLayer(_enemyLayer);
        int playerBullet = LayerMask.NameToLayer(_playerBulletLayer);
        int enemyBullet = LayerMask.NameToLayer(_enemyBulletLayer);

        // Chain units don't push each other
        Physics.IgnoreLayerCollision(player, follower, true);
        Physics.IgnoreLayerCollision(follower, follower, true);

        // Player bullets don't hit friendly units
        Physics.IgnoreLayerCollision(playerBullet, player, true);
        Physics.IgnoreLayerCollision(playerBullet, follower, true);

        // Enemy bullets don't hit enemies
        Physics.IgnoreLayerCollision(enemyBullet, enemy, true);
    }

    private void OnEnable()
    {
        Health.OnPlayerDeath += HandlePlayerDeath;
    }

    private void OnDisable()
    {
        // Always unsubscribe to avoid stale references
        Health.OnPlayerDeath -= HandlePlayerDeath;
    }

    private void HandlePlayerDeath()
    {
        // TODO: pause game and show game over screen before reloading
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
