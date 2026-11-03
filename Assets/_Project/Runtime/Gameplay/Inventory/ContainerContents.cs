using System;
using System.Collections.Generic;
using TLN.Application.Localization;
using TLN.Gameplay.Items;

namespace TLN.Gameplay.Inventory
{
	/// <summary>
	/// Plain in-memory contents of a loot container (no carry weight limit).
	/// </summary>
	public sealed class ContainerContents
	{
		private readonly List<ItemStack> _items = new();

		public IReadOnlyList<ItemStack> Items => _items;

		public float CurrentWeight { get; private set; }

		public event Action Changed;

		public void ReplaceItems(IReadOnlyList<ItemStack> items)
		{
			if (ReferenceEquals(items, _items))
			{
				items = new List<ItemStack>(items);
			}

			_items.Clear();

			if (items != null)
			{
				for (int i = 0; i < items.Count; i++)
				{
					ItemStack stack = items[i];

					if (stack.Definition == null || stack.Amount <= 0)
					{
						continue;
					}

					_items.Add(stack);
				}
			}

			RecalculateWeight();
			Changed?.Invoke();
		}

		public bool CanAddItem(ItemDefinition definition, int amount)
		{
			return definition != null && amount > 0;
		}

		public InventoryAddResult AddItem(ItemDefinition definition, int amount)
		{
			if (definition == null)
			{
				return InventoryAddResult.Failure(Loc.InventoryInvalidItem);
			}

			if (amount <= 0)
			{
				return InventoryAddResult.Failure(Loc.InvalidAmount);
			}

			if (definition.IsStackable)
			{
				AddStackableItem(definition, amount);
			}
			else
			{
				for (int i = 0; i < amount; i++)
				{
					_items.Add(new ItemStack(definition, 1));
				}
			}

			RecalculateWeight();
			Changed?.Invoke();

			return InventoryAddResult.Success();
		}

		public bool TryRemoveItemAt(int index, int amount, out string reason)
		{
			if (index < 0 || index >= _items.Count)
			{
				reason = Loc.InventoryInvalidSlot;
				return false;
			}

			if (amount <= 0)
			{
				reason = Loc.InvalidAmount;
				return false;
			}

			ItemStack stack = _items[index];

			if (amount > stack.Amount)
			{
				reason = Loc.NotEnough;
				return false;
			}

			stack.RemoveAmount(amount);

			if (stack.Amount <= 0)
			{
				_items.RemoveAt(index);
			}
			else
			{
				_items[index] = stack;
			}

			RecalculateWeight();
			Changed?.Invoke();

			reason = string.Empty;
			return true;
		}

		public int CountItems(ItemDefinition definition)
		{
			if (definition == null)
			{
				return 0;
			}

			int amount = 0;

			for (int i = 0; i < _items.Count; i++)
			{
				ItemStack stack = _items[i];

				if (stack.Definition == definition)
				{
					amount += stack.Amount;
				}
			}

			return amount;
		}

		private void AddStackableItem(ItemDefinition definition, int amount)
		{
			int remainingAmount = amount;

			for (int i = 0; i < _items.Count; i++)
			{
				ItemStack stack = _items[i];

				if (stack.Definition != definition)
				{
					continue;
				}

				int freeSpace = definition.MaxStackSize - stack.Amount;

				if (freeSpace <= 0)
				{
					continue;
				}

				int amountToAdd = Math.Min(remainingAmount, freeSpace);

				stack.AddAmount(amountToAdd);
				_items[i] = stack;

				remainingAmount -= amountToAdd;

				if (remainingAmount <= 0)
				{
					return;
				}
			}

			while (remainingAmount > 0)
			{
				int amountToAdd = Math.Min(remainingAmount, definition.MaxStackSize);
				_items.Add(new ItemStack(definition, amountToAdd));
				remainingAmount -= amountToAdd;
			}
		}

		private void RecalculateWeight()
		{
			float totalWeight = 0f;

			for (int i = 0; i < _items.Count; i++)
			{
				ItemStack stack = _items[i];
				totalWeight += stack.Definition.Weight * stack.Amount;
			}

			CurrentWeight = totalWeight;
		}
	}
}
