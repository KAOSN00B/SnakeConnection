using System.Collections;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    [SerializeField] private int _damage = 1;

    private GameObject _owner;
    private Rigidbody _rb;
    private Coroutine _lifetimeCoroutine;
    private bool _spent; // true when inactive/returning — blocks collision events during that window

    // Set once by BulletPool so Release() knows which stack to return to
    public int PoolKey { get; private set; }
    public void AssignPoolKey(int key) { PoolKey = key; }

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }

    // Called by BulletPool.Get() immediately after positioning.
    // Sets velocity, starts lifetime countdown, and resets state for reuse.
    public void Launch(Vector3 velocity, float lifetime, GameObject owner = null)
    {
        _spent = false;
        _owner = null;

        if (owner != null) SetOwner(owner);
        if (_rb != null) _rb.linearVelocity = velocity;

        if (_lifetimeCoroutine != null) StopCoroutine(_lifetimeCoroutine);
        _lifetimeCoroutine = StartCoroutine(LifetimeRoutine(lifetime));
    }

    public void SetOwner(GameObject owner)
    {
        _owner = owner;

        // Physics.IgnoreCollision prevents the callback from ever firing for the owner.
        // ShouldIgnore() handles Player/Follower tag filtering in code, so this is only
        // needed to stop the bullet from hitting its direct shooter.
        Collider bulletCol = GetComponent<Collider>();
        if (bulletCol == null) return;

        foreach (Collider col in owner.GetComponentsInChildren<Collider>())
            if (col != null) Physics.IgnoreCollision(bulletCol, col);
    }

    private IEnumerator LifetimeRoutine(float lifetime)
    {
        yield return new WaitForSeconds(lifetime);
        ReturnToPool();
    }

    private void ReturnToPool()
    {
        if (_spent) return;
        _spent = true;

        if (_lifetimeCoroutine != null) { StopCoroutine(_lifetimeCoroutine); _lifetimeCoroutine = null; }
        if (_rb != null) _rb.linearVelocity = Vector3.zero;
        _owner = null;

        if (BulletPool.Instance != null)
            BulletPool.Instance.Release(this);
        else
            Destroy(gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (_spent || ShouldIgnore(collision.gameObject)) return;
        ProcessCollision(collision.gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_spent || ShouldIgnore(other.gameObject)) return;
        ProcessCollision(other.gameObject);
    }

    private bool ShouldIgnore(GameObject target)
    {
        if (target == _owner) return true;

        // Enemy bullets (_owner == null) should hit Player and Follower — don't skip them.
        // Friendly bullets skip all Player/Follower colliders to avoid friendly fire.
        if (_owner != null && (target.CompareTag("Player") || target.CompareTag("Follower")))
            return true;

        return false;
    }

    private void ProcessCollision(GameObject target)
    {
        IDamageable damageable = target.GetComponent<IDamageable>();
        if (damageable != null) damageable.TakeDamage(_damage);
        ReturnToPool();
    }
}
