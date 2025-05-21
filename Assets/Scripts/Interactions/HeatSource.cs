using UnityEngine;
public class HeatSource : MonoBehaviour, IInteractable
{
    public InteractionCategory Category => InteractionCategory.Heat;
    public void HandleInteraction(IInteractable other) { /* no self‐state change */ }
}
