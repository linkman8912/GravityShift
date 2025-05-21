using UnityEngine;

/// <summary>
/// Helper to test if a LayerMask contains a given layer index.
/// </summary>
public static class LayerMaskExtensions
{
    public static bool HasLayer(this LayerMask mask, int layer)
    {
        return (mask.value & (1 << layer)) != 0;
    }
}