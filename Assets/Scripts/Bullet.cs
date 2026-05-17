using UnityEngine;

public class Bullet : MonoBehaviour
{

    [SerializeField] private int _damage = 1;
    
    private int GetDamage()
    {
        return _damage;
    }

    private void OnCollisionEnter(Collision collision)
    {
        ProcessCollision(collision.gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        ProcessCollision(other.gameObject);
    }

    private void ProcessCollision(GameObject target)
    {
        IDamageable damageable = target.GetComponent<IDamageable>();

        if (damageable != null)
        {
            damageable.TakeDamage(_damage);
        }

        Destroy(gameObject);
    }
    }
