using TLN.Application.Localization;
using TLN.Core.Logging;
using TLN.Gameplay.Inventory;
using TLN.Gameplay.Placement;
using UnityEngine;

namespace TLN.Gameplay.Items
{
	/// <summary>
	/// Removes items from the inventory and spawns their world pickup in front of the player.
	/// </summary>
	public sealed class ItemDropService : IItemDropService
	{
		private const int SingleItemAmount = 1;

		private readonly IInventoryService _inventoryService;
		private readonly PlacementService _placementService;
		private readonly WorldItemPrefabCatalog _prefabCatalog;

		public ItemDropService(
			IInventoryService inventoryService,
			PlacementService placementService,
			WorldItemPrefabCatalog prefabCatalog
		)
		{
			_inventoryService = inventoryService;
			_placementService = placementService;
			_prefabCatalog = prefabCatalog;
		}

		public bool DropItemAt(int inventoryIndex, int amount, out string failureReason)
		{
			failureReason = null;

			if (_inventoryService == null)
			{
				failureReason = Loc.BedrollInventoryMissing;
				return false;
			}

			if (inventoryIndex < 0 || inventoryIndex >= _inventoryService.Items.Count)
			{
				failureReason = Loc.InventoryInvalidSlot;
				return false;
			}

			if (amount <= 0)
			{
				failureReason = Loc.InvalidAmount;
				return false;
			}

			ItemStack stack = _inventoryService.Items[inventoryIndex];

			if (amount > stack.Amount)
			{
				failureReason = Loc.NotEnough;
				return false;
			}

			ItemDefinition definition = stack.Definition;

			if (_prefabCatalog == null ||
			    !_prefabCatalog.TryGetPrefab(definition.Id, out GameObject prefab) ||
			    prefab == null)
			{
				failureReason = Loc.DropPrefabMissing(definition.DisplayName);
				return false;
			}

			if (!_placementService.TryPlace(prefab, DropDistance, out GameObject instance))
			{
				failureReason = Loc.CannotPlaceHere;
				return false;
			}

			ConfigureWorldItem(instance, definition, amount);

			if (!_inventoryService.TryRemoveItemAt(inventoryIndex, amount, out failureReason))
			{
				Object.Destroy(instance);
				return false;
			}

			return true;
		}

		private float DropDistance => 1.2f;

		private static void ConfigureWorldItem(GameObject instance, ItemDefinition definition, int amount)
		{
			if (instance == null)
			{
				return;
			}

			WorldItemActor actor = instance.GetComponent<WorldItemActor>();

			if (actor == null)
			{
				TLNLogger.LogWarning(
					$"Dropped prefab has no WorldItemActor: {instance.name}",
					instance
				);

				return;
			}

			actor.SetDropContent(definition, amount);
		}
	}
}
