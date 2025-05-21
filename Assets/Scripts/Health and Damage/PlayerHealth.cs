using UnityEngine;

[RequireComponent(typeof(Collider), typeof(Rigidbody))]
public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [Tooltip("How many hit points the player starts with.")]
    public int maxHealth = 3;
    private int currentHealth;

    [Header("Water Damage Settings")]
    [Tooltip("Which physics layer your water objects live on.")]
    public LayerMask waterLayer;
    [Tooltip("Which WaterState(s) count as dangerous.")]
    public WaterState dangerousStates = WaterState.Boiled | WaterState.Electrified;

    private void Start()
    {
        currentHealth = maxHealth;
    }

    // NOTE: both this collider and the water collider must be non-trigger
    private void OnCollisionEnter(Collision collision)
    {
        // Quick layer check
        if (((1 << collision.gameObject.layer) & waterLayer.value) == 0)
            return;

        // Grab the WaterBehaviour to inspect its state
        var water = collision.gameObject.GetComponent<WaterBehaviour>();
        if (water != null && (dangerousStates & water.currentState) != 0)
        {
            TakeDamage(1);
        }
    }

    private void TakeDamage(int amount)
    {
        currentHealth = Mathf.Max(currentHealth - amount, 0);
        Debug.Log($"Player damaged! HP: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0)
            Die();
    }

    private void Die()
    {
        Debug.Log("Player has died!");
        // TODO: your death logic (reload level, play animation, etc.)
    }
}
