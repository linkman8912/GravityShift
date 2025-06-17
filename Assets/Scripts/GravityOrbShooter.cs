using UnityEngine;
using Cinemachine;

public class GravityOrbShooter : MonoBehaviour
{
    [Header("Shooter Settings")]
    [Tooltip("The orb prefab that must have a GravityOrb component attached.")]
    public GameObject orbPrefab;

    [Tooltip("Transform used as the hold position when the orb is summoned (e.g., near the player's hand).")]
    public Transform holdPosition;

    [Tooltip("Offset from the camera in local space where the orb will appear while held.")]
    public Vector3 holdPositionOffset = new Vector3(1f, 0f, 2.5f);

    [Tooltip("Transform used as the aim reference (usually the player's camera) for determining the shot direction).")]
    public Transform aimTransform;

    [Header("Summon Settings")]
    [Tooltip("Key used to summon the orb.")]
    public KeyCode summonKey = KeyCode.E;

    [Header("Charge Throw Settings")]
    [Tooltip("Key used to shoot the orb for pulling effect (pulls objects in).")]
    public KeyCode pullKey = KeyCode.Mouse0;
    [Tooltip("Key used to shoot the orb for pushing effect (pushes objects away).")]
    public KeyCode pushKey = KeyCode.Mouse1;
    [Tooltip("Minimum speed applied when throw is uncharged.")]
    public float minProjectileSpeed = 5f;
    [Tooltip("Maximum speed applied when charge is full.")]
    public float maxProjectileSpeed = 20f;
    [Tooltip("Time in seconds it takes to reach maximum charge.")]
    public float maxChargeTime = 1.5f;

    [Header("Arc & Spin Settings")]
    [Tooltip("Angle above forward direction to lob the orb.")]
    public float arcAngle = 45f;
    [Tooltip("Use impulse mode for a snappier throw.")]
    public bool useImpulse = true;
    [Tooltip("How much random spin to add on throw.")]
    public float spinTorque = 1f;

    [Header("Drag Settings")]
    [Tooltip("Linear drag applied to orb on throw for a cleaner arc.")]
    public float orbDrag = 0.3f;
    [Tooltip("Angular drag applied to orb spin.")]
    public float orbAngularDrag = 0.3f;

    [Header("Hold Smoothing")]
    [Tooltip("Smoothing speed for smoothing the hold position movement and rotation.")]
    public float holdSmoothSpeed = 10f;

    [Header("Trajectory Preview Settings")]
    [Tooltip("LineRenderer used to draw the trajectory preview.")]
    public LineRenderer trajectoryLine;
    [Tooltip("Number of points in the trajectory preview.")]
    public int trajectoryResolution = 30;
    [Tooltip("Time step between each trajectory sample (seconds).")]
    public float trajectoryTimeStep = 0.1f;

    [Header("Camera Shake")]
    [Tooltip("The Cinemachine Impulse Source that generates the throw shake.")]
    public CinemachineImpulseSource impulseSource;

    // Optional shared flag if you need to block a second pull-shot in the same frame.
    public static bool leftClickConsumed = false;

    // Holds a reference to the currently summoned orb.
    private GameObject activeOrb = null;

    // Internal state for charging
    private bool isCharging = false;
    private bool chargingPullMode = false;
    private float currentChargeTime = 0f;

    // Public property to indicate if an orb is currently held.
    [HideInInspector] public bool IsOrbHeld { get { return activeOrb != null; } }

    private void Start()
    {
        // Auto-assign camera if none set
        if (aimTransform == null)
        {
            if (Camera.main != null)
                aimTransform = Camera.main.transform;
            else
                Debug.LogError("GravityOrbShooter: No aimTransform assigned and no main camera found!");
        }

        // Hide line initially
        if (trajectoryLine != null)
            trajectoryLine.enabled = false;

        // Optionally generate a startup impulse
        impulseSource?.GenerateImpulse();
    }

    private void Update()
    {
        if (activeOrb != null)
        {
            // Smoothly move & rotate holder to stay in front of camera
            if (holdPosition != null && aimTransform != null)
            {
                Vector3 targetPos = aimTransform.TransformPoint(holdPositionOffset);
                Quaternion targetRot = aimTransform.rotation;
                holdPosition.position = Vector3.Lerp(holdPosition.position, targetPos, Time.deltaTime * holdSmoothSpeed);
                holdPosition.rotation = Quaternion.Slerp(holdPosition.rotation, targetRot, Time.deltaTime * holdSmoothSpeed);
            }

            // Start charging on key down
            if (!isCharging && Input.GetKeyDown(pullKey))
            {
                isCharging = true;
                chargingPullMode = true;
                currentChargeTime = 0f;
                leftClickConsumed = false;
            }
            else if (!isCharging && Input.GetKeyDown(pushKey))
            {
                isCharging = true;
                chargingPullMode = false;
                currentChargeTime = 0f;
            }

            // Continue charging
            if (isCharging)
            {
                currentChargeTime += Time.deltaTime;
                currentChargeTime = Mathf.Min(currentChargeTime, maxChargeTime);

                // Update trajectory preview
                ShowTrajectory(currentChargeTime / maxChargeTime);
            }

            // Release to throw
            if (isCharging && ((chargingPullMode && Input.GetKeyUp(pullKey)) ||
                               (!chargingPullMode && Input.GetKeyUp(pushKey))))
            {
                float chargePercent = currentChargeTime / maxChargeTime;
                FireOrb(chargingPullMode, chargePercent);
                isCharging = false;

                // Hide trajectory after throw
                if (trajectoryLine != null)
                    trajectoryLine.enabled = false;
            }
        }
        else
        {
            if (Input.GetKeyDown(summonKey))
                SummonOrb();
        }
    }

    /// <summary>
    /// Instantiates the orb at the hold position and marks it as held.
    /// </summary>
    private void SummonOrb()
    {
        if (orbPrefab == null)
        {
            Debug.LogWarning("GravityOrbShooter: orbPrefab is not assigned.");
            return;
        }
        if (holdPosition == null || aimTransform == null)
        {
            Debug.LogWarning("GravityOrbShooter: holdPosition or aimTransform is missing.");
            return;
        }

        // Ensure holder is positioned correctly before spawning
        Vector3 initPos = aimTransform.TransformPoint(holdPositionOffset);
        Quaternion initRot = aimTransform.rotation;
        holdPosition.position = initPos;
        holdPosition.rotation = initRot;

        activeOrb = Instantiate(orbPrefab, holdPosition.position, holdPosition.rotation);
        activeOrb.transform.SetParent(holdPosition);

        Rigidbody rb = activeOrb.GetComponent<Rigidbody>();
        if (rb != null)
            rb.isKinematic = true;

        GravityOrb orbScript = activeOrb.GetComponent<GravityOrb>();
        if (orbScript != null)
        {
            orbScript.isHeld = true;
            orbScript.ownerShooter = this;
        }
    }

    /// <summary>
    /// Draws a trajectory preview based on current charge.
    /// </summary>
    private void ShowTrajectory(float chargePercent)
    {
        if (trajectoryLine == null || aimTransform == null || holdPosition == null)
            return;

        trajectoryLine.enabled = true;
        trajectoryLine.positionCount = trajectoryResolution;

        Vector3 startPos = holdPosition.position;
        float throwSpeed = Mathf.Lerp(minProjectileSpeed, maxProjectileSpeed, chargePercent);
        Vector3 forward = aimTransform.forward;
        Vector3 throwDir = Quaternion.AngleAxis(-arcAngle, aimTransform.right) * forward;
        Vector3 velocity = throwDir * throwSpeed;

        for (int i = 0; i < trajectoryResolution; i++)
        {
            float t = i * trajectoryTimeStep;
            Vector3 point = startPos + velocity * t + 0.5f * Physics.gravity * t * t;
            trajectoryLine.SetPosition(i, point);
        }
    }

    /// <summary>
    /// Fires the summoned orb by detaching it, enabling physics, and applying an initial arc, spin, and drag.
    /// </summary>
    private void FireOrb(bool isPullMode, float chargePercent)
    {
        if (activeOrb == null)
            return;

        activeOrb.transform.SetParent(null);

        GravityOrb orbScript = activeOrb.GetComponent<GravityOrb>();
        if (orbScript != null)
        {
            orbScript.isPull = isPullMode;
            orbScript.isHeld = false;
        }

        Rigidbody rb = activeOrb.GetComponent<Rigidbody>();
        if (rb == null)
            rb = activeOrb.AddComponent<Rigidbody>();

        rb.isKinematic = false;
        rb.useGravity = true;

        float throwSpeed = Mathf.Lerp(minProjectileSpeed, maxProjectileSpeed, chargePercent);
        Vector3 forward = aimTransform.forward;
        Vector3 throwDir = Quaternion.AngleAxis(-arcAngle, aimTransform.right) * forward;

        if (useImpulse)
            rb.AddForce(throwDir * throwSpeed, ForceMode.Impulse);
        else
            rb.velocity = throwDir * throwSpeed;

        rb.AddTorque(Random.insideUnitSphere * spinTorque, ForceMode.Impulse);
        rb.drag = orbDrag;
        rb.angularDrag = orbAngularDrag;

        impulseSource?.GenerateImpulse();

        activeOrb = null;
    }

    /// <summary>
    /// Called by the orb when it activates while still held to reset charge and trajectory.
    /// </summary>
    public void OnOrbActivatedWhileHeld()
    {
        isCharging = false;
        currentChargeTime = 0f;
        if (trajectoryLine != null)
            trajectoryLine.enabled = false;
    }
}
