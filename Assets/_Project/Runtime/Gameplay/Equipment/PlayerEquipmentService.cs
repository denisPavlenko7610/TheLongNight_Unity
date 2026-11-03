using System;
using System.Collections.Generic;
using TLN.Application.Localization;
using TLN.Core.Results;
using TLN.Gameplay.Inventory;
using TLN.Gameplay.Items;

namespace TLN.Gameplay.Equipment
{
	public sealed class PlayerEquipmentService : IPlayerEquipmentService, IDisposable
	{
		private const int DefaultSlotCapacity = 2;

		private readonly List<ClothingItemDefinition> _equippedItems = new();
		private readonly IInventoryService _inventory;

		public PlayerEquipmentService(IInventoryService inventory)
		{
			_inventory = inventory;
			_inventory.Changed += OnInventoryChanged;
		}

		public void Dispose() => _inventory.Changed -= OnInventoryChanged;

		private bool HasItem(ClothingItemDefinition item)
		{
			foreach (ItemStack stack in _inventory.Items)
			{
				if (stack.Definition.Id == item.Id && stack.Amount > 0)
					return true;
			}
			return false;
		}

		private void OnInventoryChanged()
		{
			if (_equippedItems.RemoveAll(item => !HasItem(item)) > 0)
				ApplyEquipmentChanged();
		}

		public void RestoreEquipment(IReadOnlyList<ClothingItemDefinition> items)
		{
			_equippedItems.Clear();
			if (items != null)
			{
				foreach (ClothingItemDefinition item in items)
				{
					if (item != null &&
						HasItem(item) &&
						!IsEquipped(item) &&
						GetEquippedCount(item.Slot) < GetSlotCapacity(item.Slot))
						_equippedItems.Add(item);
				}
			}
			ApplyEquipmentChanged();
		}

		public IReadOnlyList<ClothingItemDefinition> EquippedItems => _equippedItems;
		public float WarmthBonus { get; private set; }

		public event Action Changed;

		public bool IsEquipped(ClothingItemDefinition item)
		{
			return item != null && _equippedItems.Contains(item);
		}

		public int GetEquippedCount(ClothingSlotId slot)
		{
			int count = 0;

			for (int i = 0; i < _equippedItems.Count; i++)
			{
				ClothingItemDefinition item = _equippedItems[i];

				if (item.Slot == slot)
				{
					count++;
				}
			}

			return count;
		}

		public int GetSlotCapacity(ClothingSlotId slot)
		{
			return slot == ClothingSlotId.None ? 0 : DefaultSlotCapacity;
		}

		public OperationResult ToggleEquip(ClothingItemDefinition item)
		{
			if (item == null)
			{
				return OperationResult.Failure(Loc.EquipmentItemMissing);
			}

			if (item.Slot == ClothingSlotId.None)
			{
				return OperationResult.Failure(Loc.EquipmentSlotMissing);
			}

			if (IsEquipped(item))
			{
				_equippedItems.Remove(item);
				ApplyEquipmentChanged();

				return OperationResult.Success(Loc.Unequipped(item.DisplayName));
			}

			if (GetEquippedCount(item.Slot) >= GetSlotCapacity(item.Slot))
			{
				return OperationResult.Failure(Loc.NoFreeSlot(item.Slot));
			}

			if (!HasItem(item))
				return OperationResult.Failure(Loc.EquipmentItemMissing);

			_equippedItems.Add(item);
			ApplyEquipmentChanged();

			return OperationResult.Success(Loc.Equipped(item.DisplayName));
		}

		private void ApplyEquipmentChanged()
		{
			RecalculateWarmthBonus();
			Changed?.Invoke();
		}

		private void RecalculateWarmthBonus()
		{
			float totalWarmthBonus = 0f;

			for (int i = 0; i < _equippedItems.Count; i++)
			{
				ClothingItemDefinition item = _equippedItems[i];
				totalWarmthBonus += item.WarmthBonus;
			}

			WarmthBonus = totalWarmthBonus;
		}
	}
}