using UnityEngine;

public class DefaultInteractable : MonoBehaviour, IInteractable
{
    public InteractionCategory Category => InteractionCategory.Default;
    public void HandleInteraction(IInteractable other)
    {
        // no state change, truly a no-op
    }
}
