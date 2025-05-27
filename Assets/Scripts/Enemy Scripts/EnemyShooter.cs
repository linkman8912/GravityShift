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
    public float tiltThreshold = 30f; // degrees from upright before ragdoll triggers
    public float ragdollDuration = 3f;

    private float fireCooldown;
    private bool isRagdoll = false;
    private float ragdollTimer;

    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        if (isRagdoll)
        {
            ragdollTimer -= Time.deltaTime;
            if (ragdollTimer <= 0f)
            {
                RecoverFromRagdoll();
            }
            return;
        }

        if (player == null) return;

        if (IsTilted())
        {
            EnterRagdoll();
            return;
        }

        RotateBodyTowardsPlayer();
        RotateFirePointTowardsPlayer();
        HandleShooting();
    }

    bool IsTilted()
    {
        float angle = Vector3.Angle(transform.up, Vector3.up);
        return angle > tiltThreshold;
    }

    void EnterRagdoll()
    {
        isRagdoll = true;
        ragdollTimer = ragdollDuration;
        rb.constraints = RigidbodyConstraints.None; // allow full physics
        rb.useGravity = true;
    }

    void RecoverFromRagdoll()
    {
        isRagdoll = false;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.rotation = Quaternion.Euler(0, transform.eulerAngles.y, 0); // reset upright
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ | RigidbodyConstraints.FreezePositionY;
    }

    void RotateBodyTowardsPlayer()
    {
        Vector3 direction = player.position - transform.position;
        direction.y = 0f;
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
