using UnityEngine;

public class ProjectileBehavior : MonoBehaviour
{
    public float lifeTime = 50f;
    public GameObject impactEffect;
    public LayerMask destroyLayers;
    public LayerMask bounceLayers;
    public float bounceForceMultiplier = 1f;

    private Rigidbody rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        Destroy(gameObject, lifeTime);
    }

    private void OnCollisionEnter(Collision collision)
    {
        int collisionLayer = collision.gameObject.layer;

        if (((1 << collisionLayer) & destroyLayers) != 0)
        {
            if (impactEffect != null)
            {
                Instantiate(impactEffect, transform.position, Quaternion.identity);
            }
            Destroy(gameObject);
        }
        else if (((1 << collisionLayer) & bounceLayers) != 0)
        {
            Vector3 reflectDir = Vector3.Reflect(rb.velocity, collision.contacts[0].normal);
            rb.velocity = reflectDir * bounceForceMultiplier;
        }
    }
}
