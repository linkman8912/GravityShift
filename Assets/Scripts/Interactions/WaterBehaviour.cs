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

    private MeshRenderer _rend;

    void Awake()
    {
        _rend = GetComponent<MeshRenderer>();
        ApplyVisual();
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
        // Pick the correct material array based on the current flags
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
    }

    /// <summary>
    /// Call to reset this water back to its initial (None) state.
    /// </summary>
    public void ResetState()
    {
        currentState = WaterState.None;
        ApplyVisual();
        Debug.Log($"🔄 {name} reset to normal.");
    }
}