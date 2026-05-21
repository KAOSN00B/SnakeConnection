using TMPro;
using UnityEngine;

public class TimerUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _timerText;

    private float _timer;

    private void Update()
    {
        _timer += Time.deltaTime;

        int minutes = Mathf.FloorToInt(_timer / 60f);
        int seconds = Mathf.FloorToInt(_timer % 60f);

        _timerText.text = $"Time:{minutes:00}:{seconds:00}";
    }

    public float GetElapsedTime()
    {
        return _timer;
    }
}
