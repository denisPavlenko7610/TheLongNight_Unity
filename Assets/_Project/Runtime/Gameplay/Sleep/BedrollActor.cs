using TLN.Gameplay.Interaction;
using UnityEngine;

namespace TLN.Gameplay.Sleep
{
	public sealed class BedrollActor : MonoBehaviour, IInteractable
	{
		[SerializeField] private string _interactionText = "Sleep";

		private ISleepWindow _sleepWindow;

		public string InteractionText => _interactionText;

		public void Construct(ISleepWindow sleepWindow)
		{
			_sleepWindow = sleepWindow;
		}

		public bool CanInteract(InteractionContext context)
		{
			return _sleepWindow != null;
		}

		public void Interact(InteractionContext context)
		{
			_sleepWindow.Show();
		}
	}
}
