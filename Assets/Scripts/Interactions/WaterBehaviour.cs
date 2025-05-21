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

[RequireComponent(typeof(Collider), typeof(Renderer))]
public class WaterBehaviour : MonoBehaviour, IInteractable
{
    public InteractionCategory Category => InteractionCategory.Water;

    [Header("Visuals")]
    public Material normalMat;
    public Material heatedMat;
    public Material boiledMat;
    public Material electrifiedMat;
    public Material heatedElectrifiedMat;    // when both Heated and Electrified
    public Material boiledElectrifiedMat;    // when both Boiled and Electrified

    [Header("State")]
    public WaterState currentState = WaterState.None;

    private Renderer _rend;

    void Awake()
    {
        _rend = GetComponent<Renderer>();
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
        bool he = currentState.HasFlag(WaterState.Heated);
        bool bo = currentState.HasFlag(WaterState.Boiled);
        bool el = currentState.HasFlag(WaterState.Electrified);

        if (bo && el) _rend.material = boiledElectrifiedMat;
        else if (he && el) _rend.material = heatedElectrifiedMat;
        else if (bo) _rend.material = boiledMat;
        else if (he) _rend.material = heatedMat;
        else if (el) _rend.material = electrifiedMat;
        else _rend.material = normalMat;
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
