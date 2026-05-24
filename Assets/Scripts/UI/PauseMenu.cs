using UnityEngine;
using UnityEngine.SceneManagement;

// Attach to an always-active object (Canvas or GameManager).
// Assign the pause panel in the Inspector — it starts inactive.
// Press Escape to toggle. Resume/Retry buttons are wired in the Inspector.
public class PauseMenu : MonoBehaviour
{
    [SerializeField] private GameObject _pausePanel;

    private bool _isPaused;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (_isPaused) Resume();
            else Pause();
        }
    }

    private void Pause()
    {
        _isPaused = true;
        Time.timeScale = 0f;
        if (_pausePanel != null) _pausePanel.SetActive(true);
    }

    public void Resume()
    {
        _isPaused = false;
        Time.timeScale = 1f;
        if (_pausePanel != null) _pausePanel.SetActive(false);
    }

    public void OnRetryButton()
    {
        _isPaused = false;
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
