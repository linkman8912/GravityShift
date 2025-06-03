using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EnemyShooter : MonoBehaviour
{
    [Header("References")]
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
    public float ragdollDuration = 10f;
    public LayerMask groundLayer;

    [Header("Advanced Ragdoll Control")]
    [Tooltip("Layers that block recovery (and reset the initial delay).")]
    public LayerMask disableRecoveryLayer;

    [Header("Recovery Delay Settings")]
    [Tooltip("How long after hitting the ground before the ragdoll‐duration timer even begins.")]
    public float recoveryDelayDuration = 3f;

    // internals
    private Rigidbody rb;
    private bool isRagdoll = false;
    private bool isRecoveryDelayActive = false;
    private float recoveryDelayTimer = 0f;
    private float ragdollTimer = 0f;
    private bool isGrounded = false;
    private float fireCooldown = 0f;
    private HashSet<Collider> collisionStayObjects = new HashSet<Collider>();
    private int recoveryBlockContacts = 0;

    // <-- This flag comes from your grappling code:
    public bool IsGrappled { get; set; } = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        // Debug state each frame
        Debug.Log($"[State] Ragdoll={isRagdoll}, DelayActive={isRecoveryDelayActive}, " +
                  $"DelayT={recoveryDelayTimer:F2}, RagdollT={ragdollTimer:F2}, " +
                  $"Grounded={isGrounded}, Blocked={IsBlockedFromRecovery()}, Grappled={IsGrappled}");

        // ——— If we just got grappled (and are not already ragdolled), force ragdoll now ———
        if (!isRagdoll && IsGrappled)
        {
            EnterRagdoll();
            return;
        }

        if (isRagdoll)
        {
            // ————— PHASE 1: INITIAL DELAY BEFORE RAGDOLL DURATION —————
            if (isRecoveryDelayActive)
            {
                // === CHANGE STARTS HERE ===
                // As long as "IsGrappled" is true, keep resetting the delay timer.
                // Likewise, if touching a disable-recovery layer, also reset.
                if (IsBlockedFromRecovery() || IsGrappled)
                {
                    recoveryDelayTimer = recoveryDelayDuration;
                }
                else
                {
                    recoveryDelayTimer -= Time.deltaTime;
                }
                // === CHANGE ENDS HERE ===

                if (recoveryDelayTimer <= 0f)
                {
                    isRecoveryDelayActive = false;
                    ragdollTimer = ragdollDuration;
                    Debug.Log("[RecoveryDelay] elapsed; starting ragdoll-duration timer.");
                }
                return;
            }

            // ————— PHASE 2: RAGDOLL DURATION COUNTDOWN —————
            ragdollTimer -= Time.deltaTime;
            if (ragdollTimer > 0f)
            {
                Debug.Log($"[RagdollTimer] {ragdollTimer:F2}s remaining.");
            }
            else if (!IsGrappled && isGrounded && !IsBlockedFromRecovery())
            {
                Debug.Log("[Recovery] Conditions met; recovering.");
                RecoverFromRagdoll();
            }
            return;
        }

        // ————— Normal AI when not ragdolled —————
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
        return Vector3.Angle(transform.up, Vector3.up) > tiltThreshold;
    }

    bool IsBlockedFromRecovery()
    {
        return recoveryBlockContacts > 0;
    }

    void OnCollisionStay(Collision collision)
    {
        int layer = collision.gameObject.layer;

        // ground detection
        if (((1 << layer) & groundLayer) != 0)
            isGrounded = true;

        // recovery-block detection
        if (((1 << layer) & disableRecoveryLayer) != 0 &&
            !collisionStayObjects.Contains(collision.collider))
        {
            collisionStayObjects.Add(collision.collider);
            recoveryBlockContacts++;
        }
    }

    void OnCollisionExit(Collision collision)
    {
        int layer = collision.gameObject.layer;

        if (((1 << layer) & groundLayer) != 0)
            isGrounded = false;

        if (((1 << layer) & disableRecoveryLayer) != 0 &&
            collisionStayObjects.Contains(collision.collider))
        {
            collisionStayObjects.Remove(collision.collider);
            recoveryBlockContacts = Mathf.Max(0, recoveryBlockContacts - 1);
        }
    }

    void EnterRagdoll()
    {
        Debug.Log("[EnterRagdoll] Entering ragdoll state.");
        isRagdoll = true;
        isRecoveryDelayActive = true;
        recoveryDelayTimer = recoveryDelayDuration;
        ragdollTimer = ragdollDuration;

        rb.useGravity = true;
        rb.constraints = RigidbodyConstraints.None;
    }

    private IEnumerator ReleaseRotationLockAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        // Allow full physics again so they can tilt/ragdoll
        rb.constraints = RigidbodyConstraints.None;
        Debug.Log("[Recovery] Rotation locks released — can ragdoll again.");
    }

    void RecoverFromRagdoll()
    {
        isRagdoll = false;
        isRecoveryDelayActive = false;
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

        // snap upright & nudge down
        rb.rotation = targetRot;
        rb.velocity = new Vector3(rb.velocity.x, -0.1f, rb.velocity.z);
        rb.useGravity = true;
        rb.WakeUp();

        // freeze X/Z so we stay standing
        rb.constraints = RigidbodyConstraints.FreezeRotationX
                       | RigidbodyConstraints.FreezeRotationZ;

        // **after 2 seconds**, remove those locks so tilt can happen again
        StartCoroutine(ReleaseRotationLockAfterDelay(3f));

        Debug.Log("[SmoothRecovery] Upright and locked for 3s, then will re-enable ragdoll.");
    }

    void RotateBodyTowardsPlayer()
    {
        Vector3 dir = player.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) return;

        Quaternion look = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.Slerp(
            transform.rotation, look, bodyRotationSpeed * Time.deltaTime
        );
    }

    void RotateFirePointTowardsPlayer()
    {
        Vector3 dir = player.position - firePoint.position;
        if (dir.sqrMagnitude < 0.001f) return;

        Quaternion look = Quaternion.LookRotation(dir.normalized);
        firePoint.rotation = Quaternion.Slerp(
            firePoint.rotation, look, firePointRotationSpeed * Time.deltaTime
        );
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

        // 1) Instantiate using the firePoint’s rotation so forward is correct:
        GameObject p = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);

        // 2) Immediately apply whatever visual‐only rotation you need.
        //    Here we rotate 90° around the LOCAL forward axis (so it still flies the same way).
        p.transform.Rotate(90f, 0f, 0f, Space.Self);

        // 3) Give it velocity along firePoint.forward as before:
        if (p.TryGetComponent<Rigidbody>(out var prb))
            prb.velocity = firePoint.forward * projectileSpeed;
    }


}
