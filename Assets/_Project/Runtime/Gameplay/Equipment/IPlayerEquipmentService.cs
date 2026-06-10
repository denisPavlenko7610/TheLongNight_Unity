using System;
using System.Collections.Generic;
using TLN.Core.Lifetime;
using TLN.Core.Results;

namespace TLN.Gameplay.Equipment
{
	public interface IPlayerEquipmentService : IGameService
	{
		IReadOnlyList<ClothingItemDefinition> EquippedItems { get; }
		float WarmthBonus { get; }

		event Action Changed;

		bool IsEquipped(ClothingItemDefinition item);
		int GetEquippedCount(ClothingSlotId slot);
		int GetSlotCapacity(ClothingSlotId slot);
		OperationResult ToggleEquip(ClothingItemDefinition item);
	}
}
