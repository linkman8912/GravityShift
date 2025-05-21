using UnityEngine;
public class ElectricSource : MonoBehaviour, IInteractable
{
    public InteractionCategory Category => InteractionCategory.Electric;
    public void HandleInteraction(IInteractable other) { /* no self‐state change */ }
}
