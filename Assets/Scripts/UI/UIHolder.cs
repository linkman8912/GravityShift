// UIHolder.cs
using UnityEngine;
using UnityEngine.UI;

public class UIHolder : MonoBehaviour
{
    [Header("Crosshair Settings")]
    [SerializeField] private Image crosshair;
    [SerializeField] private Grappling grapplingScript;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private TimeManager timeManager;

    [Header("Crosshair Feedback")]
    [SerializeField] private float minScale = 0.5f;
    [SerializeField] private float maxScale = 1.5f;
    [SerializeField] private float inRangeOpacity = 1f;
    [SerializeField] private float outOfRangeOpacity = 0.3f;
    [SerializeField] private float transitionSpeed = 5f;
    [SerializeField] private float visualFeedbackRange = 100f; // Distance at which size scaling starts

    [Header("Pull Bar Settings")]
    [SerializeField] private Image pullBarImage;
    [SerializeField] private float maxFillAmount = 0.5f; // 0.5 for semi-circle, 1.0 for full circle
    [SerializeField] private float fadeSpeed = 3f; // How fast the pull bar fades in/out
    [SerializeField] private float hideDelayAfterFull = 2f; // Seconds to wait after full regen before hiding

    private float targetAlpha = 0f;
    private float currentAlpha = 0f;
    private float hideTimer = 0f;
    private bool wasRegenerating = false;
    private bool wasPulling = false;

    [Header("Time Slow Bar Settings")]
    [SerializeField] private Image timeSlowBarImage;
    [SerializeField] private float timeSlowMaxFillAmount = 0.5f; // 0.5 for semi-circle, 1.0 for full circle
    [SerializeField] private float timeSlowFadeSpeed = 3f; // How fast the time slow bar fades in/out
    [SerializeField] private float timeSlowHideDelayAfterFull = 2f; // Seconds to wait after full regen before hiding

    private float timeSlowTargetAlpha = 0f;
    private float timeSlowCurrentAlpha = 0f;
    private float timeSlowHideTimer = 0f;
    private bool wasTimeSlowRegenerating = false;
    private bool wasTimeSlowing = false;

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

        if (playerMovement == null)
            playerMovement = FindObjectOfType<PlayerMovement>();

        if (timeManager == null)
            timeManager = FindObjectOfType<TimeManager>();

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

        // Initialize time slow bar
        InitializeTimeSlowBar();

        // Validation
        if (crosshair == null)
            Debug.LogError("UIHolder: Crosshair Image not found! Make sure there's a UI Image named 'Crosshair' in your scene.");
        if (grapplingScript == null)
            Debug.LogError("UIHolder: Grappling script not found!");
        if (playerCamera == null)
            Debug.LogError("UIHolder: Player camera not found!");
        if (pullBarImage == null)
            Debug.LogError("UIHolder: Pull Bar Image not found! Please assign it in the inspector.");
        if (playerMovement == null)
            Debug.LogError("UIHolder: PlayerMovement component not found on player!");
        if (timeSlowBarImage == null)
            Debug.LogError("UIHolder: Time Slow Bar Image not found! Please assign it in the inspector.");
        if (timeManager == null)
            Debug.LogError("UIHolder: TimeManager component not found!");
    }

    void Update()
    {
        UpdateCrosshairFeedback();
        UpdatePullBarVisibility();
        UpdateTimeSlowBarVisibility();
    }

    void InitializePullBar()
    {
        if (pullBarImage == null)
        {
            // Try to find pull bar image if not assigned
            pullBarImage = GameObject.Find("PullBarImage")?.GetComponent<Image>();
        }

        if (pullBarImage != null)
        {
            // Set initial fill amount to max (full pull budget)
            pullBarImage.fillAmount = maxFillAmount;

            // Start with pull bar invisible
            currentAlpha = 0f;
            targetAlpha = 0f;
            UpdatePullBarAlpha();
        }
    }

    void InitializeTimeSlowBar()
    {
        if (timeSlowBarImage == null)
        {
            // Try to find time slow bar image if not assigned
            timeSlowBarImage = GameObject.Find("TimeSlowBarImage")?.GetComponent<Image>();
        }

        if (timeSlowBarImage != null)
        {
            // Set initial fill amount to max (full time slow budget)
            timeSlowBarImage.fillAmount = timeSlowMaxFillAmount;

            // Start with time slow bar invisible
            timeSlowCurrentAlpha = 0f;
            timeSlowTargetAlpha = 0f;
            UpdateTimeSlowBarAlpha();
        }
    }

    public void SetPull(float currentPullTime)
    {
        if (pullBarImage != null && grapplingScript != null)
        {
            // Calculate fill amount based on current pull time vs max pull budget
            float fillPercentage = currentPullTime / grapplingScript.pullBudget;
            pullBarImage.fillAmount = fillPercentage * maxFillAmount;
        }
    }

    public void SetTimeSlowMeter(float currentTimeSlowMeter)
    {
        if (timeSlowBarImage != null && timeManager != null)
        {
            // Calculate fill amount based on current time slow meter vs max
            float fillPercentage = timeManager.GetMeterPercent();
            timeSlowBarImage.fillAmount = fillPercentage * timeSlowMaxFillAmount;
        }
    }

    void UpdatePullBarVisibility()
    {
        if (pullBarImage == null || grapplingScript == null || playerMovement == null)
            return;

        bool isCurrentlyPulling = grapplingScript.isGrappling() && Input.GetKey(grapplingScript.pullKey);
        bool isCurrentlyRegenerating = IsRegenerating();
        bool isPullBarFull = Mathf.Approximately(grapplingScript.pullBudget, GetCurrentPullTime());

        // Check if we just finished regenerating
        if (wasRegenerating && !isCurrentlyRegenerating && isPullBarFull)
        {
            hideTimer = hideDelayAfterFull;
        }

        // Determine if pull bar should be visible
        // Show if: pulling, regenerating, not full, or within hide delay after becoming full
        bool shouldShow = isCurrentlyPulling || isCurrentlyRegenerating || !isPullBarFull || hideTimer > 0;

        if (shouldShow)
        {
            targetAlpha = 1f;
        }
        else
        {
            targetAlpha = 0f;
        }

        // Update hide timer
        if (hideTimer > 0)
        {
            hideTimer -= Time.deltaTime;
        }

        // Smooth alpha transition
        currentAlpha = Mathf.Lerp(currentAlpha, targetAlpha, Time.deltaTime * fadeSpeed);
        UpdatePullBarAlpha();

        // Store previous states
        wasRegenerating = isCurrentlyRegenerating;
        wasPulling = isCurrentlyPulling;
    }

    void UpdateTimeSlowBarVisibility()
    {
        if (timeSlowBarImage == null || timeManager == null)
            return;

        bool isCurrentlySlowing = Input.GetKey(KeyCode.LeftShift) && timeManager.GetMeterPercent() > 0;
        bool isCurrentlyRegeneratingTimeSlow = IsTimeSlowRegenerating();
        bool isTimeSlowBarFull = Mathf.Approximately(1f, timeManager.GetMeterPercent());

        // Check if we just finished regenerating
        if (wasTimeSlowRegenerating && !isCurrentlyRegeneratingTimeSlow && isTimeSlowBarFull)
        {
            timeSlowHideTimer = timeSlowHideDelayAfterFull;
        }

        // Determine if time slow bar should be visible
        // Show if: slowing time, regenerating, not full, or within hide delay after becoming full
        bool shouldShow = isCurrentlySlowing || isCurrentlyRegeneratingTimeSlow || !isTimeSlowBarFull || timeSlowHideTimer > 0;

        if (shouldShow)
        {
            timeSlowTargetAlpha = 1f;
        }
        else
        {
            timeSlowTargetAlpha = 0f;
        }

        // Update hide timer
        if (timeSlowHideTimer > 0)
        {
            timeSlowHideTimer -= Time.deltaTime;
        }

        // Smooth alpha transition
        timeSlowCurrentAlpha = Mathf.Lerp(timeSlowCurrentAlpha, timeSlowTargetAlpha, Time.deltaTime * timeSlowFadeSpeed);
        UpdateTimeSlowBarAlpha();

        // Update the time slow meter display
        SetTimeSlowMeter(timeManager.GetMeterPercent());

        // Store previous states
        wasTimeSlowRegenerating = isCurrentlyRegeneratingTimeSlow;
        wasTimeSlowing = isCurrentlySlowing;
    }

    void UpdatePullBarAlpha()
    {
        if (pullBarImage != null)
        {
            Color currentColor = pullBarImage.color;
            pullBarImage.color = new Color(currentColor.r, currentColor.g, currentColor.b, currentAlpha);
        }
    }

    void UpdateTimeSlowBarAlpha()
    {
        if (timeSlowBarImage != null)
        {
            Color currentColor = timeSlowBarImage.color;
            timeSlowBarImage.color = new Color(currentColor.r, currentColor.g, currentColor.b, timeSlowCurrentAlpha);
        }
    }

    bool IsRegenerating()
    {
        if (grapplingScript == null || playerMovement == null)
            return false;

        // Check if pull budget is less than max and player is grounded/wallrunning
        float currentPull = GetCurrentPullTime();
        bool canRegenerate = (playerMovement.grounded || playerMovement.wallrunning);

        return currentPull < grapplingScript.pullBudget && canRegenerate;
    }

    bool IsTimeSlowRegenerating()
    {
        if (timeManager == null)
            return false;

        // Check if time slow meter is less than max and not currently being used
        float currentMeter = timeManager.GetMeterPercent();
        bool notUsingTimeSlow = !Input.GetKey(KeyCode.LeftShift);

        return currentMeter < 1f && notUsingTimeSlow;
    }

    float GetCurrentPullTime()
    {
        if (grapplingScript == null) return 0f;
        return grapplingScript.pullBudgetTime;
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