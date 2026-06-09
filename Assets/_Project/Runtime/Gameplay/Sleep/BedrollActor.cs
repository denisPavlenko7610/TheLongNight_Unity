using TLN.Core.Logging;
using TLN.Gameplay.Interaction;
using TLN.Gameplay.Items;
using UnityEngine;
using VContainer;

namespace TLN.Gameplay.Sleep
{
	public sealed class BedrollActor : MonoBehaviour, IInteractable
	{
		[Header("Interaction")]
		[SerializeField] private string _interactionText = "Sleep";

		[Header("Pickup")]
		[SerializeField] private bool _canPickUp;
		[SerializeField] private ItemDefinition _packedItemDefinition;
		[SerializeField] private int _packedAmount = 1;

		private ISleepWindow _sleepWindow;

		public string InteractionText => _interactionText;

		public bool CanPickUp => _canPickUp;
		public ItemDefinition PackedItemDefinition => _packedItemDefinition;
		public int PackedAmount => _packedAmount;

		[Inject]
		public void Construct(ISleepWindow sleepWindow)
		{
			_sleepWindow = sleepWindow;
		}

		public bool CanInteract(InteractionContext context)
		{
			return true;
		}

		public void Interact(InteractionContext context)
		{
			if (_sleepWindow == null)
			{
				TLNLogger.Warning("Cannot open sleep window because BedrollActor was not constructed.", this);
				return;
			}

			_sleepWindow.Show(this);
		}
	}
}
