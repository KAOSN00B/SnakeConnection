using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

// Attach to the Game Over panel (starts inactive in the Inspector).
// PlayerFeelController calls Show() after the death explosion finishes.
public class GameOverUI : MonoBehaviour
{
    [SerializeField] private TMP_Text _finalScoreText;

    public void Show()
    {
        Time.timeScale = 0f;
        gameObject.SetActive(true);
        MusicManager.Instance?.PlayGameOver();

        if (_finalScoreText != null && ScoreManager.Instance != null)
            _finalScoreText.text = $"Score: {ScoreManager.Instance.CurrentScore:N0}";
    }

    public void OnRetryButton()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
