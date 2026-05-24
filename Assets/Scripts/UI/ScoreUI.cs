using UnityEngine;
using TMPro;

public class ScoreUI : MonoBehaviour
{
    [SerializeField] private TMP_Text _scoreText;

    private void Start()
    {
        if (_scoreText == null)
            _scoreText = GetComponentInChildren<TMP_Text>();

        // If instance isn't set yet (rare but possible), try to find it manually
        if (ScoreManager.Instance == null)
        {
            ScoreManager.Instance = Object.FindAnyObjectByType<ScoreManager>();
        }

        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.OnScoreChanged += UpdateDisplay;
            UpdateDisplay(ScoreManager.Instance.CurrentScore);
        }
    }

    private void OnDestroy()
    {
        if (ScoreManager.Instance != null)
            ScoreManager.Instance.OnScoreChanged -= UpdateDisplay;
    }

    private void UpdateDisplay(int score)
    {
        if (_scoreText != null)
            _scoreText.text = "PTS:" + score.ToString("N0");
    }
}
