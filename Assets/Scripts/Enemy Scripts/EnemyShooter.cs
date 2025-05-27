using UnityEngine;

public class EnemyShooter : MonoBehaviour
{
    public Transform player;
    public GameObject projectilePrefab;
    public Transform firePoint;
    public float fireRate = 2f;
    public float bodyRotationSpeed = 5f;
    public float firePointRotationSpeed = 10f;
    public float projectileSpeed = 10f;

    private float fireCooldown;

    void Update()
    {
        if (player == null) return;

        RotateBodyTowardsPlayer();
        RotateFirePointTowardsPlayer();
        HandleShooting();
    }

    void RotateBodyTowardsPlayer()
    {
        Vector3 direction = player.position - transform.position;
        direction.y = 0f; // Only Y-axis rotation
        if (direction == Vector3.zero) return;

        Quaternion lookRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, bodyRotationSpeed * Time.deltaTime);
    }

    void RotateFirePointTowardsPlayer()
    {
        Vector3 directionToPlayer = player.position - firePoint.position;
        if (directionToPlayer == Vector3.zero) return;

        Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer.normalized);
        firePoint.rotation = Quaternion.Slerp(firePoint.rotation, targetRotation, firePointRotationSpeed * Time.deltaTime);
    }

    void HandleShooting()
    {
        fireCooldown -= Time.deltaTime;
        if (fireCooldown <= 0f)
        {
            Shoot();
            fireCooldown = 1f / fireRate;
        }
    }

    void Shoot()
    {
        if (projectilePrefab == null) return;

        GameObject projectile = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);
        Rigidbody rb = projectile.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = firePoint.forward * projectileSpeed;
        }
    }
}
