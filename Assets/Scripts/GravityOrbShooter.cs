using UnityEngine;

public class GravityOrbShooter : MonoBehaviour
{
    [Header("Shooter Settings")]
    [Tooltip("The orb prefab that must have a GravityOrb component attached.")]
    public GameObject orbPrefab;

    [Tooltip("Transform used as the hold position when the orb is summoned (e.g., a point in front of the player's hand).")]
    public Transform holdPosition;

    [Tooltip("Transform used as the aim reference (usually the player's camera) for determining the shot direction.")]
    public Transform aimTransform;

    [Tooltip("Speed at which the orb is fired once launched.")]
    public float projectileSpeed = 20f;

    [Tooltip("Key used to summon the orb.")]
    public KeyCode summonKey = KeyCode.E;

    [Tooltip("Key used to shoot the orb for pulling effect (pulls objects in).")]
    public KeyCode pullKey = KeyCode.Mouse0; // Left mouse button

    [Tooltip("Key used to shoot the orb for pushing effect (pushes objects away).")]
    public KeyCode pushKey = KeyCode.Mouse1; // Right mouse button

    // Holds a reference to the currently summoned orb.
    private GameObject activeOrb = null;

    private void Start()
    {
        // If no aimTransform is assigned, try to use the main camera.
        if (aimTransform == null)
        {
            if (Camera.main != null)
            {
                aimTransform = Camera.main.transform;
            }
            else
            {
                Debug.LogError("GravityOrbShooter: No aimTransform assigned and no main camera found!");
            }
        }

        // (Optional) You can also check for holdPosition here.
        if (holdPosition == null)
        {
            Debug.LogError("GravityOrbShooter: holdPosition is not assigned!");
        }
    }

    private void Update()
    {
        // If no orb is currently summoned, check for summon input.
        if (activeOrb == null)
        {
            if (Input.GetKeyDown(summonKey))
            {
                SummonOrb();
            }
        }
        else // An orb is active and waiting to be fired.
        {
            if (Input.GetKeyDown(pullKey))
            {
                FireOrb(true);
            }
            else if (Input.GetKeyDown(pushKey))
            {
                FireOrb(false);
            }
        }
    }

    /// <summary>
    /// Instantiates the orb prefab at the hold position and parents it so that it stays with the player.
    /// </summary>
    private void SummonOrb()
    {
        if (orbPrefab == null || holdPosition == null)
        {
            Debug.LogWarning("GravityOrbShooter: orbPrefab or holdPosition is not assigned.");
            return;
        }

        // Instantiate the orb at the hold position.
        activeOrb = Instantiate(orbPrefab, holdPosition.position, holdPosition.rotation);
        // Parent it to the hold position so it moves with the player.
        activeOrb.transform.SetParent(holdPosition);

        // Disable physics until fired.
        Rigidbody rb = activeOrb.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
        }
    }

    /// <summary>
    /// Fires the summoned orb by detaching it, enabling physics, and applying an initial velocity.
    /// </summary>
    /// <param name="isPull">If true, the orb will pull objects in; if false, it will push objects away.</param>
    private void FireOrb(bool isPull)
    {
        if (activeOrb == null)
            return;

        // Detach the orb from the hold position.
        activeOrb.transform.SetParent(null);

        // Set the orb's mode (pull or push).
        GravityOrb orbScript = activeOrb.GetComponent<GravityOrb>();
        if (orbScript != null)
        {
            orbScript.isPull = isPull;
        }
        else
        {
            Debug.LogWarning("GravityOrbShooter: Summoned orb does not have a GravityOrb component.");
        }

        // Enable physics.
        Rigidbody rb = activeOrb.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = activeOrb.AddComponent<Rigidbody>();
        }
        rb.isKinematic = false;
        rb.useGravity = true;

        // Launch the orb in the forward direction of the aimTransform.
        rb.velocity = aimTransform.forward * projectileSpeed;

        // Clear the active orb reference.
        activeOrb = null;
    }
}
