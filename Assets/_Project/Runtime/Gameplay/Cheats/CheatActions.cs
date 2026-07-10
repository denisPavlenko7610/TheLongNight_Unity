using System;
using System.Collections.Generic;
using TLN.Gameplay.Campfire;
using TLN.Gameplay.Inventory;
using TLN.Gameplay.Items;
using TLN.Gameplay.Survival;
using TLN.Gameplay.Time;

namespace TLN.Gameplay.Cheats
{
	public static class CheatActions
	{
		public static bool CanRun(CheatSettings settings)
		{
			return settings != null && settings.CanUseCheats;
		}

		public static bool StabilizeSurvival(ISurvivalService survivalService)
		{
			if (survivalService == null)
			{
				return false;
			}

			survivalService.SetValues(0f, 0f, 0f, 0f, 100f);
			return true;
		}

		public static bool FreezePlayer(ISurvivalService survivalService)
		{
			if (survivalService == null)
			{
				return false;
			}

			survivalService.SetValues(
				survivalService.Hunger.Value,
				survivalService.Thirst.Value,
				survivalService.Fatigue.Value,
				100f,
				survivalService.Condition.Value
			);

			return true;
		}

		public static bool DamageCondition(ISurvivalService survivalService, float amount)
		{
			if (survivalService == null)
			{
				return false;
			}

			survivalService.DamageCondition(amount);
			return true;
		}

		public static bool AdvanceHours(IGameTimeService gameTimeService, int hours)
		{
			if (gameTimeService == null || hours <= 0)
			{
				return false;
			}

			gameTimeService.AdvanceHours(hours);
			return true;
		}

		public static bool SetCurrentDayTime(IGameTimeService gameTimeService, int hour, int minute)
		{
			if (gameTimeService == null)
			{
				return false;
			}

			GameTime currentTime = gameTimeService.CurrentTime;
			gameTimeService.SetTotalMinutes(GameTime.ToTotalMinutes(currentTime.Day, hour, minute));
			return true;
		}

		public static int AddStarterKit(
			IInventoryService inventoryService,
			IReadOnlyList<ItemDefinition> items
		)
		{
			if (inventoryService == null || items == null)
			{
				return 0;
			}

			HashSet<string> addedItemIds = new(StringComparer.Ordinal);
			int addedStacks = 0;

			addedStacks += AddMatchingItems(
				inventoryService,
				items,
				item => item is ConsumableItemDefinition,
				4,
				2,
				addedItemIds
			);
			addedStacks += AddMatchingItems(
				inventoryService,
				items,
				item => item is FuelItemDefinition || item.Category == ItemCategory.Fuel,
				10,
				1,
				addedItemIds
			);
			addedStacks += AddMatchingItems(
				inventoryService,
				items,
				item => item is PlaceableItemDefinition,
				1,
				1,
				addedItemIds
			);

			return addedStacks;
		}

		private static int AddMatchingItems(
			IInventoryService inventoryService,
			IReadOnlyList<ItemDefinition> items,
			Func<ItemDefinition, bool> predicate,
			int amount,
			int maxItems,
			HashSet<string> addedItemIds
		)
		{
			int addedStacks = 0;

			for (int i = 0; i < items.Count; i++)
			{
				ItemDefinition item = items[i];
				if (addedStacks >= maxItems)
				{
					break;
				}

				if (item == null ||
				    string.IsNullOrWhiteSpace(item.Id) ||
				    addedItemIds.Contains(item.Id) ||
				    !predicate(item))
				{
					continue;
				}

				InventoryAddResult result = inventoryService.AddItem(item, amount);
				if (!result.IsSuccess)
				{
					continue;
				}

				addedItemIds.Add(item.Id);
				addedStacks++;
			}

			return addedStacks;
		}
	}
}
