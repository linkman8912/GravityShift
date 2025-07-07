using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class TimeManager : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private KeyCode timeSlowKey = KeyCode.LeftShift;
    [SerializeField] private float slowTimeScale = 0.3f;
    [SerializeField] private float normalTimeScale = 1f;
    [SerializeField] private float timeSlowMax = 5f;
    [SerializeField] private float drainRate = 1f;
    [SerializeField] private float regenRate = 0.5f;

    [Header("Post-Processing")]
    [SerializeField] private Volume postProcessingVolume;
    [SerializeField] private float blueHueIntensity = 20f;
    private ColorAdjustments colorAdjustments;

    [Header("Camera FOV Boost")]
    [SerializeField] private CameraManager cameraManager;
    [SerializeField] private float fovBoostDuringSlow = 15f;

    private float timeSlowMeter;
    private bool isSlowingTime;

    [Header("Blue Tint Settings")]
    [SerializeField] private Color normalTint = Color.white;
    [SerializeField] private Color timeSlowTint = new Color(0.7f, 0.85f, 1f);
    [SerializeField] private float tintLerpSpeed = 5f;

    private Color currentTint;


    void Start()
    {
        timeSlowMeter = timeSlowMax;
        currentTint = normalTint;

        if (postProcessingVolume != null && postProcessingVolume.profile.TryGet(out colorAdjustments))
        {
            colorAdjustments.colorFilter.overrideState = true;
            colorAdjustments.colorFilter.value = currentTint;
        }

    }

    void Update()
    {
        bool canSlow = timeSlowMeter > 0f;
        bool isHolding = Input.GetKey(timeSlowKey);

        if (isHolding && canSlow)
        {
            if (!isSlowingTime)
            {
                ActivateTimeSlow();
            }

            timeSlowMeter -= drainRate * Time.unscaledDeltaTime;
            if (timeSlowMeter <= 0f)
            {
                timeSlowMeter = 0f;
                DeactivateTimeSlow();
            }
        }
        else
        {
            if (isSlowingTime)
            {
                DeactivateTimeSlow();
            }

            if (timeSlowMeter < timeSlowMax)
                timeSlowMeter += regenRate * Time.unscaledDeltaTime;
        }

        timeSlowMeter = Mathf.Clamp(timeSlowMeter, 0f, timeSlowMax);

        if (colorAdjustments != null)
        {
            Color targetTint = isSlowingTime ? timeSlowTint : normalTint;
            currentTint = Color.Lerp(currentTint, targetTint, Time.unscaledDeltaTime * tintLerpSpeed);
            colorAdjustments.colorFilter.value = currentTint;
        }
    }

    private void ActivateTimeSlow()
    {
        Time.timeScale = slowTimeScale;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;
        isSlowingTime = true;

        if (colorAdjustments != null)
            colorAdjustments.colorFilter.value = new Color(0.7f, 0.85f, 1f); 

        if (cameraManager != null)
            cameraManager.SetFOVBoost(fovBoostDuringSlow);
    }

    private void DeactivateTimeSlow()
    {
        Time.timeScale = normalTimeScale;
        Time.fixedDeltaTime = 0.02f;
        isSlowingTime = false;

        if (colorAdjustments != null)
            colorAdjustments.colorFilter.value = Color.white;

        if (cameraManager != null)
            cameraManager.SetFOVBoost(0f);
    }

    public float GetMeterPercent()
    {
        return timeSlowMeter / timeSlowMax;
    }
}
