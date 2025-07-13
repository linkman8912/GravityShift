using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class WindSoundController : MonoBehaviour
{
    [Header("Audio Settings")]
    [SerializeField] private AudioClip windSound;
    [SerializeField] private bool enablePitchEffect = true;

    [Header("Speed Thresholds")]
    [SerializeField] private float minSpeedThreshold = 10f; // Speed at which wind starts
    [SerializeField] private float maxSpeedThreshold = 50f; // Speed at which wind reaches max intensity

    [Header("Volume Settings")]
    [SerializeField] private float minVolume = 0f;
    [SerializeField] private float maxVolume = 0.7f;
    [SerializeField] private float volumeFadeSpeed = 2f; // How fast volume changes

    [Header("Pitch Settings")]
    [SerializeField] private float basePitch = 1f;
    [SerializeField] private float minPitch = 0.8f;
    [SerializeField] private float maxPitch = 1.3f;
    [SerializeField] private float pitchFadeSpeed = 3f; // How fast pitch changes

    [Header("Smoothing")]
    [SerializeField] private float speedSmoothingFactor = 5f; // Smooths out speed reading

    [Header("References")]
    private Rigidbody rb;
    private AudioSource audioSource;
    private PlayerMovement playerMovement;

    // Internal variables
    private float currentVolume = 0f;
    private float currentPitch = 1f;
    private float targetVolume = 0f;
    private float targetPitch = 1f;
    private float smoothedSpeed = 0f;
    private bool wasPlayingLastFrame = false;

    void Start()
    {
        // Get components
        rb = GetComponent<Rigidbody>();
        audioSource = GetComponent<AudioSource>();
        playerMovement = GetComponent<PlayerMovement>();

        // Setup audio source
        if (windSound != null)
        {
            audioSource.clip = windSound;
            audioSource.loop = true; // Set to loop since it's 1 minute long
            audioSource.playOnAwake = false;
            audioSource.volume = 0f;
            audioSource.pitch = basePitch;
        }
        else
        {
            Debug.LogError("Wind sound clip not assigned!");
        }
    }

    void Update()
    {
        if (windSound == null || rb == null) return;

        // Calculate current speed (horizontal only, or include vertical if desired)
        float currentSpeed = CalculateSpeed();

        // Smooth the speed reading to avoid jarring changes
        smoothedSpeed = Mathf.Lerp(smoothedSpeed, currentSpeed, Time.deltaTime * speedSmoothingFactor);

        // Calculate target values based on speed
        CalculateTargetValues(smoothedSpeed);

        // Smoothly interpolate to target values
        InterpolateAudioValues();

        // Apply values to audio source
        ApplyAudioSettings();

        // Handle starting/stopping the audio
        HandleAudioPlayback();
    }

    float CalculateSpeed()
    {
        // Option 1: Horizontal speed only (recommended for most games)
        Vector3 horizontalVelocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        return horizontalVelocity.magnitude;

        // Option 2: Full 3D speed (uncomment if you want vertical speed to affect wind)
        // return rb.velocity.magnitude;
    }

    void CalculateTargetValues(float speed)
    {
        if (speed < minSpeedThreshold)
        {
            targetVolume = minVolume;
            targetPitch = basePitch;
        }
        else
        {
            // Calculate normalized speed (0-1) between thresholds
            float normalizedSpeed = Mathf.InverseLerp(minSpeedThreshold, maxSpeedThreshold, speed);
            normalizedSpeed = Mathf.Clamp01(normalizedSpeed);

            // Calculate volume based on speed
            targetVolume = Mathf.Lerp(minVolume, maxVolume, normalizedSpeed);

            // Calculate pitch based on speed (if enabled)
            if (enablePitchEffect)
            {
                targetPitch = Mathf.Lerp(minPitch, maxPitch, normalizedSpeed);
            }
            else
            {
                targetPitch = basePitch;
            }
        }
    }

    void InterpolateAudioValues()
    {
        // Smoothly interpolate volume
        currentVolume = Mathf.Lerp(currentVolume, targetVolume, Time.deltaTime * volumeFadeSpeed);

        // Smoothly interpolate pitch
        currentPitch = Mathf.Lerp(currentPitch, targetPitch, Time.deltaTime * pitchFadeSpeed);
    }

    void ApplyAudioSettings()
    {
        audioSource.volume = currentVolume;
        audioSource.pitch = currentPitch;
    }

    void HandleAudioPlayback()
    {
        bool shouldPlay = currentVolume > 0.01f; // Small threshold to avoid playing at near-zero volume

        if (shouldPlay && !audioSource.isPlaying)
        {
            audioSource.Play();
            wasPlayingLastFrame = true;
        }
        else if (!shouldPlay && audioSource.isPlaying && wasPlayingLastFrame)
        {
            // Only stop if volume has been low for a moment (prevents cutting out during brief stops)
            if (currentVolume < 0.005f)
            {
                audioSource.Stop();
                wasPlayingLastFrame = false;
            }
        }
    }

    // Optional: Public methods for external control
    public void SetVolumeMultiplier(float multiplier)
    {
        maxVolume = Mathf.Clamp01(maxVolume * multiplier);
    }

    public void SetPitchEnabled(bool enabled)
    {
        enablePitchEffect = enabled;
        if (!enabled)
        {
            targetPitch = basePitch;
        }
    }

    // Debug visualization in editor
    void OnDrawGizmosSelected()
    {
        if (Application.isPlaying && rb != null)
        {
            // Draw speed threshold spheres
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, minSpeedThreshold);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, maxSpeedThreshold);

            // Draw current speed
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, smoothedSpeed);
        }
    }
}