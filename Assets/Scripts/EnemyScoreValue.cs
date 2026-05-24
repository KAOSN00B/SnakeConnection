using UnityEngine;

// Attach to every enemy prefab and set the point value in the Inspector.
// EnemyDeathFeedback and HackerDeathFeedback read this on death.
//   Enemy type 0 (basic) → 50
//   Hacker              → 100
//   Enemy type 2        → 150
public class EnemyScoreValue : MonoBehaviour
{
    [SerializeField] private int _scoreWorth = 50;
    public int ScoreWorth => _scoreWorth;
}
