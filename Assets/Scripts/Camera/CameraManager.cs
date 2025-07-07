using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using CameraShake;

public class CameraManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody playerRigidbody;
    [SerializeField] private Volume postProcessingVolume;

    [Header("Speed Shake Settings")]
    [SerializeField] private float maxSpeed = 20f;
    [SerializeField] private Vector3 positionStrength = new Vector3(1, 1, 0.5f);
    [SerializeField] private Vector3 rotationStrength = new Vector3(1, 1, 0.3f);

    [Header("Lens Distortion Settings")]
    [SerializeField] private float maxDistortion = -0.5f;
    [SerializeField] private float distortionLerpSpeed = 5f;

    [Header("FOV Kick Settings")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private float baseFOV = 60f;
    [SerializeField] private float maxFOV = 85f;
    [SerializeField] private float fovLerpSpeed = 5f;
    [SerializeField] private float fovSpeedThreshold = 5f;



    private PerlinShake dynamicShake;
    private LensDistortion lensDistortion;

    private float extraFOVBoost = 0f;
    public void SetFOVBoost(float boost) => extraFOVBoost = boost;


    void Start()
    {
        // Grab LensDistortion from the volume
        if (postProcessingVolume != null && postProcessingVolume.profile.TryGet(out lensDistortion))
        {
            lensDistortion.intensity.Override(0f);
        }

        var noiseModes = new[]
        {
            new PerlinShake.NoiseMode(8f, 1f),
            new PerlinShake.NoiseMode(20f, 0.3f)
        };

        var envelopeParams = new Envelope.EnvelopeParams
        {
            attack = 5f,
            decay = 5f,
            degree = Degree.Cubic
        };

        var shakeParams = new PerlinShake.Params
        {
            strength = new Displacement(positionStrength, rotationStrength),
            noiseModes = noiseModes,
            envelope = envelopeParams
        };

        dynamicShake = new PerlinShake(shakeParams, maxAmplitude: 0f, manualStrengthControl: true);
        CameraShaker.Instance.RegisterShake(dynamicShake);
    }

    void Update()
    {
        if (playerRigidbody == null || dynamicShake == null || lensDistortion == null) return;
        float speed = playerRigidbody.velocity.magnitude;
        float normalizedSpeed = Mathf.InverseLerp(0f, maxSpeed, speed);

        // Update shake
        dynamicShake.AmplitudeController.SetTargetAmplitude(normalizedSpeed);

        // Update lens distortion
        if (lensDistortion != null)
        {
            float targetDistortion = normalizedSpeed * maxDistortion;
            float currentDistortion = lensDistortion.intensity.value;
            lensDistortion.intensity.Override(Mathf.Lerp(currentDistortion, targetDistortion, Time.deltaTime * distortionLerpSpeed));
        }

        // Update FOV only if above speed threshold
        if (mainCamera != null)
        {
            float targetFOV = baseFOV;

            if (speed >= fovSpeedThreshold)
            {
                normalizedSpeed = Mathf.InverseLerp(fovSpeedThreshold, maxSpeed, speed);
                targetFOV = Mathf.Lerp(baseFOV, maxFOV, normalizedSpeed);
            }

            targetFOV += extraFOVBoost;
            mainCamera.fieldOfView = Mathf.Lerp(mainCamera.fieldOfView, targetFOV, Time.deltaTime * fovLerpSpeed);
        }


    }
}
