using TLN.Application.Notifications;
using TLN.Gameplay.Inventory;
using TLN.Gameplay.Survival;

namespace TLN.Gameplay.Items
{
	public sealed class ItemUseService
	{
		private readonly IInventoryService _inventoryService;
		private readonly ISurvivalService _survivalService;
		private readonly INotificationService _notificationService;

		public ItemUseService(IInventoryService inventoryService, ISurvivalService survivalService, INotificationService notificationService)
		{
			_inventoryService = inventoryService;
			_survivalService = survivalService;
			_notificationService = notificationService;
		}

		public ItemUseResult UseItemAt(int index)
		{
			if (index < 0 || index >= _inventoryService.Items.Count)
			{
				return ItemUseResult.Failure("Invalid inventory slot.");
			}

			ItemStack stack = _inventoryService.Items[index];

			if (stack.Definition is not ConsumableItemDefinition consumable)
			{
				return ItemUseResult.Failure("This item cannot be used.");
			}

			bool wasRemoved = _inventoryService.TryRemoveItemAt(index, 1, out string removeFailureReason);

			if (!wasRemoved)
			{
				return ItemUseResult.Failure(removeFailureReason);
			}

			_survivalService.ApplyConsumable(consumable);

			string message = $"Used {consumable.DisplayName}";
			_notificationService.Show(message);

			return ItemUseResult.Success(message);
		}
	}
}
