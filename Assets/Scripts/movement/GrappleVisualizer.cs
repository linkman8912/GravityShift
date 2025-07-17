using UnityEngine;
using System.Collections;

public class GrappleVisualizer : MonoBehaviour
{
    [Header("Grapple Hook Visual Settings")]
    [SerializeField] private GameObject customHookPrefab; // Drag your custom hook prefab here
    [SerializeField] private bool useCustomHook = false; // Toggle to use custom hook vs generated cylinder
    [SerializeField] private float hookShootSpeed = 50f;
    [SerializeField] private float hookRetractSpeed = 30f;
    [Header("Rope Physics")]
    [SerializeField] private int ropeSegments = 10; // Number of rope segments for sag calculation
    [SerializeField] private float sagIntensity = 2f; // How much the rope sags when slack
    [SerializeField] private float minTension = 0.1f; // Minimum tension (prevents over-sagging)
    [SerializeField] private float maxTension = 1f; // Maximum tension (rope becomes straight)
    [SerializeField] private bool enableRopeSag = true; // Toggle rope sag on/off
    [SerializeField] private float ropeThickness = 0.1f;
    [SerializeField] private Material ropeMaterial;
    [SerializeField] private Material hookMaterial;

    [Header("Animation Settings")]
    [SerializeField] private AnimationCurve shootCurve = new AnimationCurve(new Keyframe(0, 0, 0, 2), new Keyframe(1, 1, 0, 0));
    [SerializeField] private AnimationCurve retractCurve = new AnimationCurve(new Keyframe(0, 0, 0, 0), new Keyframe(1, 1, 2, 0));
    [SerializeField] private float impactEffectDuration = 0.3f;
    [SerializeField] private float impactScale = 1.5f;

    [Header("References")]
    [SerializeField] private Grappling grappling;
    [SerializeField] private Transform gunTip;
    [SerializeField] private Transform player;

    // Visual components
    private GameObject hookObject;
    private GameObject ropeObject;
    private LineRenderer ropeRenderer;
    private MeshRenderer hookRenderer;

    // Animation state
    private bool isShootingHook = false;
    private bool isRetractingHook = false;
    private bool hookAttached = false; // New flag to track if hook is attached
    private Vector3 targetGrapplePoint;
    private Vector3 currentHookPosition;
    private Vector3 attachedHookPosition; // Store the final attached position
    private Coroutine shootCoroutine;
    private Coroutine retractCoroutine;
    private Coroutine ropeUpdateCoroutine;

    void Start()
    {
        // Auto-find components if not assigned
        if (grappling == null)
            grappling = GetComponent<Grappling>();

        if (gunTip == null)
            gunTip = grappling != null ? grappling.gunTip : null;

        if (player == null)
            player = grappling != null ? grappling.player : null;

        // Debug check for missing references
        if (gunTip == null)
        {
            Debug.LogError("GrappleVisualizer: gunTip is null! Make sure it's assigned in the Grappling component.");
            return;
        }

        if (player == null)
        {
            Debug.LogError("GrappleVisualizer: player is null! Make sure it's assigned in the Grappling component.");
            return;
        }

        CreateVisualComponents();
        HideGrappleVisuals();
    }

    void CreateVisualComponents()
    {
        // Create hook object
        if (useCustomHook && customHookPrefab != null)
        {
            // Instantiate custom hook prefab
            hookObject = Instantiate(customHookPrefab);
            hookObject.name = "GrappleHook (Custom)";
            hookObject.transform.SetParent(transform);

            // Get or add MeshRenderer for the custom hook
            hookRenderer = hookObject.GetComponent<MeshRenderer>();
            if (hookRenderer == null)
            {
                // If no MeshRenderer found, try to find one in children
                hookRenderer = hookObject.GetComponentInChildren<MeshRenderer>();
            }
        }
        else
        {
            // Create default cylinder hook
            hookObject = new GameObject("GrappleHook (Generated)");
            hookObject.transform.SetParent(transform);

            // Add hook mesh (cylinder for the hook)
            MeshFilter hookMeshFilter = hookObject.AddComponent<MeshFilter>();
            hookMeshFilter.mesh = CreateHookMesh();

            hookRenderer = hookObject.AddComponent<MeshRenderer>();
        }

        // Apply hook material if available and renderer exists
        if (hookRenderer != null && hookMaterial != null)
        {
            hookRenderer.material = hookMaterial;
        }

        // Create rope object
        ropeObject = new GameObject("GrappleRope");
        ropeObject.transform.SetParent(transform);

        ropeRenderer = ropeObject.AddComponent<LineRenderer>();
        SetupRopeRenderer();
    }

    void SetupRopeRenderer()
    {
        ropeRenderer.material = ropeMaterial;
        ropeRenderer.startWidth = ropeThickness;
        ropeRenderer.endWidth = ropeThickness;
        ropeRenderer.positionCount = enableRopeSag ? ropeSegments + 1 : 2;
        ropeRenderer.useWorldSpace = true;
        ropeRenderer.sortingOrder = 1;
    }

    Mesh CreateHookMesh()
    {
        // Create a simple cylinder mesh for the hook
        GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Mesh mesh = cylinder.GetComponent<MeshFilter>().mesh;
        DestroyImmediate(cylinder);
        return mesh;
    }

    void Update()
    {
        // Check if grappling state changed
        if (grappling.isGrappling() && !isShootingHook && !IsHookVisible())
        {
            StartGrappleAnimation();
        }
        else if (grappling.isGrappling() && isShootingHook && targetGrapplePoint != grappling.getGrapplePoint())
        {
            // New grapple point while shooting - restart animation
            StartGrappleAnimation();
        }
        else if (!grappling.isGrappling() && (isShootingHook || IsHookVisible()))
        {
            StartRetractAnimation();
        }
    }

    public void StartGrappleAnimation()
    {
        // Stop any existing animations
        if (shootCoroutine != null)
            StopCoroutine(shootCoroutine);

        if (retractCoroutine != null)
            StopCoroutine(retractCoroutine);

        if (ropeUpdateCoroutine != null)
            StopCoroutine(ropeUpdateCoroutine);

        // Reset animation state
        isShootingHook = false;
        isRetractingHook = false;
        hookAttached = false; // Reset attachment state

        targetGrapplePoint = grappling.getGrapplePoint();
        currentHookPosition = gunTip.position;

        ShowGrappleVisuals();
        shootCoroutine = StartCoroutine(ShootHookAnimation());
    }

    public void StartRetractAnimation()
    {
        // Stop any existing animations
        if (shootCoroutine != null)
            StopCoroutine(shootCoroutine);

        if (ropeUpdateCoroutine != null)
            StopCoroutine(ropeUpdateCoroutine);

        // Reset animation state
        isShootingHook = false;
        hookAttached = false; // Hook is no longer attached

        // Use the stored attached position for retraction starting point
        if (hookObject != null)
            currentHookPosition = hookObject.transform.position;

        retractCoroutine = StartCoroutine(RetractHookAnimation());
    }

    IEnumerator ShootHookAnimation()
    {
        isShootingHook = true;
        float distance = Vector3.Distance(gunTip.position, targetGrapplePoint);
        float duration = distance / hookShootSpeed;
        float elapsed = 0f;

        Vector3 startPos = gunTip.position;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float curveValue = shootCurve.Evaluate(t);

            currentHookPosition = Vector3.Lerp(startPos, targetGrapplePoint, curveValue);
            UpdateHookPosition();
            UpdateRopeVisual();

            yield return null;
        }

        // Snap to final position and mark as attached
        attachedHookPosition = grappling.getGrapplePoint();
        currentHookPosition = attachedHookPosition;
        hookObject.transform.position = attachedHookPosition;
        hookAttached = true; // Hook is now attached and won't move again

        // Set initial rotation for the hook
        Vector3 direction = (gunTip.position - attachedHookPosition).normalized;
        if (direction != Vector3.zero)
        {
            hookObject.transform.rotation = Quaternion.LookRotation(direction);
        }

        // Impact effect
        StartCoroutine(ImpactEffect());

        isShootingHook = false;

        // Start continuous rope update
        ropeUpdateCoroutine = StartCoroutine(ContinuousRopeUpdate());
    }

    IEnumerator RetractHookAnimation()
    {
        isRetractingHook = true;
        float distance = Vector3.Distance(currentHookPosition, gunTip.position);
        float duration = distance / hookRetractSpeed;
        float elapsed = 0f;

        Vector3 startPos = currentHookPosition;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float curveValue = retractCurve.Evaluate(t);

            currentHookPosition = Vector3.Lerp(startPos, gunTip.position, curveValue);
            UpdateHookPosition();
            UpdateRopeVisual();

            yield return null;
        }

        isRetractingHook = false;
        HideGrappleVisuals();
    }

    IEnumerator ImpactEffect()
    {
        Vector3 originalScale = hookObject.transform.localScale;
        Vector3 impactScaleVec = originalScale * impactScale;

        float elapsed = 0f;

        while (elapsed < impactEffectDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / impactEffectDuration;

            // Scale up then down
            float scaleMultiplier = Mathf.Sin(t * Mathf.PI);
            Vector3 currentScale = Vector3.Lerp(originalScale, impactScaleVec, scaleMultiplier);
            hookObject.transform.localScale = currentScale;

            yield return null;
        }

        hookObject.transform.localScale = originalScale;
    }

    IEnumerator ContinuousRopeUpdate()
    {
        while (grappling.isGrappling())
        {
            UpdateRopeVisual();

            // Only update hook rotation when attached
            if (hookAttached && hookObject != null && gunTip != null)
            {
                // Keep the hook at the attached position
                hookObject.transform.position = attachedHookPosition;

                // Update rotation to face the gun
                Vector3 direction = (gunTip.position - attachedHookPosition).normalized;
                if (direction != Vector3.zero)
                {
                    hookObject.transform.rotation = Quaternion.LookRotation(direction);
                }
            }
            yield return null;
        }
    }

    void UpdateHookPosition()
    {
        if (hookObject != null && gunTip != null)
        {
            // Only update position during shooting/retracting animations
            if (!hookAttached)
            {
                hookObject.transform.position = currentHookPosition;

                // Orient hook towards gun tip
                Vector3 direction = (gunTip.position - currentHookPosition).normalized;
                if (direction != Vector3.zero)
                {
                    hookObject.transform.rotation = Quaternion.LookRotation(direction);
                }
            }
            // If attached, position updates are handled in ContinuousRopeUpdate
        }
    }

    void UpdateRopeVisual()
    {
        if (ropeRenderer != null && gunTip != null)
        {
            if (enableRopeSag)
            {
                UpdateRopeWithSag();
            }
            else
            {
                // Simple straight line
                ropeRenderer.positionCount = 2;
                ropeRenderer.SetPosition(0, gunTip.position);
                ropeRenderer.SetPosition(1, hookAttached ? attachedHookPosition : currentHookPosition);
            }
        }
    }

    void UpdateRopeWithSag()
    {
        if (gunTip == null) return;

        Vector3 startPos = gunTip.position;
        Vector3 endPos = hookAttached ? attachedHookPosition : currentHookPosition;

        // Calculate tension based on distance vs original grapple distance
        float currentDistance = Vector3.Distance(startPos, endPos);
        float originalDistance = Vector3.Distance(gunTip.position, targetGrapplePoint);
        float tension = Mathf.Clamp(currentDistance / originalDistance, minTension, maxTension);

        // Calculate sag amount (inverse of tension)
        float sagAmount = (1f - tension) * sagIntensity;

        // Set rope segment count
        ropeRenderer.positionCount = ropeSegments + 1;

        // Calculate rope points with sag
        for (int i = 0; i <= ropeSegments; i++)
        {
            float t = (float)i / ropeSegments;

            // Linear interpolation between start and end
            Vector3 point = Vector3.Lerp(startPos, endPos, t);

            // Add sag using a parabolic curve
            float sagCurve = 4f * t * (1f - t); // Parabola that peaks at t=0.5
            Vector3 sagOffset = Vector3.down * sagCurve * sagAmount;

            // Apply gravity direction (in case player is on walls/ceiling)
            Vector3 gravityDirection = Physics.gravity.normalized;
            if (gravityDirection == Vector3.zero) gravityDirection = Vector3.down;

            point += gravityDirection * sagCurve * sagAmount;

            ropeRenderer.SetPosition(i, point);
        }
    }

    void ShowGrappleVisuals()
    {
        if (hookObject != null)
            hookObject.SetActive(true);

        if (ropeObject != null)
            ropeObject.SetActive(true);
    }

    void HideGrappleVisuals()
    {
        if (hookObject != null)
            hookObject.SetActive(false);

        if (ropeObject != null)
            ropeObject.SetActive(false);
    }

    bool IsHookVisible()
    {
        return hookObject != null && hookObject.activeInHierarchy;
    }

    void OnDestroy()
    {
        if (shootCoroutine != null)
            StopCoroutine(shootCoroutine);

        if (retractCoroutine != null)
            StopCoroutine(retractCoroutine);

        if (ropeUpdateCoroutine != null)
            StopCoroutine(ropeUpdateCoroutine);
    }
}