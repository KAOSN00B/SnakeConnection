using UnityEngine;

// Singleton that tracks the player's score.
// Formula: base_worth * max(1, follower_count * _pointsPerFollower)
// Having more followers in the chain multiplies every kill's value.
public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance;

    [Tooltip("Points added to the multiplier per follower. 5 followers = 5 x this value.")]
    [SerializeField] private int _pointsPerFollower = 25;

    public int CurrentScore { get; private set; }

    // UI subscribes to this to update the display without polling every frame
    public event System.Action<int> OnScoreChanged;

    private void Awake()
    {
        Instance = this;
    }

    public void AddScore(int baseWorth)
    {
        int followerCount = ChainManager.Instance != null ? ChainManager.Instance.FollowerCount : 0;

        // Always award at least base_worth when no followers; bonus scales with chain length
        int multiplier = Mathf.Max(1, followerCount * _pointsPerFollower);
        int points = baseWorth * multiplier;

        CurrentScore += points;
        OnScoreChanged?.Invoke(CurrentScore);
    }
}
