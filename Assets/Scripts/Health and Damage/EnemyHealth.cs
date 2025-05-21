using UnityEngine;

[RequireComponent(typeof(Collider), typeof(Rigidbody))]
public class EnemyHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [Tooltip("How many hit points the enemy starts with.")]
    public int maxHealth = 3;
    private int currentHealth;

    [Header("Water Damage Settings")]
    [Tooltip("Which physics layer your water objects live on.")]
    public LayerMask waterLayer;
    [Tooltip("Which WaterState(s) count as dangerous to this enemy.")]
    public WaterState dangerousStates = WaterState.Boiled | WaterState.Electrified;

    void Start()
    {
        currentHealth = maxHealth;
    }

    // NOTE: both this collider and the water collider must be non-trigger
    void OnCollisionEnter(Collision collision)
    {
        // Only consider collisions with objects on the water layer
        if (((1 << collision.gameObject.layer) & waterLayer.value) == 0)
            return;

        // If it’s a WaterBehaviour, and its state is dangerous, take damage
        var water = collision.gameObject.GetComponent<WaterBehaviour>();
        if (water != null && (dangerousStates & water.currentState) != 0)
        {
            TakeDamage(1);
        }
    }

    void TakeDamage(int amount)
    {
        currentHealth = Mathf.Max(currentHealth - amount, 0);
        Debug.Log($"{name} took damage! HP: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0)
            Die();
    }

    void Die()
    {
        Debug.Log($"{name} has been destroyed!");
        // You can add an explosion VFX or drop loot here before destroying:
        Destroy(gameObject);
    }
}
