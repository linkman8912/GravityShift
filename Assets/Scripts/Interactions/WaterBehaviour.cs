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

    [Header("Heated Particle Effects")]
    [Tooltip("Every GameObject (ParticleSystem + ParticleRateRiser) to play when the water first becomes Heated.")]
    public GameObject[] heatedEffects;

    [Header("Boiled Particle Effects")]
    [Tooltip("Every GameObject (ParticleSystem + ParticleRateRiser) to play when the water first becomes Boiled.")]
    public GameObject[] boiledEffects;

    [Header("Electrified Particle Effects")]
    [Tooltip("Every GameObject (ParticleSystem + ParticleRateRiser) to play when the water first becomes Electrified.")]
    public GameObject[] electrifiedEffects;


    private MeshRenderer _rend;
    private WaterState prevState = WaterState.None;

    void Awake()
    {
        _rend = GetComponent<MeshRenderer>();

        // Immediately pick the correct material
        ApplyVisual();

        // Disable every effect at start
        SetAllInactive(heatedEffects);
        SetAllInactive(boiledEffects);
        SetAllInactive(electrifiedEffects);

        // Record starting state
        prevState = currentState;
    }

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
        // 1st time: set Heated. 2nd time: if already Heated but not Boiled, set Boiled.
        if (!currentState.HasFlag(WaterState.Heated))
        {
            currentState |= WaterState.Heated;
        }
        else if (!currentState.HasFlag(WaterState.Boiled))
        {
            currentState |= WaterState.Boiled;
        }

        ApplyVisual();
    }

    private void ApplyElectricity()
    {
        currentState |= WaterState.Electrified;
        ApplyVisual();
    }

    private void ApplyVisual()
    {
        // ─── 1) Choose materials ───
        if (currentState.HasFlag(WaterState.Boiled) && currentState.HasFlag(WaterState.Electrified))
        {
            _rend.materials = boiledElectrifiedMats;
        }
        else if (currentState.HasFlag(WaterState.Heated) && currentState.HasFlag(WaterState.Electrified))
        {
            _rend.materials = heatedElectrifiedMats;
        }
        else if (currentState.HasFlag(WaterState.Boiled))
        {
            _rend.materials = boiledMats;
        }
        else if (currentState.HasFlag(WaterState.Heated))
        {
            _rend.materials = heatedMats;
        }
        else if (currentState.HasFlag(WaterState.Electrified))
        {
            _rend.materials = electrifiedMats;
        }
        else
        {
            _rend.materials = normalMats;
        }

        // ─── 2) Detect newly‐set or newly‐cleared flags ───
        bool justHeated =
            currentState.HasFlag(WaterState.Heated) &&
            !prevState.HasFlag(WaterState.Heated);

        bool justBoiled =
            currentState.HasFlag(WaterState.Boiled) &&
            !prevState.HasFlag(WaterState.Boiled);

        bool justElectrified =
            currentState.HasFlag(WaterState.Electrified) &&
            !prevState.HasFlag(WaterState.Electrified);

        bool justUnHeated =
            prevState.HasFlag(WaterState.Heated) &&
            !currentState.HasFlag(WaterState.Heated);

        bool justUnBoiled =
            prevState.HasFlag(WaterState.Boiled) &&
            !currentState.HasFlag(WaterState.Boiled);

        bool justUnElectrified =
            prevState.HasFlag(WaterState.Electrified) &&
            !currentState.HasFlag(WaterState.Electrified);

        // ─── 3) Ramp‐UP any newly set flags ───
        if (justHeated)
            ActivateAndRampAll(heatedEffects);

        if (justBoiled)
            ActivateAndRampAll(boiledEffects);

        if (justElectrified)
            ActivateAndRampAll(electrifiedEffects);

        // ─── 4) Ramp‐DOWN any newly cleared flags ───
        if (justUnHeated)
            RampDownAll(heatedEffects);

        if (justUnBoiled)
            RampDownAll(boiledEffects);

        if (justUnElectrified)
            RampDownAll(electrifiedEffects);

        // ─── 5) Store for next comparison ───
        prevState = currentState;
    }


    /// <summary>
    /// Reset the water completely.  We clear the state and let ApplyVisual()
    /// invoke RampDown... on any effect that was active.  Then the ParticleRateRiser
    /// on each effect will fade it out, and finally disable itself.
    /// </summary>
    public void ResetState()
    {
        bool wasHeated = prevState.HasFlag(WaterState.Heated);
        bool wasBoiled = prevState.HasFlag(WaterState.Boiled);
        bool wasElectrified = prevState.HasFlag(WaterState.Electrified);

        currentState = WaterState.None;
        ApplyVisual();

        // We do NOT immediately deactivate heated/boiled/electrified here,
        // because we want each one’s ParticleRateRiser to ramp down gracefully.

        Debug.Log($"🔄 {name} reset to normal state.  Former flags -> " +
                  $"Heated={wasHeated}, Boiled={wasBoiled}, Electrified={wasElectrified}");
    }


    // ─── UTILITIES ───

    private void SetAllInactive(GameObject[] effects)
    {
        if (effects == null) return;
        foreach (var go in effects)
        {
            if (go != null)
                go.SetActive(false);
        }
    }

    private void ActivateAndRampAll(GameObject[] effects)
    {
        if (effects == null) return;
        foreach (var go in effects)
        {
            if (go == null) continue;

            go.SetActive(true);
            var riser = go.GetComponent<ParticleRateRiser>();
            if (riser != null)
                riser.RestartRamp();
            else
                Debug.LogWarning($"[WaterBehaviour] No ParticleRateRiser on “{go.name}”.");
        }
    }

    private void RampDownAll(GameObject[] effects)
    {
        if (effects == null) return;
        foreach (var go in effects)
        {
            if (go == null) continue;

            var riser = go.GetComponent<ParticleRateRiser>();
            if (riser != null)
                riser.RampDown();
            else
                Debug.LogWarning($"[WaterBehaviour] No ParticleRateRiser on “{go.name}”.");
        }
    }
}
