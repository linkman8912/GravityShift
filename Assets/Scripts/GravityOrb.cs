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

    [Tooltip("If true, the orb pulls objects in; if false, it pushes objects away.")]
    public bool isPull = false;  // Default is push mode

    [Tooltip("Duration (in seconds) for which the orb continuously applies its force after collision.")]
    public float effectDuration = 2f;

    [Tooltip("Indicates whether the orb is still held (i.e., not fired yet).")]
    public bool isHeld = false;

    private Rigidbody _rb;
    private bool _effectTriggered = false;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        // Auto-destroy after lifeTime seconds.
        Destroy(gameObject, lifeTime);
    }

    private void OnCollisionEnter(Collision collision)
    {
        // If the orb is held and it collides, we activate it without detaching from the holder.
        if (isHeld)
        {
            // Force push mode.
            isPull = false;
            // Only start the effect if not already triggered.
            if (!_effectTriggered)
            {
                TriggerGravityEffect();
            }
            // Do not detach: the orb remains with the holder.
            return;
        }

        // For orbs that have been fired, detach them on collision.
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
    /// Begins applying continuous force for effectDuration.
    /// </summary>
    public void TriggerGravityEffect()
    {
        _effectTriggered = true;
        StartCoroutine(ApplyContinuousForce());
    }

    private IEnumerator ApplyContinuousForce()
    {
        float elapsed = 0f;
        WaitForFixedUpdate wait = new WaitForFixedUpdate();
        while (elapsed < effectDuration)
        {
            Collider[] hitColliders = Physics.OverlapSphere(transform.position, effectRadius);
            foreach (Collider col in hitColliders)
            {
                Rigidbody otherRb = col.GetComponent<Rigidbody>();
                if (otherRb != null && otherRb != _rb)
                {
                    Vector3 direction = isPull ?
                        (transform.position - otherRb.transform.position).normalized :
                        (otherRb.transform.position - transform.position).normalized;
                    float force = isPull ? pullForce : pushForce;
                    otherRb.AddForce(direction * force, ForceMode.Force);
                }
            }
            elapsed += Time.fixedDeltaTime;
            yield return wait;
        }
        Destroy(gameObject);
    }
}