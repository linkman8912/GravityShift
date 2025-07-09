// UIHolder.cs
using UnityEngine;
using UnityEngine.UI;

public class UIHolder : MonoBehaviour
{
    [Header("Crosshair Settings")]
    [SerializeField] private Image crosshair;
    [SerializeField] private Grappling grapplingScript;

    [Header("Crosshair Feedback")]
    [SerializeField] private float minScale = 0.5f;
    [SerializeField] private float maxScale = 1.5f;
    [SerializeField] private float inRangeOpacity = 1f;
    [SerializeField] private float outOfRangeOpacity = 0.3f;
    [SerializeField] private float transitionSpeed = 5f;
    [SerializeField] private float visualFeedbackRange = 100f; // Distance at which size scaling starts

    [Header("Pull Bar Settings")]
    [SerializeField] private Slider pullBar;

    private Vector3 originalScale;
    private Color originalColor;
    private Camera playerCamera;

    void Start()
    {
        // Auto-find references if not set
        if (crosshair == null)
            crosshair = GameObject.Find("Crosshair")?.GetComponent<Image>();

        if (grapplingScript == null)
            grapplingScript = FindObjectOfType<Grappling>();

        if (crosshair != null)
        {
            originalScale = crosshair.transform.localScale;
            originalColor = crosshair.color;
        }

        // Get camera reference from grappling script
        if (grapplingScript != null && grapplingScript.camera != null)
        {
            playerCamera = grapplingScript.camera.GetComponent<Camera>();
        }

        // Initialize pull bar
        InitializePullBar();

        // Validation
        if (crosshair == null)
            Debug.LogError("UIHolder: Crosshair Image not found! Make sure there's a UI Image named 'Crosshair' in your scene.");
        if (grapplingScript == null)
            Debug.LogError("UIHolder: Grappling script not found!");
        if (playerCamera == null)
            Debug.LogError("UIHolder: Player camera not found!");
        if (pullBar == null)
            Debug.LogError("UIHolder: Pull Bar Slider not found! Please assign it in the inspector.");
    }

    void Update()
    {
        UpdateCrosshairFeedback();
    }

    void InitializePullBar()
    {
        if (pullBar == null)
        {
            // Try to find pull bar if not assigned
            pullBar = GameObject.Find("PullBar")?.GetComponent<Slider>();
        }

        if (pullBar != null && grapplingScript != null)
        {
            pullBar.maxValue = grapplingScript.pullBudget;
            pullBar.value = grapplingScript.pullBudget;
        }
    }

    public void SetPull(float time)
    {
        if (pullBar != null)
        {
            pullBar.value = time;
        }
    }

    void UpdateCrosshairFeedback()
    {
        if (crosshair == null || grapplingScript == null || playerCamera == null)
            return;

        // Perform raycast to check for grappable objects within visual feedback range
        RaycastHit hit;
        bool hitGrappleable = Physics.Raycast(
            playerCamera.transform.position,
            playerCamera.transform.forward,
            out hit,
            visualFeedbackRange,
            grapplingScript.whatIsGrappleable
        );

        float targetScale;
        float targetOpacity;

        if (hitGrappleable)
        {
            // Check if hit object is not the player
            if (hit.collider.transform.root != grapplingScript.player.transform)
            {
                float distance = hit.distance;

                // Check if within actual grapple range for opacity
                bool withinGrappleRange = distance <= grapplingScript.maxDistance;
                targetOpacity = withinGrappleRange ? inRangeOpacity : outOfRangeOpacity;

                // Scale based on distance within visual feedback range
                if (withinGrappleRange)
                {
                    // Within grapple range - use max scale
                    targetScale = maxScale;
                }
                else
                {
                    // Outside grapple range but within visual range - scale based on distance
                    float normalizedDistance = distance / visualFeedbackRange;
                    targetScale = Mathf.Lerp(maxScale, minScale, normalizedDistance);
                }
            }
            else
            {
                // Hit player, treat as out of range
                targetScale = minScale;
                targetOpacity = outOfRangeOpacity;
            }
        }
        else
        {
            // No grappable object in visual range
            targetScale = minScale;
            targetOpacity = outOfRangeOpacity;
        }

        // Apply smooth transitions
        Vector3 currentScale = crosshair.transform.localScale;
        Vector3 newScale = Vector3.Lerp(currentScale, originalScale * targetScale, Time.deltaTime * transitionSpeed);
        crosshair.transform.localScale = newScale;

        Color currentColor = crosshair.color;
        Color targetColor = new Color(originalColor.r, originalColor.g, originalColor.b, targetOpacity);
        crosshair.color = Color.Lerp(currentColor, targetColor, Time.deltaTime * transitionSpeed);
    }
}