using UnityEngine;

public class EnemyAttack : MonoBehaviour
{
    [SerializeField] private float attackRange = 10f;
    [SerializeField] private float fireRate = 1f;
    [SerializeField] private float bulletSpeed = 12f;
    [SerializeField] private float bulletLifetime = 3f;

    [SerializeField] private Transform _firePoint;
    [SerializeField] private GameObject _bulletPrefab;

    private Transform _target;
    private float _fireCooldown;

    void Start()
    {
        // Start cooldown full so the enemy doesn't fire the instant a target enters range
        _fireCooldown = 1f / fireRate;
    }

    void Update()
    {
        FindNearestTarget();
        if (_target == null) return;
        FaceTarget();
        HandleFiring();
    }

    // Finds the nearest player or follower within attackRange using tags
    private void FindNearestTarget()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, attackRange);

        Transform nearest = null;
        float nearestDist = float.MaxValue;

        foreach (Collider hit in hits)
        {
            if (!hit.CompareTag("Player") && !hit.CompareTag("Follower")) continue;

            float dist = Vector3.Distance(transform.position, hit.transform.position);
            if (dist < nearestDist)
            {
                nearestDist = dist;
                nearest = hit.transform;
            }
        }

        _target = nearest;
    }

    private void FaceTarget()
    {
        Vector3 dir = _target.position - transform.position;
        // Flatten to XZ so the enemy doesn't tilt up/down toward the target
        dir.y = 0f;
        // LookRotation aligns the Z axis (forward) toward dir
        if (dir != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(dir);
    }

    private void HandleFiring()
    {
        _fireCooldown -= Time.deltaTime;
        if (_fireCooldown <= 0f)
        {
            Shoot();
            // Reset cooldown — 1 / fireRate gives seconds between shots (e.g. fireRate 2 = 0.5s gap)
            _fireCooldown = 1f / fireRate;
        }
    }

    private void Shoot()
    {
        if (_firePoint == null || _bulletPrefab == null) return;

        // Spawn bullet at the fire point's position and rotation
        GameObject bullet = Instantiate(_bulletPrefab, _firePoint.position, _firePoint.rotation);

        // Push the bullet forward along the fire point's Z axis
        Rigidbody rb = bullet.GetComponent<Rigidbody>();
        if (rb != null)
            rb.linearVelocity = _firePoint.forward * bulletSpeed;

        // Auto-destroy so bullets don't accumulate in the scene forever
        Destroy(bullet, bulletLifetime);
    }

    private void OnDrawGizmosSelected()
    {
        // Visualize attack range in the editor when this object is selected
        Gizmos.color = Color.red;
        Vector3 center = _firePoint != null ? _firePoint.position : transform.position;
        Gizmos.DrawWireSphere(center, attackRange);
    }
}