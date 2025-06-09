// GrappleInteraction.cs
using UnityEngine;
using System.Collections.Generic;

public class GrappleInteraction : MonoBehaviour
{
    [Header("Interaction Settings")]
    [Tooltip("How far you can click to select an object.")]
    public float maxInteractDistance = 50f;

    [Tooltip("Which layers count as electric sources.")]
    public LayerMask electricLayer;

    [Tooltip("Which layers count as heat sources.")]
    public LayerMask heatLayer;

    [Tooltip("Which layers count as water objects.")]
    public LayerMask waterLayer;

    [Tooltip("Which layers count as 'default' no-ops.")]
    public LayerMask defaultLayer;

    [Header("References")]
    [Tooltip("The Camera used for raycasting. If left empty, will try Camera.main.")]
    public Camera playerCamera;

    [Header("Orb Reference")]
    [Tooltip("Drag your GravityOrbShooter here, or it'll auto-find at Start")]
    public GravityOrbShooter orb;

    // internal two-click state
    private RaycastHit firstHit;
    private bool isFirstSelected = false;

    // track every WaterBehaviour we modified
    private List<WaterBehaviour> _modifiedWaters = new List<WaterBehaviour>();

    // ——— Buffer logic fields ———
    private bool prevOrbHeld = false;
    private float orbReleaseTime = -Mathf.Infinity;
    private const float grappleBuffer = 0.1f;  // seconds after release

    void Start()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;
        if (playerCamera == null)
            Debug.LogError("GrappleInteraction: No Camera assigned or found.");

        // auto-find orb shooter if not assigned
        if (orb == null)
            orb = FindObjectOfType<GravityOrbShooter>();
        Debug.Log($"[Interact] Orb reference {(orb != null ? "FOUND" : "MISSING")}");

        // sanity check: make sure you didn't accidentally include Default in your waterLayer
        Debug.Assert((waterLayer.value & (1 << 0)) == 0,
            "Water layer mask includes Default layer — uncheck it in the Inspector!");
    }

    void Update()
    {
        // 0) Track orb hold→release transition
        if (orb != null)
        {
            bool currentlyHeld = orb.IsOrbHeld;
            if (prevOrbHeld && !currentlyHeld)
            {
                orbReleaseTime = Time.time;
                Debug.Log($"[Interact] Orb released — buffer until {orbReleaseTime + grappleBuffer:F2}");
            }
            prevOrbHeld = currentlyHeld;
        }

        // 1) RESET ON “R”
        if (Input.GetKeyDown(KeyCode.R))
        {
            foreach (var wb in _modifiedWaters)
                wb.ResetState();
            _modifiedWaters.Clear();

            foreach (var sj in FindObjectsOfType<SpringJoint>())
                Destroy(sj);
            foreach (var lr in FindObjectsOfType<LineRenderer>())
                if (lr.gameObject.name.StartsWith("GrappleLine"))
                    Destroy(lr.gameObject);

            isFirstSelected = false;
            Debug.Log("[Interact] Cleared all connections and reset water.");
            return;
        }

        // 2) INTERACT ON RIGHT-CLICK WITH ORB GUARDS
        if (Input.GetMouseButtonDown(1))
        {
            // log entry state
            float sinceRelease = Time.time - orbReleaseTime;
            Debug.Log($"[Interact] Right-click ▶ orbHeld={(orb != null && orb.IsOrbHeld)} | sinceRelease={sinceRelease:F2}s");

            // a) block while orb is held
            if (orb != null && orb.IsOrbHeld)
            {
                Debug.Log("[Interact] ⛔ Blocked: Orb is held — skipping interaction");
                return;
            }
            // b) block during buffer
            if (sinceRelease < grappleBuffer)
            {
                float remaining = grappleBuffer - sinceRelease;
                Debug.Log($"[Interact] ⛔ Blocked: Buffer active, {remaining:F2}s remaining");
                return;
            }
            // else: fall through to actual interaction
        }
        else
        {
            return;
        }

        // 3) build our raycast mask (now including defaultLayer)
        int mask = electricLayer.value
                 | heatLayer.value
                 | waterLayer.value
                 | defaultLayer.value;

        if (!Physics.Raycast(
                playerCamera.transform.position,
                playerCamera.transform.forward,
                out RaycastHit hit,
                maxInteractDistance,
                mask))
        {
            Debug.Log("[Interact] Nothing hit.");
            isFirstSelected = false;
            return;
        }

        var go = hit.collider.gameObject;
        var ia = go.GetComponent<IInteractable>();
        if (ia == null)
        {
            Debug.LogWarning($"[Interact] {go.name} isn’t IInteractable.");
            isFirstSelected = false;
            return;
        }

        // first click?
        if (!isFirstSelected)
        {
            firstHit = hit;
            isFirstSelected = true;
            Debug.Log($"[Interact] First selected → {go.name} ({ia.Category})");
            return;
        }

        // second click
        var firstGO = firstHit.collider.gameObject;
        var ib = firstGO.GetComponent<IInteractable>();
        if (ib == null)
        {
            Debug.LogWarning($"[Interact] First object {firstGO.name} isn’t IInteractable.");
            isFirstSelected = false;
            return;
        }

        Debug.Log($"[Interact] Pair → {firstGO.name} ({ib.Category})  ↔  {go.name} ({ia.Category})");

        // short-circuit if either side is Default
        if (ib.Category == InteractionCategory.Default || ia.Category == InteractionCategory.Default)
        {
            Debug.Log("[Interact] Includes Default → no effect.");
            isFirstSelected = false;
            return;
        }

        // now your existing water/heat/electric logic
        bool aIsWater = ib.Category == InteractionCategory.Water;
        bool bIsWater = ia.Category == InteractionCategory.Water;
        bool aIsHeat = ib.Category == InteractionCategory.Heat;
        bool bIsHeat = ia.Category == InteractionCategory.Heat;
        bool aIsElectric = ib.Category == InteractionCategory.Electric;
        bool bIsElectric = ia.Category == InteractionCategory.Electric;

        if ((aIsWater && bIsHeat) || (bIsWater && aIsHeat))
        {
            var waterI = aIsWater ? ib : ia;
            var heatI = aIsHeat ? ib : ia;
            waterI.HandleInteraction(heatI);
            if (waterI is WaterBehaviour wb && !_modifiedWaters.Contains(wb))
                _modifiedWaters.Add(wb);
        }
        else if ((aIsWater && bIsElectric) || (bIsWater && aIsElectric))
        {
            var waterI = aIsWater ? ib : ia;
            var electricI = aIsElectric ? ib : ia;
            waterI.HandleInteraction(electricI);
            if (waterI is WaterBehaviour wb && !_modifiedWaters.Contains(wb))
                _modifiedWaters.Add(wb);
        }
        else
        {
            Debug.Log("[Interact] Those categories don’t form a valid interaction.");
        }

        isFirstSelected = false;
    }
}
