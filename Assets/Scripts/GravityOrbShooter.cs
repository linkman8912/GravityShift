using UnityEngine;
using Cinemachine;

public class GravityOrbShooter : MonoBehaviour
{
    [Header("Shooter Settings")]
    [Tooltip("The orb prefab that must have a GravityOrb component attached.")]
    public GameObject orbPrefab;
    [Tooltip("Transform used as the hold position when the orb is summoned (e.g., near the player's hand).")]
    public Transform holdPosition;
    [Tooltip("Transform used as the aim reference (usually the player's camera) for determining the shot direction).")]
    public Transform aimTransform;
    [Tooltip("Speed at which the orb is fired once launched.")]
    public float projectileSpeed = 20f;

    [Tooltip("Key used to summon the orb.")]
    public KeyCode summonKey = KeyCode.E;
    [Tooltip("Key used to shoot the orb for pulling effect (pulls objects in).")]
    public KeyCode pullKey = KeyCode.Mouse0;
    [Tooltip("Key used to shoot the orb for pushing effect (pushes objects away).")]
    public KeyCode pushKey = KeyCode.Mouse1;

    [Header("Arc & Spin Settings")]
    [Tooltip("Angle above forward direction to lob the orb.")]
    public float arcAngle = 45f;
    [Tooltip("Use Impulse mode for a snappier throw.")]
    public bool useImpulse = true;
    [Tooltip("How much random spin to add on throw.")]
    public float spinTorque = 1f;

    [Header("Drag Settings")]
    [Tooltip("Linear drag applied to orb on throw for a cleaner arc.")]
    public float orbDrag = 0.3f;
    [Tooltip("Angular drag applied to orb spin.")]
    public float orbAngularDrag = 0.3f;

    [Header("Camera Shake")]
    [Tooltip("The Cinemachine Impulse Source that generates the throw shake.")]
    public CinemachineImpulseSource impulseSource;

    // Optional shared flag if you need to block a second pull-shot in the same frame.
    public static bool leftClickConsumed = false;

    // Holds a reference to the currently summoned orb.
    private GameObject activeOrb = null;

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

        FindObjectOfType<CinemachineImpulseSource>()
    ?.GenerateImpulse();



    }

    private void Update()
    {
        if (activeOrb != null)
        {
            // Pull mode (left click)
            if (Input.GetKeyDown(pullKey))
            {
                FireOrb(true);
                leftClickConsumed = true;
            }
            // Push mode (right click)
            else if (Input.GetKeyDown(pushKey))
            {
                FireOrb(false);
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
        if (orbPrefab == null || holdPosition == null)
        {
            Debug.LogWarning("GravityOrbShooter: orbPrefab or holdPosition is not assigned.");
            return;
        }

        activeOrb = Instantiate(orbPrefab, holdPosition.position, holdPosition.rotation);
        activeOrb.transform.SetParent(holdPosition);

        // Freeze physics while held
        Rigidbody rb = activeOrb.GetComponent<Rigidbody>();
        if (rb != null)
            rb.isKinematic = true;

        // Tell the orb it's being held
        GravityOrb orbScript = activeOrb.GetComponent<GravityOrb>();
        if (orbScript != null)
            orbScript.isHeld = true;
    }

    /// <summary>
    /// Fires the summoned orb by detaching it, enabling physics, and applying an initial arc, spin, and drag.
    /// </summary>
    /// <param name="isPullMode">If true, sets the orb to pull; if false, to push.</param>
    private void FireOrb(bool isPullMode)
    {
        if (activeOrb == null)
            return;

        // Detach
        activeOrb.transform.SetParent(null);

        // Configure orb logic
        GravityOrb orbScript = activeOrb.GetComponent<GravityOrb>();
        if (orbScript != null)
        {
            orbScript.isPull = isPullMode;
            orbScript.isHeld = false;
        }

        // Ensure we have a Rigidbody
        Rigidbody rb = activeOrb.GetComponent<Rigidbody>();
        if (rb == null)
            rb = activeOrb.AddComponent<Rigidbody>();

        rb.isKinematic = false;
        rb.useGravity = true;

        // 1) Calculate a lob direction pitched upward by arcAngle
        Vector3 forward = aimTransform.forward;
        Vector3 throwDir = Quaternion.AngleAxis(-arcAngle, aimTransform.right) * forward;

        // 2) Launch with impulse or direct velocity
        if (useImpulse)
            rb.AddForce(throwDir * projectileSpeed, ForceMode.Impulse);
        else
            rb.velocity = throwDir * projectileSpeed;

        // 3) Give it some spin for feedback
        rb.AddTorque(Random.insideUnitSphere * spinTorque, ForceMode.Impulse);

        // 4) Tweak drag so it arcs cleanly
        rb.drag = orbDrag;
        rb.angularDrag = orbAngularDrag;

        if (impulseSource != null)
            impulseSource.GenerateImpulse();
        Debug.Log("Impulse");



        activeOrb = null;
    }
}
