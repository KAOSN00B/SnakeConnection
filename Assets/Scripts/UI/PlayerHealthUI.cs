using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthUI : MonoBehaviour
{
    [SerializeField] private Slider _healthSlider;

    private Health _playerHealth;

    private void Start()
    {
        // Start() is guaranteed to run after all Awake() calls in the scene,
        // so Health.Awake() has already set _currentHealth = _maxHealth by this point.
        if (_healthSlider == null)
            _healthSlider = GetComponentInChildren<Slider>();

        SubscribeToPlayer();
    }

    private void SubscribeToPlayer()
    {
        if (_playerHealth != null)
            _playerHealth.OnHealthChanged -= HandleHealthChanged;

        GameObject player = GameObject.FindWithTag("Player");
        if (player == null) return;

        _playerHealth = player.GetComponentInChildren<Health>();
        if (_playerHealth == null) return;

        _playerHealth.OnHealthChanged += HandleHealthChanged;

        if (_healthSlider == null) return;

        _healthSlider.minValue = 0;
        _healthSlider.maxValue = _playerHealth.MaxHealth;
        _healthSlider.value = _playerHealth.CurrentHealth;
    }

    private void HandleHealthChanged(int current, int max)
    {
        if (_healthSlider == null) return;
        _healthSlider.value = current;
    }

    private void OnDestroy()
    {
        if (_playerHealth != null)
            _playerHealth.OnHealthChanged -= HandleHealthChanged;
    }
}
