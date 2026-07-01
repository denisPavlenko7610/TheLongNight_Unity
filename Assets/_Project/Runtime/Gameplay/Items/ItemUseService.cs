using TLN.Application.Assets;
using TLN.Application.Localization;
using TLN.Application.Multiplayer;
using TLN.Application.Notifications;
using TLN.Core.Results;
using TLN.Gameplay.Equipment;
using TLN.Gameplay.Inventory;
using TLN.Gameplay.Player;
using TLN.Gameplay.Placement;
using TLN.Gameplay.Survival;
using UnityEngine;

namespace TLN.Gameplay.Items
{
	public sealed class ItemUseService : IItemUseService
	{
		private readonly IInventoryService _inventoryService;
		private readonly ISurvivalService _survivalService;
		private readonly IMultiplayerSessionService _multiplayerSessionService;
		private readonly LocalPlayerService _localPlayerService;
		private readonly INotificationService _notificationService;
		private readonly PlacementService _placementService;
		private readonly IPlayerEquipmentService _equipmentService;
		private readonly IAddressableAssetService _addressableAssetService;

		public ItemUseService(
			IInventoryService inventoryService,
			ISurvivalService survivalService,
			IMultiplayerSessionService multiplayerSessionService,
			LocalPlayerService localPlayerService,
			INotificationService notificationService,
			PlacementService placementService,
			IPlayerEquipmentService equipmentService,
			IAddressableAssetService addressableAssetService
		)
		{
			_inventoryService = inventoryService;
			_survivalService = survivalService;
			_multiplayerSessionService = multiplayerSessionService;
			_localPlayerService = localPlayerService;
			_notificationService = notificationService;
			_placementService = placementService;
			_equipmentService = equipmentService;
			_addressableAssetService = addressableAssetService;
		}

		public ItemUseResult UseItemAt(int index)
		{
			if (_multiplayerSessionService is { IsMultiplayer: true })
			{
				return Fail(Loc.CannotUse);
			}
			
			if (index < 0 || index >= _inventoryService.Items.Count)
			{
				return Fail(Loc.ItemsInvalidSlot);
			}

			ItemStack stack = _inventoryService.Items[index];

			return stack.Definition.UseKind switch
			{
				ItemUseKind.Consumable => UseConsumableAt(index, stack),
				ItemUseKind.Placeable => UsePlaceableAt(index, stack),
				ItemUseKind.Clothing => UseClothing(stack),
				_ => Fail(Loc.CannotUse)
			};
		}

		private ItemUseResult Fail(string key, params object[] arguments)
		{
			string message = string.Format(key, arguments);
			_notificationService.Show(message);
			return ItemUseResult.Failure(message);
		}

		private ItemUseResult UseConsumableAt(int index, ItemStack stack)
		{
			if (stack.Definition is not ConsumableItemDefinition consumable)
			{
				return ItemUseResult.Failure(Loc.CannotConsume);
			}

			ISurvivalService survivalService = GetActiveSurvivalService();
			if (survivalService == null)
			{
				return Fail(Loc.ServiceMissingItemUse);
			}

			bool wasRemoved = _inventoryService.TryRemoveItemAt(index, 1, out string removeFailureReason);

			if (!wasRemoved)
			{
				return ItemUseResult.Failure(removeFailureReason);
			}

			survivalService.ApplyConsumable(consumable);

			string message = Loc.Used(consumable.DisplayName);
			_notificationService.Show(message);

			return ItemUseResult.Success(message);
		}

		private ItemUseResult UsePlaceableAt(int index, ItemStack stack)
		{
			if (stack.Definition is not PlaceableItemDefinition placeable)
			{
				return Fail(Loc.CannotPlace);
			}

			if (_addressableAssetService == null)
			{
				return Fail(Loc.AddressableServiceMissing);
			}

			if (placeable.PlacedPrefabReference == null ||
				!placeable.PlacedPrefabReference.RuntimeKeyIsValid())
			{
				return Fail(Loc.PrefabReferenceMissing, placeable.DisplayName);
			}

			_addressableAssetService.LoadPrefab(placeable.PlacedPrefabReference, prefab =>
				{
					OnPlaceablePrefabLoaded(placeable, prefab);
				}
			);

			return ItemUseResult.Success(Loc.Placing(placeable.DisplayName));
		}

		private void OnPlaceablePrefabLoaded(PlaceableItemDefinition placeable, GameObject prefab)
		{
			if (placeable == null)
			{
				return;
			}

			if (prefab == null)
			{
				Fail(Loc.PrefabLoadFailed, placeable.DisplayName);
				return;
			}

			bool wasPlaced = _placementService.TryPlace(prefab, placeable.PlaceDistance, out GameObject placedObject);
			if (!wasPlaced)
			{
				Fail(Loc.CannotPlaceHere);
				return;
			}

			bool wasRemoved = _inventoryService.TryRemoveItem(placeable, 1, out string removeFailureReason);
			if (!wasRemoved)
			{
				Object.Destroy(placedObject);
				_notificationService.Show(removeFailureReason);
				return;
			}

			string message = Loc.Placed(placeable.DisplayName);
			_notificationService.Show(message);
		}

		private ItemUseResult UseClothing(ItemStack stack)
		{
			if (stack.Definition is not ClothingItemDefinition clothing)
			{
				return Fail(Loc.CannotEquip);
			}

			if (_equipmentService == null)
			{
				return Fail(Loc.EquipmentServiceMissing);
			}

			OperationResult result = _equipmentService.ToggleEquip(clothing);
			_notificationService.Show(result.Message);

			return !result.IsSuccess ? ItemUseResult.Failure(result.Message) : ItemUseResult.Success(result.Message);
		}

		private ISurvivalService GetActiveSurvivalService()
		{
			if (_multiplayerSessionService is { IsMultiplayer: true })
			{
				return _localPlayerService?.SurvivalService;
			}

			return _survivalService;
		}
	}
}
