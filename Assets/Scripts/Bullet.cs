using UnityEngine;

public class Bullet : MonoBehaviour
{

    [SerializeField] private int _damage = 1;
    
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private int GetDamage()
    {
        return _damage;
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Check if the collided object has the "Enemy" tag
        if (collision.gameObject.CompareTag("Enemy"))
        {
            // Try to get the Health component from the collided object
            Health enemyHealth = collision.gameObject.GetComponent<Health>();
            if (enemyHealth != null)
            {
                // Apply damage to the enemy
                enemyHealth.TakeDamage(_damage);
            }
        }
        // Destroy the bullet after it collides with anything
        Destroy(gameObject);
    }
}
