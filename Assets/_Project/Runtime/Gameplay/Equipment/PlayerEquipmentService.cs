using System;
using System.Collections.Generic;
using TLN.Application.Localization;
using TLN.Core.Results;

namespace TLN.Gameplay.Equipment
{
	public sealed class PlayerEquipmentService : IPlayerEquipmentService
	{
		private const int DefaultSlotCapacity = 2;

		private readonly List<ClothingItemDefinition> _equippedItems = new();
		public IReadOnlyList<ClothingItemDefinition> EquippedItems => _equippedItems;
		public float WarmthBonus { get; private set; }

		public event Action Changed;

		public PlayerEquipmentService()
		{
		}

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

				if (item == null)
				{
					continue;
				}

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
				RecalculateWarmthBonus();
				Changed?.Invoke();

				return OperationResult.Success(Loc.Unequipped(item.DisplayName));
			}

			int equippedCount = GetEquippedCount(item.Slot);
			int slotCapacity = GetSlotCapacity(item.Slot);

			if (equippedCount >= slotCapacity)
			{
				return OperationResult.Failure(Loc.NoFreeSlot(item.Slot));
			}

			_equippedItems.Add(item);
			RecalculateWarmthBonus();
			Changed?.Invoke();

			return OperationResult.Success(Loc.Equipped(item.DisplayName));
		}

		private void RecalculateWarmthBonus()
		{
			float totalWarmthBonus = 0f;

			for (int i = 0; i < _equippedItems.Count; i++)
			{
				ClothingItemDefinition item = _equippedItems[i];

				if (item == null)
				{
					continue;
				}

				totalWarmthBonus += item.WarmthBonus;
			}

			WarmthBonus = totalWarmthBonus;
		}
	}
}
