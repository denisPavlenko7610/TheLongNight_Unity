using TLN.Application.Notifications;
using TLN.Core.Results;
using TLN.Gameplay.Equipment;
using TLN.Gameplay.Inventory;
using TLN.Gameplay.Placement;
using TLN.Gameplay.Survival;
using UnityEngine;

namespace TLN.Gameplay.Items
{
	public sealed class ItemUseService : IItemUseService
	{
		private readonly IInventoryService _inventoryService;
		private readonly ISurvivalService _survivalService;
		private readonly INotificationService _notificationService;
		private readonly PlacementService _placementService;
		private readonly IPlayerEquipmentService _equipmentService;

		public ItemUseService(IInventoryService inventoryService, ISurvivalService survivalService, INotificationService notificationService
		, PlacementService placementService, IPlayerEquipmentService equipmentService)
		{
			_inventoryService = inventoryService;
			_survivalService = survivalService;
			_notificationService = notificationService;
			_placementService = placementService;
			_equipmentService = equipmentService;
		}

		public ItemUseResult UseItemAt(int index)
		{
			if (index < 0 || index >= _inventoryService.Items.Count)
			{
				return Fail("Invalid inventory slot.");
			}

			ItemStack stack = _inventoryService.Items[index];

			return stack.Definition.UseKind switch
			{
				ItemUseKind.Consumable => UseConsumableAt(index, stack),
				ItemUseKind.Placeable => UsePlaceableAt(index, stack),
				ItemUseKind.Clothing => UseClothing(stack),
				_ => Fail("This item cannot be used.")
			};
		}

		private ItemUseResult Fail(string message)
		{
			_notificationService.Show(message);
			return ItemUseResult.Failure(message);
		}

		private ItemUseResult UseConsumableAt(int index, ItemStack stack)
		{
			if (stack.Definition is not ConsumableItemDefinition consumable)
			{
				return ItemUseResult.Failure("This item cannot be consumed.");
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

		private ItemUseResult UsePlaceableAt(int index, ItemStack stack)
		{
			if (stack.Definition is not PlaceableItemDefinition placeable)
			{
				return ItemUseResult.Failure("This item cannot be placed.");
			}

			bool wasPlaced = _placementService.TryPlace(placeable.PlacedPrefab, placeable.PlaceDistance, out GameObject placedObject);

			string message = "";
			if (!wasPlaced)
			{
				return Fail("Cannot place item here.");
			}

			bool wasRemoved = _inventoryService.TryRemoveItemAt(index, 1, out string removeFailureReason);

			if (!wasRemoved)
			{
				Object.Destroy(placedObject);
				return ItemUseResult.Failure(removeFailureReason);
			}

			message = $"Placed {placeable.DisplayName}";
			_notificationService.Show(message);

			return ItemUseResult.Success(message);
		}

		private ItemUseResult UseClothing(ItemStack stack)
		{
			if (stack.Definition is not ClothingItemDefinition clothing)
			{
				return Fail("This item cannot be equipped.");
			}

			if (_equipmentService == null)
			{
				return Fail("Equipment service is missing.");
			}

			OperationResult result = _equipmentService.ToggleEquip(clothing);
			_notificationService.Show(result.Message);

			return !result.IsSuccess ? ItemUseResult.Failure(result.Message) : ItemUseResult.Success(result.Message);
		}
	}
}
