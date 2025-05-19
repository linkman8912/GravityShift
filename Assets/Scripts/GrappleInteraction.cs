using UnityEngine;

public class GrappleInteraction : MonoBehaviour
{
    [Header("Interaction Settings")]
    public float maxInteractDistance = 50f;
    public LayerMask electricLayer;
    public LayerMask heatLayer;
    public LayerMask waterLayer;
    [Tooltip("Name of layer to assign when water is electrified")]
    public string electrocutedWaterLayerName = "ElectrocutedWater";
    [Tooltip("Name of layer to assign when water is boiled")]
    public string boiledLayerName = "Boiled";

    [Header("References")]
    public Camera playerCamera;

    private RaycastHit firstHit;
    private bool isFirstSelected = false;

    void Start()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;
        if (playerCamera == null)
            Debug.LogError("GrappleInteraction: No Camera assigned or found.");
    }

    void Update()
    {
        // only proceed on right‐click
        if (!Input.GetMouseButtonDown(1))
            return;

        int mask = electricLayer.value | heatLayer.value | waterLayer.value;
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

        if (!isFirstSelected)
        {
            firstHit = hit;
            isFirstSelected = true;
            Debug.Log($"[Interact] First selected: {hit.collider.name} ({LayerMask.LayerToName(hit.collider.gameObject.layer)})");
            return;
        }

        int layerA = firstHit.collider.gameObject.layer;
        int layerB = hit.collider.gameObject.layer;

        bool aIsWater = (waterLayer.value & (1 << layerA)) != 0;
        bool bIsWater = (waterLayer.value & (1 << layerB)) != 0;
        bool aIsElectric = (electricLayer.value & (1 << layerA)) != 0;
        bool bIsElectric = (electricLayer.value & (1 << layerB)) != 0;
        bool aIsHeat = (heatLayer.value & (1 << layerA)) != 0;
        bool bIsHeat = (heatLayer.value & (1 << layerB)) != 0;

        GameObject GetWaterObj() => aIsWater ? firstHit.collider.gameObject : hit.collider.gameObject;

        if ((aIsElectric && bIsWater) || (bIsElectric && aIsWater))
        {
            var waterObj = GetWaterObj();
            int newLayer = LayerMask.NameToLayer(electrocutedWaterLayerName);
            if (newLayer == -1)
                Debug.LogWarning($"Layer '{electrocutedWaterLayerName}' not found!");
            else
            {
                waterObj.layer = newLayer;
                Debug.Log($"⚡ Water electrified on object: {waterObj.name}");
            }
        }
        else if ((aIsHeat && bIsWater) || (bIsHeat && aIsWater))
        {
            var waterObj = GetWaterObj();
            int newLayer = LayerMask.NameToLayer(boiledLayerName);
            if (newLayer == -1)
                Debug.LogWarning($"Layer '{boiledLayerName}' not found!");
            else
            {
                waterObj.layer = newLayer;
                Debug.Log($"🔥 Water boiled on object: {waterObj.name}");
            }
        }
        else
        {
            Debug.Log("[Interact] Those two layers don't form a valid interaction.");
        }

        isFirstSelected = false;
    }
}
