// Assets/Scripts/Interactions/InteractionCategory.cs

public enum InteractionCategory
{
    Water,
    Heat,
    Electric,
    Default,
    Metal,
    Wooden,
    Explodable
}

public interface IInteractable
{
    InteractionCategory Category { get; }
    void HandleInteraction(IInteractable other);
}
