// WaterBehaviour.cs
using UnityEngine;

[System.Flags]
public enum WaterState
{
    None = 0,
    Heated = 1 << 0,
    Boiled = 1 << 1,
    Electrified = 1 << 2
}

[RequireComponent(typeof(Collider), typeof(MeshRenderer))]
public class WaterBehaviour : MonoBehaviour, IInteractable
{
    public InteractionCategory Category => InteractionCategory.Water;

    [Header("Visuals")]
    [Tooltip("Materials to use when in the normal state.")]
    public Material[] normalMats;
    [Tooltip("Materials to use when only Heated.")]
    public Material[] heatedMats;
    [Tooltip("Materials to use when only Boiled.")]
    public Material[] boiledMats;
    [Tooltip("Materials to use when only Electrified.")]
    public Material[] electrifiedMats;
    [Tooltip("Materials to use when Heated + Electrified.")]
    public Material[] heatedElectrifiedMats;
    [Tooltip("Materials to use when Boiled + Electrified.")]
    public Material[] boiledElectrifiedMats;

    [Header("State")]
    public WaterState currentState = WaterState.None;

    // ───────── NOW SUPPORT MULTIPLE BOILED EFFECTS ─────────
    [Header("Boiled Particle Effects")]
    [Tooltip("List every particle‐system GameObject you want to play when water becomes Boiled.")]
    public GameObject[] boiledEffects;

    private MeshRenderer _rend;
    private WaterState prevState = WaterState.None;

    void Awake()
    {
        _rend = GetComponent<MeshRenderer>();
        ApplyVisual();

        // Ensure every boiledEffect starts disabled
        foreach (var go in boiledEffects)
            if (go != null)
                go.SetActive(false);

        // Record the starting state so we don't accidentally trigger on first ApplyVisual
        prevState = currentState;
    }

    /// <summary>
    /// Called by GrappleInteraction when this water object
    /// interacts with another IInteractable.
    /// </summary>
    public void HandleInteraction(IInteractable other)
    {
        switch (other.Category)
        {
            case InteractionCategory.Heat:
                ApplyHeat();
                Debug.Log($"🔥 {name} heat state now: {currentState}");
                break;

            case InteractionCategory.Electric:
                ApplyElectricity();
                Debug.Log($"⚡ {name} electric state now: {currentState}");
                break;

            default:
                Debug.Log($"[Interact] {name} + {other.Category} → no effect.");
                break;
        }
    }

    private void ApplyHeat()
    {
        // If not yet Heated, set Heated; else if already Heated, set Boiled
        if (!currentState.HasFlag(WaterState.Heated))
            currentState |= WaterState.Heated;
        else if (!currentState.HasFlag(WaterState.Boiled))
            currentState |= WaterState.Boiled;

        ApplyVisual();
    }

    private void ApplyElectricity()
    {
        currentState |= WaterState.Electrified;
        ApplyVisual();
    }

    private void ApplyVisual()
    {
        // ───────── PICK MATERIALS ─────────
        if (currentState.HasFlag(WaterState.Boiled) && currentState.HasFlag(WaterState.Electrified))
            _rend.materials = boiledElectrifiedMats;
        else if (currentState.HasFlag(WaterState.Heated) && currentState.HasFlag(WaterState.Electrified))
            _rend.materials = heatedElectrifiedMats;
        else if (currentState.HasFlag(WaterState.Boiled))
            _rend.materials = boiledMats;
        else if (currentState.HasFlag(WaterState.Heated))
            _rend.materials = heatedMats;
        else if (currentState.HasFlag(WaterState.Electrified))
            _rend.materials = electrifiedMats;
        else
            _rend.materials = normalMats;

        // ───────── SEE IF “Boiled” JUST GOT SET THIS FRAME ─────────
        bool justBoiled =
            currentState.HasFlag(WaterState.Boiled) &&
            !prevState.HasFlag(WaterState.Boiled);

        if (justBoiled)
        {
            // Activate + restart every boiled particle effect
            foreach (var go in boiledEffects)
            {
                if (go == null)
                    continue;

                go.SetActive(true);

                var riser = go.GetComponent<ParticleRateRiser>();
                if (riser != null)
                {
                    riser.RestartRamp();
                }
                else
                {
                    Debug.LogWarning($"[WaterBehaviour] No ParticleRateRiser on “{go.name}”.");
                }
            }
        }

        // Update prevState for the next time we call ApplyVisual()
        prevState = currentState;
    }

    /// <summary>
    /// Call to reset this water back to its initial (None) state.
    /// Disables all boiled particle effects immediately.
    /// </summary>
    public void ResetState()
    {
        currentState = WaterState.None;
        ApplyVisual();

        foreach (var go in boiledEffects)
            if (go != null)
                go.SetActive(false);

        Debug.Log($"🔄 {name} reset to normal.");
    }
}
