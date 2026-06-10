using TLN.Application.Notifications;
using TLN.Core.Validation;
using TLN.Gameplay.Interaction;
using TLN.Gameplay.Inventory;
using UnityEngine;
using VContainer;

namespace TLN.Gameplay.Items
{
	public sealed class WorldItemActor : MonoBehaviour, IInteractable
	{
		[Header("Item")]
		[SerializeField, Required] private ItemDefinition _definition;
		[SerializeField] private int _amount = 1;

		private INotificationService _notificationService;
		private IInventoryService _inventoryService;

		public string InteractionText
		{
			get
			{
				return _definition == null ? "Pick up item" : $"Pick up {_definition.DisplayName}";
			}
		}

		[Inject]
		public void Construct(IInventoryService inventoryService, INotificationService notificationService)
		{
			_inventoryService = inventoryService;
			_notificationService = notificationService;
		}

		public bool CanInteract(InteractionContext context)
		{
			if (_definition == null)
			{
				return false;
			}

			if (_amount <= 0)
			{
				return false;
			}

			if (_inventoryService == null)
			{
				return false;
			}

			return _inventoryService.CanAddItem(_definition, _amount, out _);
		}

		public void Interact(InteractionContext context)
		{
			if (!CanInteract(context))
			{
				return;
			}

			InventoryAddResult result = _inventoryService.AddItem(_definition, _amount);

			if (!result.IsSuccess)
			{
				_notificationService?.Show(result.FailureReason);
				Debug.Log($"Cannot pick up {_definition.DisplayName}. Reason: {result.FailureReason}");
				return;
			}

			_notificationService?.Show($"Picked up {_definition.DisplayName}");
			Destroy(gameObject);
		}
	}
}
