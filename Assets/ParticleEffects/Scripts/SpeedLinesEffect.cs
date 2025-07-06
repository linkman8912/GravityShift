using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class SpeedLinesEffect : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Your player or camera Rigidbody to sample velocity from.")]
    public Rigidbody playerRigidbody;
    private ParticleSystem ps;
    private Transform camTransform;

    [Header("Speed Thresholds")]
    public float minSpeed = 5f;
    public float maxSpeed = 50f;

    [Header("Emission Settings")]
    public float minEmission = 0f;
    public float maxEmission = 200f;

    [Header("Line Velocity")]
    public float minLineSpeed = 10f;
    public float maxLineSpeed = 100f;
    [Tooltip("Maximum horizontal angle (degrees) from camera forward for particle direction.")]
    public float maxHorizontalAngle = 35f;

    [Header("Lifetime Settings")]
    public float minLifetime = 0.5f;
    public float maxLifetime = 1.5f;

    [Header("Opacity")]
    [Range(0f, 1f)] public float minOpacity = 0f;
    [Range(0f, 1f)] public float maxOpacity = 1f;

    private ParticleSystem.EmissionModule emissionModule;
    private ParticleSystem.MainModule mainModule;
    private ParticleSystem.VelocityOverLifetimeModule velocityModule;

    void Awake()
    {
        ps = GetComponent<ParticleSystem>();
        emissionModule = ps.emission;
        mainModule = ps.main;
        velocityModule = ps.velocityOverLifetime;
        velocityModule.enabled = true;
        velocityModule.space = ParticleSystemSimulationSpace.World;

        // Cache main camera transform
        if (Camera.main != null)
            camTransform = Camera.main.transform;
        else
            Debug.LogError("[SpeedLinesEffect] No Camera tagged 'MainCamera' found.");

        // Auto-assign Rigidbody if missing
        if (playerRigidbody == null)
        {
            playerRigidbody = GetComponentInParent<Rigidbody>();
            if (playerRigidbody == null)
                Debug.LogError("[SpeedLinesEffect] No Rigidbody assigned or found in parent.");
        }
    }

    void Update()
    {
        if (playerRigidbody == null || camTransform == null)
            return;

        Vector3 vel = playerRigidbody.velocity;
        float speed = vel.magnitude;
        float tSpeed = Mathf.InverseLerp(minSpeed, maxSpeed, speed);
        float speedScaled = Mathf.Lerp(minLineSpeed, maxLineSpeed, tSpeed);

        // Compute normalized movement components
        Vector3 velNorm = speed > 0.01f ? vel / speed : Vector3.zero;
        float verticalComp = velNorm.y;
        Vector3 horizontalVel = new Vector3(velNorm.x, 0f, velNorm.z);
        float hMag = horizontalVel.magnitude;
        Vector3 horizontalDir = hMag > 0.01f ? horizontalVel / hMag : Vector3.zero;

        // Camera forward on horizontal plane
        Vector3 camForwardH = camTransform.forward;
        camForwardH.y = 0f;
        camForwardH.Normalize();

        // Angle between camera forward and movement dir
        float yawAngle = Vector3.SignedAngle(camForwardH, horizontalDir, Vector3.up);
        float clampedYaw = Mathf.Clamp(yawAngle, -maxHorizontalAngle, maxHorizontalAngle);

        // Clamped horizontal direction from camera forward
        Vector3 clampedHorizDir = Quaternion.AngleAxis(clampedYaw, Vector3.up) * camForwardH;

        // Build final world velocity: horizontal + vertical
        Vector3 worldDir = clampedHorizDir * hMag + Vector3.up * verticalComp;
        Vector3 worldVel = worldDir.normalized * speedScaled * -1f;

        // Apply modules
        emissionModule.rateOverTime = new ParticleSystem.MinMaxCurve(Mathf.Lerp(minEmission, maxEmission, tSpeed));
        mainModule.startLifetime = Mathf.Lerp(minLifetime, maxLifetime, tSpeed);

        velocityModule.x = new ParticleSystem.MinMaxCurve(worldVel.x);
        velocityModule.y = new ParticleSystem.MinMaxCurve(worldVel.y);
        velocityModule.z = new ParticleSystem.MinMaxCurve(worldVel.z);

        Color baseCol = mainModule.startColor.color;
        baseCol.a = Mathf.Lerp(minOpacity, maxOpacity, tSpeed);
        mainModule.startColor = new ParticleSystem.MinMaxGradient(baseCol);
    }
}
