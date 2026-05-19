using UnityEngine;

public class TimerUI : MonoBehaviour
{
    private string _timerText;

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void DisplayText()
    {
        _timerText = $"Time: {Time.timeSinceLevelLoad:F2} seconds";
        Debug.Log(_timerText);
    }
}
