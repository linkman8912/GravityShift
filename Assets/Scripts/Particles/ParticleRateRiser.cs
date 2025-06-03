// ParticleRateRiser.cs
using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class ParticleRateRiser : MonoBehaviour
{
    [Header("Rate Settings")]
    [Tooltip("Particles/sec at t = 0 when ramping up")]
    public float startRate = 5f;

    [Tooltip("Particles/sec at the end of ramp when ramping up")]
    public float endRate = 50f;

    [Tooltip("Seconds it takes to go from startRate → endRate (or endRate → 0 when ramping down)")]
    [Min(0f)]
    public float rampDuration = 3f;

    private ParticleSystem ps;
    private ParticleSystem.EmissionModule emission;

    // Separate timers, one for ramping up, one for ramping down.
    private float elapsedUp = 0f;
    private float elapsedDown = 0f;

    private bool isRampingUp = false;
    private bool isRampingDown = false;

    void Awake()
    {
        ps = GetComponent<ParticleSystem>();
        emission = ps.emission;

        // Make sure emission starts at zero (in case the GameObject was active).
        SetRate(0f);

        // If the GameObject is already active at Awake, begin ramping up immediately.
        if (gameObject.activeSelf)
        {
            BeginRampUp();
        }
    }

    void Update()
    {
        // 1) Handle ramping UP: startRate → endRate
        if (isRampingUp)
        {
            // Increment the up‐timer
            elapsedUp += Time.deltaTime;

            // If rampDuration is zero or elapsedUp already >= rampDuration, jump to endRate
            if (rampDuration <= 0f || elapsedUp >= rampDuration)
            {
                SetRate(endRate);
                isRampingUp = false;
            }
            else
            {
                float t = Mathf.Clamp01(elapsedUp / rampDuration);
                float cur = Mathf.Lerp(startRate, endRate, t);
                SetRate(cur);
            }
        }
        // 2) Handle ramping DOWN: endRate → 0
        else if (isRampingDown)
        {
            elapsedDown += Time.deltaTime;
            Debug.Log($"[ParticleRateRiser] RampingDown: elapsedDown = {elapsedDown:F3} / rampDuration = {rampDuration:F3}");

            // If rampDuration is zero (or elapsedDown ≥ rampDuration), immediately force zero + disable
            if (rampDuration <= 0f || elapsedDown >= rampDuration)
            {
                SetRate(0f);
                isRampingDown = false;

                // Wait one frame so you can see the “0” emission for at least one frame,
                // then disable the GameObject.
                // (We use Invoke instead of setting active immediately, so you don’t miss that last frame.)
                Invoke(nameof(DisableSelf), Time.deltaTime);
            }
            else
            {
                float t = Mathf.Clamp01(elapsedDown / rampDuration);
                float cur = Mathf.Lerp(endRate, 0f, t);
                SetRate(cur);
            }
        }
    }

    /// <summary>
    /// Public: Start (or restart) ramping UP from startRate → endRate.
    /// </summary>
    public void RestartRamp()
    {
        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        if (!ps.isPlaying)
            ps.Play();

        elapsedUp = 0f;
        isRampingDown = false;    // cancel any down‐ramp in progress
        isRampingUp = true;
        SetRate(startRate);
    }

    /// <summary>
    /// Public: Start ramping DOWN from endRate → 0 over `rampDuration`.
    /// After finishing, disables this GameObject.
    /// </summary>
    public void RampDown()
    {
        // If we were in the middle of ramping up, cancel that.
        Debug.Log($"[ParticleRateRiser] RampDown() called on {gameObject.name}, activeSelf = {gameObject.activeSelf}");
        isRampingUp = false;
        elapsedDown = 0f;
        isRampingDown = true;
        Debug.Log($"[ParticleRateRiser] RampDown() called on {gameObject.name}, activeSelf = {gameObject.activeSelf}");

        // Force the emission to full endRate so we start the down‐ramp from max.
        SetRate(endRate);

        if (!ps.isPlaying)
            ps.Play();
    }

    /// <summary>
    /// Helper to set rateOverTime on the emission module.
    /// </summary>
    private void SetRate(float r)
    {
        emission.rateOverTime = r;
    }

    /// <summary>
    /// Invoked by RampDown() once rampDuration has elapsed.
    /// Disables the entire GameObject so it’s invisible.
    /// </summary>
    private void DisableSelf()
    {
        gameObject.SetActive(false);
    }

    /// <summary>
    /// Used in Awake() if the GameObject starts active – automatically
    /// begin ramping up in that case.
    /// </summary>
    private void BeginRampUp()
    {
        elapsedUp = 0f;
        isRampingUp = true;
        SetRate(startRate);

        if (!ps.isPlaying)
            ps.Play();
    }
}
