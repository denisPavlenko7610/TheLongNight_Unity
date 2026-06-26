using System;
using System.Collections.Generic;
using TLN.Application.Localization;
using TLN.Core.Logging;
using TLN.Gameplay.Items;

namespace TLN.Gameplay.Inventory
{
	public sealed class InventoryService : IInventoryService
	{
		private readonly InventoryConfig _config;
		private readonly List<ItemStack> _items = new();

		public IReadOnlyList<ItemStack> Items => _items;

		public float CurrentWeight { get; private set; }
		public float MaxCarryWeight => _config.MaxCarryWeight;

		public event Action Changed;

		public InventoryService(InventoryConfig config)
		{
			_config = config;
		}

		public bool CanAddItem(ItemDefinition definition, int amount, out string reason)
		{
			if (definition == null)
			{
				reason = Loc.InventoryInvalidItem;
				return false;
			}

			if (amount <= 0)
			{
				reason = Loc.InvalidAmount;
				return false;
			}

			float addedWeight = definition.Weight * amount;
			float weightAfterAdd = CurrentWeight + addedWeight;

			if (weightAfterAdd > MaxCarryWeight)
			{
				reason = Loc.TooHeavy;
				return false;
			}

			reason = string.Empty;
			return true;
		}

		public void ReplaceItems(IReadOnlyList<ItemStack> items)
		{
			_items.Clear();

			if (items != null)
			{
				for (int i = 0; i < items.Count; i++)
				{
					ItemStack stack = items[i];

					if (stack.Definition == null)
					{
						continue;
					}

					if (stack.Amount <= 0)
					{
						continue;
					}

					_items.Add(stack);
				}
			}

			RecalculateWeight();
			Changed?.Invoke();
		}

		public InventoryAddResult AddItem(ItemDefinition definition, int amount)
		{
			if (!CanAddItem(definition, amount, out string reason))
			{
				TLNLogger.LogWarning($"Inventory: cannot add item. Reason: {reason}");
				return InventoryAddResult.Failure(reason);
			}

			if (definition.IsStackable)
			{
				AddStackableItem(definition, amount);
			}
			else
			{
				AddNonStackableItem(definition, amount);
			}

			RecalculateWeight();

			Changed?.Invoke();

			TLNLogger.Log(
				$"Inventory: added {amount} x {definition.DisplayName}. Weight: {CurrentWeight:0.##}/{MaxCarryWeight:0.##}"
			);

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

		public bool TryRemoveItem(ItemDefinition definition, int amount, out string reason)
		{
			if (definition == null)
			{
				reason = Loc.InventoryInvalidItem;
				return false;
			}

			if (amount <= 0)
			{
				reason = Loc.InvalidAmount;
				return false;
			}

			int availableAmount = CountItems(definition);

			if (availableAmount < amount)
			{
				reason = Loc.InventoryNotEnoughItem(definition.DisplayName);
				return false;
			}

			int remainingAmount = amount;

			for (int i = _items.Count - 1; i >= 0; i--)
			{
				if (remainingAmount <= 0)
				{
					break;
				}

				ItemStack stack = _items[i];

				if (stack.Definition.Id != definition.Id)
				{
					continue;
				}

				int amountToRemove = Math.Min(remainingAmount, stack.Amount);

				stack.RemoveAmount(amountToRemove);

				if (stack.Amount <= 0)
				{
					_items.RemoveAt(i);
				}
				else
				{
					_items[i] = stack;
				}

				remainingAmount -= amountToRemove;
			}

			RecalculateWeight();
			Changed?.Invoke();

			reason = string.Empty;
			return true;
		}

		private int CountItems(ItemDefinition definition)
		{
			if (definition == null)
			{
				return 0;
			}

			int amount = 0;

			for (int i = 0; i < _items.Count; i++)
			{
				ItemStack stack = _items[i];

				if (stack.Definition.Id == definition.Id)
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

				if (stack.Definition.Id != definition.Id)
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

		private void AddNonStackableItem(ItemDefinition definition, int amount)
		{
			for (int i = 0; i < amount; i++)
			{
				_items.Add(new ItemStack(definition, 1));
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
