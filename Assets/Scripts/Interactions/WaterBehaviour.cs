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

    // ───────── PARTICLE EFFECT REFERENCES ─────────

    [Header("Heated Particle Effects")]
    [Tooltip("Drag in every GameObject (with ParticleSystem + ParticleRateRiser) you want to play when water first becomes Heated.")]
    public GameObject[] heatedEffects;

    [Header("Boiled Particle Effects")]
    [Tooltip("Drag in every GameObject (with ParticleSystem + ParticleRateRiser) you want to play when water first becomes Boiled.")]
    public GameObject[] boiledEffects;

    [Header("Electrified Particle Effects")]
    [Tooltip("Drag in every GameObject (with ParticleSystem + ParticleRateRiser) you want to play when water first becomes Electrified.")]
    public GameObject[] electrifiedEffects;


    private MeshRenderer _rend;
    private WaterState prevState = WaterState.None;

    void Awake()
    {
        _rend = GetComponent<MeshRenderer>();
        ApplyVisual();

        // Ensure every effect is off at start
        SetAllInactive(heatedEffects);
        SetAllInactive(boiledEffects);
        SetAllInactive(electrifiedEffects);

        // Record the starting state so we don't accidentally trigger on first ApplyVisual
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
        // If not yet Heated, set Heated; else if already Heated but not Boiled, set Boiled
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
        // ───────── PICK MATERIALS BASED ON FLAGS ─────────
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

        // ───────── DETECT “JUST SET” FLAGS ─────────

        bool justHeated =
            currentState.HasFlag(WaterState.Heated) &&
            !prevState.HasFlag(WaterState.Heated);

        bool justBoiled =
            currentState.HasFlag(WaterState.Boiled) &&
            !prevState.HasFlag(WaterState.Boiled);

        bool justElectrified =
            currentState.HasFlag(WaterState.Electrified) &&
            !prevState.HasFlag(WaterState.Electrified);

        // ───────── TRIGGER HEATED EFFECTS ─────────
        if (justHeated)
        {
            ActivateAndRampAll(heatedEffects);
        }

        // ───────── TRIGGER BOILED EFFECTS ─────────
        if (justBoiled)
        {
            ActivateAndRampAll(boiledEffects);
        }

        // ───────── TRIGGER ELECTRIFIED EFFECTS ─────────
        if (justElectrified)
        {
            ActivateAndRampAll(electrifiedEffects);
        }

        // ───────── UPDATE PREVIOUS STATE ─────────
        prevState = currentState;
    }


    /// <summary>
    /// Resets water to None, disables all particle effects immediately.
    /// </summary>
    public void ResetState()
    {
        currentState = WaterState.None;
        ApplyVisual();

        SetAllInactive(heatedEffects);
        SetAllInactive(boiledEffects);
        SetAllInactive(electrifiedEffects);

        Debug.Log($"🔄 {name} reset to normal.");
    }


    // ──── HELPERS ────

    /// <summary>
    /// Disable every GameObject in the array (if not null).
    /// </summary>
    private void SetAllInactive(GameObject[] effects)
    {
        if (effects == null) return;
        foreach (var go in effects)
        {
            if (go != null)
                go.SetActive(false);
        }
    }

    /// <summary>
    /// For each GameObject in the array: SetActive(true) and
    /// call its ParticleRateRiser.RestartRamp() if present.
    /// </summary>
    private void ActivateAndRampAll(GameObject[] effects)
    {
        if (effects == null) return;
        foreach (var go in effects)
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
}
