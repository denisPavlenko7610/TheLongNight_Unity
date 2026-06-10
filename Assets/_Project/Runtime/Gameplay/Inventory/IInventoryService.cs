using System;
using System.Collections.Generic;
using TLN.Core.Lifetime;
using TLN.Gameplay.Items;

namespace TLN.Gameplay.Inventory
{
	public interface IInventoryService : IGameService
	{
		IReadOnlyList<ItemStack> Items { get; }

		float CurrentWeight { get; }
		float MaxCarryWeight { get; }

		event Action Changed;

		InventoryAddResult AddItem(ItemDefinition definition, int amount);
		bool CanAddItem(ItemDefinition definition, int amount, out string reason);
		bool TryRemoveItemAt(int index, int amount, out string reason);
		bool TryRemoveItem(ItemDefinition definition, int amount, out string reason);
	}
}
