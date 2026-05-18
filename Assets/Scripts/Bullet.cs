using UnityEngine;

public class Bullet : MonoBehaviour
{
    [SerializeField] private int _damage = 1;
    private GameObject _owner;

    public void SetOwner(GameObject owner)
    {
        _owner = owner;
        
        // Also ignore collision between bullet and owner's colliders
        Collider bulletCollider = GetComponent<Collider>();
        Collider ownerCollider = owner.GetComponent<Collider>();
        if (bulletCollider != null && ownerCollider != null)
        {
            Physics.IgnoreCollision(bulletCollider, ownerCollider);
        }
        
        // Also check children if the collider is on a child (like FollowerFace)
        Collider[] ownerColliders = owner.GetComponentsInChildren<Collider>();
        foreach (var col in ownerColliders)
        {
            Physics.IgnoreCollision(bulletCollider, col);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (ShouldIgnore(collision.gameObject)) return;
        ProcessCollision(collision.gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (ShouldIgnore(other.gameObject)) return;
        ProcessCollision(other.gameObject);
    }

    private bool ShouldIgnore(GameObject target)
    {
        if (target == _owner) return true;
        
        // Ignore the player and other followers
        if (target.CompareTag("Player") || target.CompareTag("Follower"))
            return true;
            
        return false;
    }

    private void ProcessCollision(GameObject target)
    {
        IDamageable damageable = target.GetComponent<IDamageable>();

        if (damageable != null)
        {
            damageable.TakeDamage(_damage);
        }

        // Bullet disappears after hitting anything else (like enemies or walls)
        Destroy(gameObject);
    }
}
