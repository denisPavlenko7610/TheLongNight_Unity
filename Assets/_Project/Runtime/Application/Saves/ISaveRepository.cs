using System.Collections.Generic;

namespace TLN.Application.Saves
{
	public interface ISaveRepository
	{
		int SlotCount { get; }

		bool SaveExists(int slotId);
		bool TryGetMostRecentSlot(out int slotId);
		bool Delete(int slotId);

		GameSaveData Load(int slotId);
		void Save(GameSaveData data);

		IReadOnlyList<SaveSlotSummary> GetSlotSummaries();
	}
}
