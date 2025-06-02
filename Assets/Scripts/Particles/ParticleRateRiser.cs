// ParticleRateRiser.cs
using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class ParticleRateRiser : MonoBehaviour
{
    [Header("Rate Settings")]
    [Tooltip("Particles/sec at t = 0")]
    public float startRate = 5f;
    [Tooltip("Particles/sec at the end of ramp")]
    public float endRate = 50f;
    [Tooltip("Seconds it takes to go from startRate to endRate")]
    public float rampDuration = 5f;

    private ParticleSystem.EmissionModule emission;
    private float elapsed = 0f;
    private bool isRamping = false;

    void Awake()
    {
        // Cache emission module
        ParticleSystem ps = GetComponent<ParticleSystem>();
        emission = ps.emission;

        // We assume the GameObject might start disabled, 
        // so we only set the rate if it’s already active.
        if (ps.isPlaying || ps.gameObject.activeSelf)
        {
            SetRate(startRate);
        }
    }

    void OnEnable()
    {
        // Whenever this GameObject is enabled, 
        // automatically start ramp from zero.
        RestartRamp();
    }

    void Update()
    {
        if (!isRamping)
            return;

        // Ramp until elapsed ≥ rampDuration
        if (elapsed < rampDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / rampDuration);
            float currentRate = Mathf.Lerp(startRate, endRate, t);
            SetRate(currentRate);
        }
        else
        {
            // Once we hit the end, clamp and stop updating
            SetRate(endRate);
            isRamping = false;
        }
    }

    /// <summary>
    /// Immediately reset the ramp (elapsed = 0) 
    /// and force the emission to startRate, then begin ramping.
    /// </summary>
    public void RestartRamp()
    {
        elapsed = 0f;
        isRamping = true;
        SetRate(startRate);

        // Also ensure the ParticleSystem is actually playing
        var ps = GetComponent<ParticleSystem>();
        if (!ps.isPlaying)
            ps.Play();
    }

    /// <summary>
    /// Convenience: assign a constant rate to the emission module.
    /// </summary>
    private void SetRate(float rate)
    {
        emission.rateOverTime = rate;
    }
}
