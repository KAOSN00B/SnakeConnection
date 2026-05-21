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
        int prefabKey = prefab.GetHashCode();
        if (!_pools.TryGetValue(prefabKey, out Stack<Bullet> bulletStack))
        {
            bulletStack = new Stack<Bullet>(16);
            _pools[prefabKey] = bulletStack;
        }

        Bullet bullet = null;
        while (bulletStack.Count > 0)
        {
            bullet = bulletStack.Pop();
            if (bullet != null) break; // discard nulls left by scene reload
            bullet = null;
        }

        if (bullet == null)
        {
            GameObject newBulletObject = Instantiate(prefab, position, rotation);
            bullet = newBulletObject.GetComponent<Bullet>();
            bullet.AssignPoolKey(prefabKey);
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
