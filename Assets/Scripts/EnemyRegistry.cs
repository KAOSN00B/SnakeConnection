using System.Collections.Generic;
using UnityEngine;

// Central enemy list — replaces GameObject.FindGameObjectsWithTag("Enemy") every frame.
// Enemies call Register on OnEnable and Unregister on OnDisable/OnDestroy.
// All reads are O(n) list walks, but n is small and no allocation happens per call.
public static class EnemyRegistry
{
    private static readonly List<Transform> _active = new List<Transform>(64);

    public static void Register(Transform t)
    {
        _active.Add(t);
    }

    public static void Unregister(Transform t)
    {
        _active.Remove(t);
    }

    // Returns the nearest enemy within maxRangeSq (squared units), or null.
    // Cleans up any null refs left by Unity destroying enemies mid-iteration.
    public static Transform GetNearest(Vector3 from, float maxRangeSq)
    {
        Transform nearest = null;
        float nearestSq = maxRangeSq;

        for (int i = _active.Count - 1; i >= 0; i--)
        {
            if (_active[i] == null) { _active.RemoveAt(i); continue; }
            float sq = (_active[i].position - from).sqrMagnitude;
            if (sq < nearestSq) { nearestSq = sq; nearest = _active[i]; }
        }

        return nearest;
    }

    public static int Count => _active.Count;
}
