using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EnemyShooter : MonoBehaviour
{
    public Transform player;
    public GameObject projectilePrefab;
    public Transform firePoint;

    [Header("Shooting Settings")]
    public float fireRate = 2f;
    public float projectileSpeed = 10f;

    [Header("Rotation Settings")]
    public float bodyRotationSpeed = 5f;
    public float firePointRotationSpeed = 10f;

    [Header("Ragdoll Settings")]
    public float tiltThreshold = 30f;
    public float ragdollDuration = 3f;
    public LayerMask groundLayer;

    [Header("Advanced Ragdoll Control")]
    public LayerMask disableRecoveryLayer;

    private float fireCooldown;
    private bool isRagdoll = false;
    private float ragdollTimer;

    private Rigidbody rb;
    public bool IsGrappled { get; set; } = false;

    private bool isGrounded = false;

    private HashSet<Collider> collisionStayObjects = new HashSet<Collider>();
    private int recoveryBlockContacts = 0;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        Debug.Log($"[EnemyState] Ragdoll: {isRagdoll}, Grounded: {isGrounded}, Blocked: {IsBlockedFromRecovery()}, Grappled: {IsGrappled}, Gravity: {rb.useGravity}, Constraints: {rb.constraints}");

        if (isRagdoll)
        {
            ragdollTimer -= Time.deltaTime;
            Debug.Log($"[RagdollTimer] Time remaining: {ragdollTimer:F2}");

            if (ragdollTimer <= 0f && !IsGrappled && IsGrounded() && !IsBlockedFromRecovery())
            {
                Debug.Log("[Recovery] Conditions met, starting recovery.");
                RecoverFromRagdoll();
            }
            return;
        }

        if (player == null) return;

        if (IsTilted())
        {
            Debug.Log("[Tilt] Tilt threshold exceeded, entering ragdoll.");
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

    bool IsGrounded()
    {
        return isGrounded;
    }

    bool IsBlockedFromRecovery()
    {
        return recoveryBlockContacts > 0;
    }

    void OnCollisionStay(Collision collision)
    {
        int layer = collision.gameObject.layer;

        if (((1 << layer) & groundLayer) != 0)
        {
            if (!isGrounded)
                Debug.Log("[Collision] Touching ground layer.");
            isGrounded = true;
        }

        if (((1 << layer) & disableRecoveryLayer) != 0)
        {
            if (!collisionStayObjects.Contains(collision.collider))
            {
                Debug.Log($"[Collision] Touching NO-RECOVERY layer object: {collision.collider.name}");
                collisionStayObjects.Add(collision.collider);
                recoveryBlockContacts++;
            }
        }
    }

    void OnCollisionExit(Collision collision)
    {
        int layer = collision.gameObject.layer;

        if (((1 << layer) & groundLayer) != 0)
        {
            Debug.Log("[Collision] Left ground layer.");
            isGrounded = false;
        }

        if (((1 << layer) & disableRecoveryLayer) != 0)
        {
            if (collisionStayObjects.Contains(collision.collider))
            {
                Debug.Log($"[Collision] Left NO-RECOVERY layer object: {collision.collider.name}");
                collisionStayObjects.Remove(collision.collider);
                recoveryBlockContacts = Mathf.Max(0, recoveryBlockContacts - 1);
            }
        }
    }

    void EnterRagdoll()
    {
        Debug.Log("[EnterRagdoll] Entering ragdoll state.");
        isRagdoll = true;
        ragdollTimer = ragdollDuration;
        rb.constraints = RigidbodyConstraints.None;
        rb.useGravity = true;
    }

    void RecoverFromRagdoll()
    {
        Debug.Log("[Recover] Initiating smooth recovery.");
        isRagdoll = false;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        StartCoroutine(SmoothRecovery(1.5f));
    }

    IEnumerator SmoothRecovery(float duration)
    {
        Quaternion startRotation = rb.rotation;
        Quaternion targetRotation = Quaternion.Euler(0, startRotation.eulerAngles.y, 0);
        float elapsed = 0f;

        Debug.Log("[SmoothRecovery] Starting rotation to upright.");

        rb.constraints = RigidbodyConstraints.None;

        while (elapsed < duration)
        {
            rb.rotation = Quaternion.Slerp(startRotation, targetRotation, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        rb.rotation = targetRotation;

        Debug.Log("[SmoothRecovery] Rotation complete. Re-enabling gravity and applying downward nudge.");

        rb.velocity = new Vector3(rb.velocity.x, -0.1f, rb.velocity.z);
        rb.useGravity = true;
        rb.WakeUp();

        rb.constraints = RigidbodyConstraints.None;

        Debug.Log("[SmoothRecovery] Final gravity: " + rb.useGravity + ", Constraints: " + rb.constraints);
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
        Rigidbody projRb = projectile.GetComponent<Rigidbody>();
        if (projRb != null)
        {
            projRb.velocity = firePoint.forward * projectileSpeed;
        }
    }
}
