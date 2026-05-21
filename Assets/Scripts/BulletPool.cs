using System.Collections.Generic;
using UnityEngine;

// Object pool for bullets. Eliminates per-shot Instantiate/Destroy GC spikes.
// Auto-creates itself on first use — no manual scene setup required.
// On scene reload the pool is destroyed with the scene and recreates cleanly.
public class BulletPool : MonoBehaviour
{
    private static BulletPool _instance;
    public static BulletPool Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject("[BulletPool]");
                _instance = go.AddComponent<BulletPool>();
            }
            return _instance;
        }
    }

    // Keyed by prefab InstanceID so each prefab type has its own stack
    private readonly Dictionary<int, Stack<Bullet>> _pools = new Dictionary<int, Stack<Bullet>>();

    private void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(gameObject); return; }
        _instance = this;
    }

    private void OnDestroy()
    {
        if (_instance == this) _instance = null;
    }

    // Retrieves (or instantiates) a bullet, positions it, and fires it in one call.
    // Pass owner=null for enemy bullets so they can hit Player and Follower.
    public Bullet Get(GameObject prefab, Vector3 position, Quaternion rotation,
                      Vector3 velocity, float lifetime, GameObject owner = null)
    {
        int key = prefab.GetHashCode();
        if (!_pools.TryGetValue(key, out Stack<Bullet> stack))
        {
            stack = new Stack<Bullet>(16);
            _pools[key] = stack;
        }

        Bullet bullet = null;
        while (stack.Count > 0)
        {
            bullet = stack.Pop();
            if (bullet != null) break; // discard nulls left by scene reload
            bullet = null;
        }

        if (bullet == null)
        {
            GameObject go = Instantiate(prefab, position, rotation);
            bullet = go.GetComponent<Bullet>();
            bullet.AssignPoolKey(key);
        }
        else
        {
            bullet.transform.SetPositionAndRotation(position, rotation);
            bullet.gameObject.SetActive(true);
        }

        bullet.Launch(velocity, lifetime, owner);
        return bullet;
    }

    public void Release(Bullet bullet)
    {
        bullet.gameObject.SetActive(false);
        if (_pools.TryGetValue(bullet.PoolKey, out Stack<Bullet> stack))
            stack.Push(bullet);
    }
}
