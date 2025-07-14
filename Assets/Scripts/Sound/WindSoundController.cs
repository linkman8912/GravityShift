using UnityEngine;
using System.Collections.Generic;

public class WindSoundController : MonoBehaviour
{
    [System.Serializable]
    public class SoundEffect
    {
        public string name;
        public AudioClip[] clips; // Array for variations
        [Range(0f, 1f)] public float volume = 1f;
        [Range(0.1f, 3f)] public float pitch = 1f;
        [Header("Variation Settings")]
        [Range(0f, 0.3f)] public float pitchVariation = 0.1f;
        [Range(0f, 0.2f)] public float volumeVariation = 0.05f;
    }

    [Header("Sound Effects")]
    [SerializeField] private SoundEffect grappleShootSound;
    [SerializeField] private SoundEffect landingSound;
    [SerializeField] private SoundEffect wallrunSound;
    [SerializeField] private SoundEffect jumpSound;
    [SerializeField] private SoundEffect doubleJumpSound;

    [Header("Wind Sound Settings")]
    [SerializeField] private AudioClip windSound;
    [SerializeField] private bool enablePitchEffect = true;

    [Header("Speed Thresholds")]
    [SerializeField] private float minSpeedThreshold = 10f;
    [SerializeField] private float maxSpeedThreshold = 50f;

    [Header("Wind Volume Settings")]
    [SerializeField] private float minVolume = 0f;
    [SerializeField] private float maxVolume = 0.7f;
    [SerializeField] private float volumeFadeSpeed = 2f;

    [Header("Wind Pitch Settings")]
    [SerializeField] private float basePitch = 1f;
    [SerializeField] private float minPitch = 0.8f;
    [SerializeField] private float maxPitch = 1.3f;
    [SerializeField] private float pitchFadeSpeed = 3f;

    [Header("Smoothing")]
    [SerializeField] private float speedSmoothingFactor = 5f;

    [Header("Audio Pool Settings")]
    [SerializeField] private int audioSourcePoolSize = 5;

    [Header("References")]
    [SerializeField] private Grappling grapplingScript;

    // References
    private Rigidbody rb;
    private PlayerMovement playerMovement;
    private Wallrunning wallrunning;

    // Audio management
    private AudioSource windAudioSource;
    private List<AudioSource> audioSourcePool = new List<AudioSource>();
    private int currentPoolIndex = 0;

    // Wind sound variables
    private float currentVolume = 0f;
    private float currentPitch = 1f;
    private float targetVolume = 0f;
    private float targetPitch = 1f;
    private float smoothedSpeed = 0f;

    // State tracking
    private bool wasGrappling = false;
    private bool wasGrounded = false;
    private bool wasWallrunning = false;
    private int lastJumpFrame = -1;

    void Start()
    {
        // Get components
        rb = GetComponent<Rigidbody>();
        playerMovement = GetComponent<PlayerMovement>();
        wallrunning = GetComponent<Wallrunning>();

        // Use inspector reference for grappling if set, otherwise try to find it
        if (grapplingScript == null)
        {
            grapplingScript = GetComponent<Grappling>();
            if (grapplingScript == null)
            {
                Debug.LogWarning("Grappling script not found! Please assign it in the inspector.");
            }
        }

        // Setup wind audio source
        windAudioSource = gameObject.AddComponent<AudioSource>();
        SetupWindAudioSource();

        // Create audio source pool for one-shot sounds
        CreateAudioSourcePool();
    }

    void SetupWindAudioSource()
    {
        if (windSound != null)
        {
            windAudioSource.clip = windSound;
            windAudioSource.loop = true;
            windAudioSource.playOnAwake = false;
            windAudioSource.volume = 0f;
            windAudioSource.pitch = basePitch;
            windAudioSource.priority = 128; // Higher priority for continuous sound
        }
        else
        {
            Debug.LogWarning("Wind sound clip not assigned!");
        }
    }

    void CreateAudioSourcePool()
    {
        for (int i = 0; i < audioSourcePoolSize; i++)
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            source.priority = 256; // Lower priority for one-shot sounds
            audioSourcePool.Add(source);
        }
    }

    void Update()
    {
        HandleWindSound();
        HandleGrappleSound();
        HandleMovementSounds();
    }

    void HandleWindSound()
    {
        if (windSound == null || rb == null) return;

        // Calculate current speed
        float currentSpeed = CalculateSpeed();

        // Smooth the speed reading
        smoothedSpeed = Mathf.Lerp(smoothedSpeed, currentSpeed, Time.deltaTime * speedSmoothingFactor);

        // Calculate target values based on speed
        CalculateWindTargetValues(smoothedSpeed);

        // Smoothly interpolate to target values
        InterpolateWindAudioValues();

        // Apply values to audio source
        ApplyWindAudioSettings();

        // Handle starting/stopping the audio
        HandleWindAudioPlayback();
    }

    void HandleGrappleSound()
    {
        if (grapplingScript == null)
        {
            Debug.LogWarning("Grappling script reference is null!");
            return;
        }

        if (grappleShootSound.clips == null || grappleShootSound.clips.Length == 0)
        {
            Debug.LogWarning("Grapple sound clips array is empty!");
            return;
        }

        bool isGrappling = grapplingScript.isGrappling();

        // Detect new grapple
        if (isGrappling && !wasGrappling)
        {
            Debug.Log("Grapple detected! Playing sound...");
            PlayOneShot(grappleShootSound);
        }

        wasGrappling = isGrappling;
    }

    void HandleMovementSounds()
    {
        if (playerMovement == null) return;

        // Landing sound
        if (!wasGrounded && playerMovement.grounded && landingSound.clips != null && landingSound.clips.Length > 0)
        {
            // Calculate landing intensity based on fall speed
            float landingVolume = Mathf.Clamp01(Mathf.Abs(rb.velocity.y) / 20f);
            PlayOneShot(landingSound, landingVolume);
        }

        // Jump sounds
        if (Input.GetButtonDown("Jump") && Time.frameCount != lastJumpFrame)
        {
            lastJumpFrame = Time.frameCount;

            if (playerMovement.grounded && jumpSound.clips != null && jumpSound.clips.Length > 0)
            {
                PlayOneShot(jumpSound);
            }
            else if (!playerMovement.grounded && playerMovement.secondJump && doubleJumpSound.clips != null && doubleJumpSound.clips.Length > 0)
            {
                PlayOneShot(doubleJumpSound);
            }
        }

        // Wallrun sound (continuous while wallrunning)
        if (wallrunning != null && wallrunSound.clips != null && wallrunSound.clips.Length > 0)
        {
            if (playerMovement.wallrunning && !wasWallrunning)
            {
                // Start wallrun sound
                // This would need to be implemented as a looping sound
                // For now, we'll just play it once
                PlayOneShot(wallrunSound);
            }
            wasWallrunning = playerMovement.wallrunning;
        }

        wasGrounded = playerMovement.grounded;
    }

    void PlayOneShot(SoundEffect sound, float volumeMultiplier = 1f)
    {
        if (sound.clips == null || sound.clips.Length == 0) return;

        // Choose random clip from array
        AudioClip clipToPlay = sound.clips[Random.Range(0, sound.clips.Length)];
        if (clipToPlay == null) return;

        AudioSource source = GetNextAudioSource();
        source.clip = clipToPlay;

        // Apply random variations
        float pitchVariation = Random.Range(-sound.pitchVariation, sound.pitchVariation);
        float volumeVariation = Random.Range(-sound.volumeVariation, sound.volumeVariation);

        source.volume = (sound.volume + volumeVariation) * volumeMultiplier;
        source.pitch = sound.pitch + pitchVariation;

        source.Play();
    }

    AudioSource GetNextAudioSource()
    {
        AudioSource source = audioSourcePool[currentPoolIndex];

        // If the current source is playing, stop it (interruption behavior)
        if (source.isPlaying)
        {
            source.Stop();
        }

        currentPoolIndex = (currentPoolIndex + 1) % audioSourcePool.Count;
        return source;
    }

    float CalculateSpeed()
    {
        Vector3 horizontalVelocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        return horizontalVelocity.magnitude;
    }

    void CalculateWindTargetValues(float speed)
    {
        if (speed < minSpeedThreshold)
        {
            targetVolume = minVolume;
            targetPitch = basePitch;
        }
        else
        {
            float normalizedSpeed = Mathf.InverseLerp(minSpeedThreshold, maxSpeedThreshold, speed);
            normalizedSpeed = Mathf.Clamp01(normalizedSpeed);

            targetVolume = Mathf.Lerp(minVolume, maxVolume, normalizedSpeed);

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

    void InterpolateWindAudioValues()
    {
        currentVolume = Mathf.Lerp(currentVolume, targetVolume, Time.deltaTime * volumeFadeSpeed);
        currentPitch = Mathf.Lerp(currentPitch, targetPitch, Time.deltaTime * pitchFadeSpeed);
    }

    void ApplyWindAudioSettings()
    {
        windAudioSource.volume = currentVolume;
        windAudioSource.pitch = currentPitch;
    }

    void HandleWindAudioPlayback()
    {
        bool shouldPlay = currentVolume > 0.01f;

        if (shouldPlay && !windAudioSource.isPlaying)
        {
            windAudioSource.Play();
        }
        else if (!shouldPlay && windAudioSource.isPlaying && currentVolume < 0.005f)
        {
            windAudioSource.Stop();
        }
    }

    // Public methods for external control
    public void PlayGrappleSound()
    {
        if (grappleShootSound.clips != null && grappleShootSound.clips.Length > 0)
        {
            Debug.Log("Playing grapple sound!");
            PlayOneShot(grappleShootSound);
        }
    }

    public void PlayCustomSound(AudioClip clip, float volume = 1f, float pitch = 1f)
    {
        SoundEffect customSound = new SoundEffect
        {
            clips = new AudioClip[] { clip },
            volume = volume,
            pitch = pitch,
            pitchVariation = 0f,
            volumeVariation = 0f
        };
        PlayOneShot(customSound);
    }

    public void SetWindEnabled(bool enabled)
    {
        if (!enabled)
        {
            targetVolume = 0f;
        }
    }

    // Debug
    void OnDrawGizmosSelected()
    {
        if (Application.isPlaying && rb != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, minSpeedThreshold);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, maxSpeedThreshold);

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, smoothedSpeed);
        }
    }
}