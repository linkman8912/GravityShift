using UnityEngine;
using System.Collections;

public class GravityOrb : MonoBehaviour
{
    [Header("Projectile Settings")]
    [Tooltip("How many seconds the orb will live before auto-destroying if it doesn’t hit anything.")]
    public float lifeTime = 5f;

    [Tooltip("Radius within which the orb will affect nearby rigidbodies.")]
    public float effectRadius = 10f;

    [Tooltip("Force applied to nearby objects when pulling them toward the orb.")]
    public float pullForce = 50f;

    [Tooltip("Force applied to nearby objects when pushing them away from the orb.")]
    public float pushForce = 50f;

    [Tooltip("Upwards modifier applied with the explosion force for a more dynamic effect.")]
    public float upwardsModifier = 0f;

    [Tooltip("If true, the orb pulls objects in; if false, it pushes objects away.")]
    public bool isPull = false;

    [Header("Effect Durations")]
    [Tooltip("Duration (in seconds) for which the orb applies pull force after collision.")]
    public float pullDuration = 2f;

    [Tooltip("Indicates whether the orb is still held (i.e., not fired yet).")]
    public bool isHeld = false;

    [Header("Collision Masks")]
    [Tooltip("Layers on which the orb should NOT activate on collision.")]
    public LayerMask ignoreActivationLayers;

    [Tooltip("Layers which always count as 'ground' hits, even if also in Ignore Activation Layers.")]
    public LayerMask groundLayers;

    private Rigidbody _rb;
    private bool _effectTriggered = false;
    public GravityOrbShooter ownerShooter;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        int orbLayer = gameObject.layer;
        int playerLayer = LayerMask.NameToLayer("Player");
        Physics.IgnoreLayerCollision(orbLayer, playerLayer, true);
    }

    private void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    private void OnCollisionEnter(Collision collision)
    {
        int layer = collision.gameObject.layer;
        bool isIgnored = (ignoreActivationLayers.value & (1 << layer)) != 0;
        bool isGround = (groundLayers.value & (1 << layer)) != 0;
        if (isIgnored && !isGround)
            return;

        if (isHeld)
        {
            isPull = false;
            if (!_effectTriggered)
                TriggerGravityEffect();
            return;
        }

        if (!_effectTriggered)
        {
            if (_rb != null)
            {
                _rb.velocity = Vector3.zero;
                _rb.isKinematic = true;
            }
            TriggerGravityEffect();
        }
    }

    /// <summary>
    /// Triggers either a continuous pull or a one-time push impulse.
    /// </summary>
    public void TriggerGravityEffect()
    {
        _effectTriggered = true;
        if (isPull)
        {
            StartCoroutine(ApplyContinuousForce(pullDuration));
        }
        else
        {
            ApplyPushImpulse();
            Destroy(gameObject);
        }
    }

    private IEnumerator ApplyContinuousForce(float duration)
    {
        float elapsed = 0f;
        var wait = new WaitForFixedUpdate();

        while (elapsed < duration)
        {
            Collider[] hitColliders = Physics.OverlapSphere(transform.position, effectRadius);
            foreach (var col in hitColliders)
            {
                var otherRb = col.attachedRigidbody;
                if (otherRb != null && otherRb != _rb)
                {
                    Vector3 dir = (transform.position - otherRb.position).normalized;
                    otherRb.AddForce(dir * pullForce, ForceMode.Force);
                }
            }
            elapsed += Time.fixedDeltaTime;
            yield return wait;
        }

        Destroy(gameObject);
    }

    /// <summary>
    /// Applies a one-time impulse using Unity's radial explosion force for realistic falloff.
    /// </summary>
    private void ApplyPushImpulse()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, effectRadius);
        foreach (var col in hitColliders)
        {
            var otherRb = col.attachedRigidbody;
            if (otherRb != null && otherRb != _rb)
            {
                otherRb.AddExplosionForce(pushForce, transform.position, effectRadius, upwardsModifier, ForceMode.Impulse);
            }
        }
    }
}
