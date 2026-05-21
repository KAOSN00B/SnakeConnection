using UnityEngine;

// General-purpose health component for the Player, Followers, and Enemies.
// On death, routes to the correct handler:
//   - Player   → fires the static OnPlayerDeath event (handled by PlayerFeelController)
//   - Follower → calls FollowerDeathFeedback.TriggerDeath() for the flash + particle sequence
//   - Everything else → Destroy(gameObject) immediately
public class Health : MonoBehaviour, IDamageable
{
    public static event System.Action OnPlayerDeath;

    // Fires whenever health changes — UI subscribes to this instead of polling every frame
    public event System.Action<int, int> OnHealthChanged; // (currentHealth, maxHealth)

    [SerializeField] private int _maxHealth = 3;
    private int _currentHealth;
    private bool _isDead;

    public int CurrentHealth => _currentHealth;
    public int MaxHealth => _maxHealth;

    private void Awake()
    {
        _currentHealth = _maxHealth;
    }

    public void TakeDamage(int damage)
    {
        if (_isDead) return;

        _currentHealth -= damage;
        _currentHealth = Mathf.Max(_currentHealth, 0);

        OnHealthChanged?.Invoke(_currentHealth, _maxHealth);

        if (_currentHealth <= 0)
            Die();
    }

    public void Die()
    {
        if (_isDead) return;
        _isDead = true;

        if (gameObject.CompareTag("Player"))
        {
            OnPlayerDeath?.Invoke();
            return;
        }

        FollowerDeathFeedback feedback = GetComponent<FollowerDeathFeedback>();
        if (feedback != null)
        {
            feedback.TriggerDeath();
            return;
        }

        Destroy(gameObject);
    }
}
