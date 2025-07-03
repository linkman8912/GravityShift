using UnityEngine;
using CameraShake;

public class CameraManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody playerRigidbody;

    [Header("Speed Shake Settings")]
    [SerializeField] private float maxSpeed = 20f;
    [SerializeField] private Vector3 positionStrength = new Vector3(1, 1, 0.5f);
    [SerializeField] private Vector3 rotationStrength = new Vector3(1, 1, 0.3f);

    private PerlinShake dynamicShake;

    void Start()
    {
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
        if (playerRigidbody == null || dynamicShake == null) return;

        float speed = playerRigidbody.velocity.magnitude;
        float normalizedSpeed = Mathf.InverseLerp(0f, maxSpeed, speed);
        dynamicShake.AmplitudeController.SetTargetAmplitude(normalizedSpeed);
    }
}
