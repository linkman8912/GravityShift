using UnityEngine;

public class GravityOrbShooter : MonoBehaviour
{
    [Header("Shooter Settings")]
    [Tooltip("The orb prefab that must have a GravityOrb component attached.")]
    public GameObject orbPrefab;

    [Tooltip("Transform used as the hold position when the orb is summoned (e.g., near the player's hand).")]
    public Transform holdPosition;

    [Tooltip("Transform used as the aim reference (usually the player's camera) for determining the shot direction.")]
    public Transform aimTransform;

    [Tooltip("Speed at which the orb is fired once launched.")]
    public float projectileSpeed = 20f;

    [Tooltip("Key used to summon the orb.")]
    public KeyCode summonKey = KeyCode.E;

    [Tooltip("Key used to shoot the orb for pulling effect (pulls objects in).")]
    public KeyCode pullKey = KeyCode.Mouse0;

    [Tooltip("Key used to shoot the orb for pushing effect (pushes objects away).")]
    public KeyCode pushKey = KeyCode.Mouse1;

    // Holds a reference to the currently summoned orb.
    private GameObject activeOrb = null;

    // Public property to indicate if an orb is currently held.
    public bool IsOrbHeld
    {
        get { return activeOrb != null; }
    }

    // Option 2: Shared flag to indicate left-click input has been consumed.
    public static bool leftClickConsumed = false;

    private void Start()
    {
        if (aimTransform == null)
        {
            if (Camera.main != null)
                aimTransform = Camera.main.transform;
            else
                Debug.LogError("GravityOrbShooter: No aimTransform assigned and no main camera found!");
        }
        if (holdPosition == null)
        {
            Debug.LogError("GravityOrbShooter: holdPosition is not assigned!");
        }
    }

    private void Update()
    {
        if (activeOrb != null)
        {
            // If orb is held, listen for left click to fire it.
            if (Input.GetKeyDown(pullKey))
            {
                FireOrb(true);
                leftClickConsumed = true;
            }
            else if (Input.GetKeyDown(pushKey))
            {
                FireOrb(false);
                leftClickConsumed = true;
            }
        }
        else
        {
            if (Input.GetKeyDown(summonKey))
            {
                SummonOrb();
            }
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
        Rigidbody rb = activeOrb.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
        }
        GravityOrb orbScript = activeOrb.GetComponent<GravityOrb>();
        if (orbScript != null)
        {
            orbScript.isHeld = true;
        }
    }

    /// <summary>
    /// Fires the summoned orb by detaching it, enabling physics, and applying an initial velocity.
    /// </summary>
    /// <param name="isPullMode">If true, the orb is set to pull; if false, to push.</param>
    private void FireOrb(bool isPullMode)
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
        rb.velocity = aimTransform.forward * projectileSpeed;
        activeOrb = null;
    }
}
