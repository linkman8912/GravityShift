using UnityEngine;
using System.Collections;

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
    [Tooltip("Surfaces that, if touched during ragdoll entry, delay the start of the ragdoll timer.")]
    public LayerMask disableRecoveryLayer;
    [Tooltip("How long to wait after entering ragdoll (and after last disable layer touch) before the ragdoll-duration timer runs.")]
    public float recoveryDelayDuration = 3f;

    private Rigidbody rb;
    private bool isRagdoll = false;
    private bool isGrounded = false;

    //–– New fields for staggered ragdoll timing ––
    private float recoveryDelayTimer = 0f;
    private bool waitingForRecoveryDelay = false;
    private bool ragdollTimerActive = false;
    private float ragdollTimer = 0f;

    public bool IsGrappled { get; set; } = false;
    private float fireCooldown;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        if (isRagdoll)
        {
            // 1) While in “delay” period, count down that delay
            if (waitingForRecoveryDelay)
            {
                recoveryDelayTimer -= Time.deltaTime;
                Debug.Log($"[RecoveryDelay] {recoveryDelayTimer:F2}s remaining before ragdoll timer starts.");

                if (recoveryDelayTimer <= 0f)
                {
                    waitingForRecoveryDelay = false;
                    ragdollTimerActive = true;
                    ragdollTimer = ragdollDuration;
                    Debug.Log("[RecoveryDelay] Delay elapsed. Starting ragdoll-duration timer.");
                }
                return;
            }

            // 2) Once ragdoll-duration has begun, count it down
            if (ragdollTimerActive)
            {
                ragdollTimer -= Time.deltaTime;
                Debug.Log($"[RagdollTimer] {ragdollTimer:F2}s remaining until recovery.");

                if (ragdollTimer <= 0f && !IsGrappled && isGrounded)
                {
                    RecoverFromRagdoll();
                }
            }

            return;
        }

        if (player == null) return;

        // Enter ragdoll if we tip over too far
        if (IsTilted())
        {
            EnterRagdoll();
            return;
        }

        // Otherwise: normal AI behavior
        RotateBodyTowardsPlayer();
        RotateFirePointTowardsPlayer();
        HandleShooting();
    }

    bool IsTilted()
    {
        float angle = Vector3.Angle(transform.up, Vector3.up);
        return angle > tiltThreshold;
    }

    void OnCollisionEnter(Collision collision)
    {
        int layer = collision.gameObject.layer;

        // Ground check
        if (((1 << layer) & groundLayer) != 0)
        {
            isGrounded = true;
        }

        // If we’re in that initial ragdoll-delay window and we hit a disable-recovery surface, reset the delay
        if (isRagdoll && waitingForRecoveryDelay &&
            ((1 << layer) & disableRecoveryLayer) != 0)
        {
            recoveryDelayTimer = recoveryDelayDuration;
            Debug.Log("[DisableRecovery] Resetting recovery-delay timer to full duration.");
        }
    }

    void OnCollisionExit(Collision collision)
    {
        int layer = collision.gameObject.layer;
        if (((1 << layer) & groundLayer) != 0)
        {
            isGrounded = false;
        }
    }

    void EnterRagdoll()
    {
        Debug.Log("[EnterRagdoll] Entering ragdoll.");

        isRagdoll = true;
        waitingForRecoveryDelay = true;
        recoveryDelayTimer = recoveryDelayDuration;
        ragdollTimerActive = false;

        rb.constraints = RigidbodyConstraints.None;
        rb.useGravity = true;
    }

    void RecoverFromRagdoll()
    {
        Debug.Log("[Recover] Conditions met—recovering from ragdoll.");
        isRagdoll = false;
        ragdollTimerActive = false;

        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        StartCoroutine(SmoothRecovery(3f));
    }

    IEnumerator SmoothRecovery(float duration)
    {
        Quaternion startRot = rb.rotation;
        Quaternion targetRot = Quaternion.Euler(0, startRot.eulerAngles.y, 0);
        float elapsed = 0f;

        rb.constraints = RigidbodyConstraints.None;

        while (elapsed < duration)
        {
            rb.rotation = Quaternion.Slerp(startRot, targetRot, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        rb.rotation = targetRot;
        rb.useGravity = true;
        rb.WakeUp();
        rb.constraints = RigidbodyConstraints.None;

        Debug.Log("[SmoothRecovery] Completed upright rotation.");
    }

    void RotateBodyTowardsPlayer()
    {
        Vector3 dir = player.position - transform.position;
        dir.y = 0f;
        if (dir == Vector3.zero) return;

        Quaternion lookRot = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, bodyRotationSpeed * Time.deltaTime);
    }

    void RotateFirePointTowardsPlayer()
    {
        Vector3 dir = player.position - firePoint.position;
        if (dir == Vector3.zero) return;

        Quaternion targetRotation = Quaternion.LookRotation(dir.normalized);
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

        GameObject proj = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);
        Rigidbody projRb = proj.GetComponent<Rigidbody>();
        if (projRb != null)
            projRb.velocity = firePoint.forward * projectileSpeed;
    }
}
