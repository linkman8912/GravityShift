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
    public bool isPull = true;

    [Tooltip("Duration (in seconds) for which the orb continuously applies its force after collision.")]
    public float effectDuration = 2f;

    private Rigidbody _rb;
    private bool _effectTriggered = false;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        // Ensure the orb is destroyed after its lifetime expires.
        Destroy(gameObject, lifeTime);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!_effectTriggered)
        {
            // Optionally stop movement on collision.
            if (_rb != null)
            {
                _rb.velocity = Vector3.zero;
                _rb.isKinematic = true;
            }
            TriggerGravityEffect();
        }
    }

    /// <summary>
    /// Begins applying continuous force for the specified effectDuration.
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
            // Use OverlapSphere to detect nearby objects.
            Collider[] hitColliders = Physics.OverlapSphere(transform.position, effectRadius);
            foreach (Collider col in hitColliders)
            {
                Rigidbody otherRb = col.GetComponent<Rigidbody>();
                // Skip if no Rigidbody or if it's our own Rigidbody.
                if (otherRb != null && otherRb != _rb)
                {
                    Vector3 direction = isPull ?
                        (transform.position - otherRb.transform.position).normalized :
                        (otherRb.transform.position - transform.position).normalized;
                    float force = isPull ? pullForce : pushForce;
                    // Apply force continuously.
                    otherRb.AddForce(direction * force, ForceMode.Force);
                }
            }
            elapsed += Time.fixedDeltaTime;
            yield return wait;
        }
        Destroy(gameObject);
    }
}
