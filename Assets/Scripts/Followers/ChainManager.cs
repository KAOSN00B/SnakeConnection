using System.Collections.Generic;
using UnityEngine;

public class ChainManager : MonoBehaviour
{
    public static ChainManager Instance;

    // How far the player must move before a new point is recorded
    [SerializeField] private float _recordDistance = 0.15f;
    // World-space gap between each unit in the chain
    [SerializeField] private float _followerSpacing = 1.5f;

    private readonly List<Vector3> _posHistory = new List<Vector3>();
    private readonly List<FollowerMovement> _followers = new List<FollowerMovement>();

    private Transform _playerTransform;
    private int _pointsPerFollower;

    private void Awake()
    {
        Instance = this;
        _pointsPerFollower = Mathf.Max(1, Mathf.RoundToInt(_followerSpacing / _recordDistance));
        InitializeHistory();
    }

    private void InitializeHistory()
    {
        _posHistory.Clear();
        
        // Find player - Priority: Active object with PlayerMovement component
        GameObject playerObj = null;
        var move = Object.FindAnyObjectByType<PlayerMovement>();
        if (move != null) 
        {
            playerObj = move.gameObject;
        }
        else
        {
            // Fallback to tag if component not found
            playerObj = GameObject.FindWithTag("Player");
        }

        if (playerObj != null)
        {
            _playerTransform = playerObj.transform;
            Vector3 startPos = _playerTransform.position;
            
            // Pre-fill history with current position
            int prefillCount = _pointsPerFollower * 20;
            for (int i = 0; i < prefillCount; i++)
                _posHistory.Add(startPos);
        }
        else
        {
            // fallback if player is really missing
            _posHistory.Add(Vector3.zero);
        }
    }

    private void FixedUpdate()
    {
        if (_playerTransform == null)
        {
            // Re-find player if lost (e.g. after some weird reload)
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null) _playerTransform = playerObj.transform;
            else return;
            
            // If we just found him, maybe re-init history if it was empty
            if (_posHistory.Count <= 1 && _posHistory[0] == Vector3.zero)
                InitializeHistory();
        }

        RecordPosition();
    }

    private void RecordPosition()
    {
        // Record a new position only when the player has moved far enough from the last point
        if (Vector3.Distance(_posHistory[0], _playerTransform.position) >= _recordDistance)
        {
            _posHistory.Insert(0, _playerTransform.position);

            // Trim history to only what the current chain length needs
            int maxPoints = (_followers.Count + 1) * _pointsPerFollower + 1;
            if (_posHistory.Count > maxPoints)
                _posHistory.RemoveRange(maxPoints, _posHistory.Count - maxPoints);
        }
    }

    public bool HasFollowers => _followers.Count > 0;

    // Called by FollowerPickup when a new follower is collected
    public void AddFollower(FollowerMovement follower)
    {
        if (_followers.Contains(follower)) return;
        _followers.Add(follower);
        // Chain index is 1-based — player is index 0 implicitly
        follower.Init(_followers.Count, _pointsPerFollower);
    }

    // Called by FollowerMovement.OnDestroy when a follower dies
    public void RemoveFollower(FollowerMovement follower)
    {
        int index = _followers.IndexOf(follower);
        if (index == -1) return;

        _followers.RemoveAt(index);

        // Re-index every follower after the removed one so they slide up to fill the gap
        for (int i = index; i < _followers.Count; i++)
            _followers[i].Init(i + 1, _pointsPerFollower);
    }

    // Returns the nearest follower transform to a given world position
    public Transform GetNearestFollower(Vector3 fromPosition)
    {
        Transform nearest = null;
        float nearestDist = float.MaxValue;

        foreach (FollowerMovement follower in _followers)
        {
            float dist = Vector3.Distance(fromPosition, follower.transform.position);
            if (dist < nearestDist)
            {
                nearestDist = dist;
                nearest = follower.transform;
            }
        }

        return nearest;
    }

    // Returns the recorded world position at the given history index
    // Clamps to oldest available point if history hasn't built up yet
    public Vector3 GetHistoryPosition(int historyIndex)
    {
        int clamped = Mathf.Min(historyIndex, _posHistory.Count - 1);
        return _posHistory[clamped];
    }
}
