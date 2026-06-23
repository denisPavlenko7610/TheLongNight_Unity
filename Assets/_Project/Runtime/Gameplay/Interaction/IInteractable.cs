namespace TLN.Gameplay.Interaction
{
	public interface IInteractable
	{
		string InteractionText { get; }

		bool CanInteract(InteractionContext context);

		void Interact(InteractionContext context);
	}
}