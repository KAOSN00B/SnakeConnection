using UnityEngine;

public class Health : MonoBehaviour, IDamageable
{
    // Any system (GameManager, UI) can subscribe to this to handle player death
    public static event System.Action OnPlayerDeath;

    [SerializeField] private int _maxHealth = 3;
    private int _currentHealth;

    private void Awake()
    {
        _currentHealth = _maxHealth;
    }

    public void TakeDamage(int damage)
    {
        _currentHealth -= damage;
        if (_currentHealth <= 0)
            Die();
    }

    public void Die()
    {
        Debug.Log($"[{gameObject.name}] Die() called");
        if (gameObject.CompareTag("Player"))
        {
// Don't destroy the player — fire the event and let GameManager handle it
            OnPlayerDeath?.Invoke();
            return;
        }

        Destroy(gameObject);
    }
}
