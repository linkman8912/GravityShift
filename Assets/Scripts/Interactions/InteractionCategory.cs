// Assets/Scripts/Interactions/InteractionCategory.cs

public enum InteractionCategory
{
    Water,
    Heat,
    Electric,
    Default   // ← new “no-op” category
}

public interface IInteractable
{
    InteractionCategory Category { get; }
    void HandleInteraction(IInteractable other);
}
