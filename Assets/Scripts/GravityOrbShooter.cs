using UnityEngine;
using Cinemachine;

public class GravityOrbShooter : MonoBehaviour
{
    [Header("Shooter Settings")]
    [Tooltip("The orb prefab that must have a GravityOrb component attached.")]
    public GameObject orbPrefab;

    [Header("Player Reference")]
    [Tooltip("Used to add the player's forward/backward and scaled sideways momentum to the throw and trajectory.")]
    public Rigidbody playerRigidbody;

    [Tooltip("Fraction of sideways (left/right) player momentum to add to the orb.")]
    [Range(0f, 1f)]
    public float lateralMomentumScale = 0.5f;

    [Tooltip("How quickly the preview momentum adapts to changes (higher is snappier).")]
    public float momentumSmoothSpeed = 5f;

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

    // Private state
    private GameObject activeOrb = null;
    private bool isCharging = false;
    private bool chargingPullMode = false;
    private float currentChargeTime = 0f;
    private Vector3 smoothedMomentum = Vector3.zero;

    // Optional shared flag if you need to block a second pull-shot in the same frame.
    public static bool leftClickConsumed = false;

    // Public property to indicate if an orb is currently held.
    [HideInInspector] public bool IsOrbHeld { get { return activeOrb != null; } }

    private void Start()
    {
        if (aimTransform == null)
        {
            if (Camera.main != null)
                aimTransform = Camera.main.transform;
            else
                Debug.LogError("GravityOrbShooter: No aimTransform assigned and no main camera found!");
        }

        if (trajectoryLine != null)
            trajectoryLine.enabled = false;

        impulseSource?.GenerateImpulse();
    }

    private void Update()
    {
        if (activeOrb != null)
        {
            UpdateHoldPosition();
            HandleCharging();
        }
        else if (Input.GetKeyDown(summonKey))
        {
            SummonOrb();
        }
    }

    private void UpdateHoldPosition()
    {
        if (holdPosition != null && aimTransform != null)
        {
            Vector3 targetPos = aimTransform.TransformPoint(holdPositionOffset);
            Quaternion targetRot = aimTransform.rotation;
            holdPosition.position = Vector3.Lerp(holdPosition.position, targetPos, Time.deltaTime * holdSmoothSpeed);
            holdPosition.rotation = Quaternion.Slerp(holdPosition.rotation, targetRot, Time.deltaTime * holdSmoothSpeed);
        }
    }

    private void HandleCharging()
    {
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

        if (isCharging)
        {
            currentChargeTime = Mathf.Min(currentChargeTime + Time.deltaTime, maxChargeTime);
            ShowTrajectory(currentChargeTime / maxChargeTime);
        }

        if (isCharging && ((chargingPullMode && Input.GetKeyUp(pullKey)) || (!chargingPullMode && Input.GetKeyUp(pushKey))))
        {
            FireOrb(chargingPullMode, currentChargeTime / maxChargeTime);
            isCharging = false;
            if (trajectoryLine != null)
                trajectoryLine.enabled = false;
        }
    }

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

        Vector3 initPos = aimTransform.TransformPoint(holdPositionOffset);
        Quaternion initRot = aimTransform.rotation;
        holdPosition.position = initPos;
        holdPosition.rotation = initRot;

        activeOrb = Instantiate(orbPrefab, holdPosition.position, holdPosition.rotation);
        activeOrb.transform.SetParent(holdPosition);

        if (activeOrb.TryGetComponent<Rigidbody>(out var rb))
            rb.isKinematic = true;

        if (activeOrb.TryGetComponent<GravityOrb>(out var orbScript))
        {
            orbScript.isHeld = true;
            orbScript.ownerShooter = this;
        }
    }

    private void ShowTrajectory(float chargePercent)
    {
        if (trajectoryLine == null || aimTransform == null || holdPosition == null)
            return;

        trajectoryLine.enabled = true;
        trajectoryLine.positionCount = trajectoryResolution;

        // 1) starting point
        Vector3 startPos = holdPosition.position;
        // 2) direction & speed
        float throwSpeed = Mathf.Lerp(minProjectileSpeed, maxProjectileSpeed, chargePercent);
        Vector3 throwDir = Quaternion.AngleAxis(-arcAngle, aimTransform.right) * aimTransform.forward;

        // 3) smooth momentum update
        smoothedMomentum = Vector3.Lerp(smoothedMomentum, GetPlayerMomentum(), Time.deltaTime * momentumSmoothSpeed);

        // 4) combined velocity
        Vector3 velocity = smoothedMomentum + throwDir * throwSpeed;

        // 5) draw parabola
        for (int i = 0; i < trajectoryResolution; i++)
        {
            float t = i * trajectoryTimeStep;
            Vector3 point = startPos + velocity * t + 0.5f * Physics.gravity * t * t;
            trajectoryLine.SetPosition(i, point);
        }
    }

    private void FireOrb(bool isPullMode, float chargePercent)
    {
        if (activeOrb == null)
            return;

        var orbScript = activeOrb.GetComponent<GravityOrb>();
        if (orbScript != null)
        {
            orbScript.isPull = isPullMode;
            orbScript.isHeld = false;
        }

        activeOrb.transform.SetParent(null);

        var rb = activeOrb.GetComponent<Rigidbody>() ?? activeOrb.AddComponent<Rigidbody>();
        rb.isKinematic = false;
        rb.useGravity = true;

        float throwSpeed = Mathf.Lerp(minProjectileSpeed, maxProjectileSpeed, chargePercent);
        Vector3 throwDir = Quaternion.AngleAxis(-arcAngle, aimTransform.right) * aimTransform.forward;
        Vector3 throwVelocity = throwDir * throwSpeed;

        // apply instantaneous raw momentum
        Vector3 rawMomentum = GetPlayerMomentum();

        if (useImpulse)
            rb.AddForce(rawMomentum + throwVelocity, ForceMode.VelocityChange);
        else
            rb.velocity = rawMomentum + throwVelocity;

        rb.AddTorque(Random.insideUnitSphere * spinTorque, ForceMode.Impulse);
        rb.drag = orbDrag;
        rb.angularDrag = orbAngularDrag;

        impulseSource?.GenerateImpulse();
        activeOrb = null;
    }

    public void OnOrbActivatedWhileHeld()
    {
        isCharging = false;
        currentChargeTime = 0f;
        if (trajectoryLine != null)
            trajectoryLine.enabled = false;
    }

    /// <summary>
    /// Returns the player’s forward/backward momentum and a scaled component of sideways momentum.
    /// </summary>
    private Vector3 GetPlayerMomentum()
    {
        if (playerRigidbody == null || aimTransform == null)
            return Vector3.zero;

        Vector3 vel = playerRigidbody.velocity;
        Vector3 forwardAxis = aimTransform.forward.normalized;
        Vector3 forwardVel = forwardAxis * Vector3.Dot(vel, forwardAxis);

        Vector3 rightAxis = aimTransform.right.normalized;
        float lateralSpeed = Vector3.Dot(vel, rightAxis);
        Vector3 lateralVel = rightAxis * lateralSpeed * lateralMomentumScale;

        return forwardVel + lateralVel;
    }
}
