using System;

namespace TLN.Application.Saves
{
	public sealed class SaveSessionService
	{
		private const int DefaultSlotId = 1;

		private readonly ISaveRepository _saveRepository;

		public int ActiveSlotId { get; private set; } = DefaultSlotId;

		public bool ShouldLoadActiveSlot { get; private set; }

		public SaveSessionService(ISaveRepository saveRepository)
		{
			_saveRepository = saveRepository ?? throw new ArgumentNullException(nameof(saveRepository));

			if (_saveRepository.SlotCount < DefaultSlotId)
			{
				throw new InvalidOperationException("Save repository must expose at least one slot.");
			}
		}

		public void StartNewGame(int slotId)
		{
			ActiveSlotId = ValidateSlot(slotId);
			ShouldLoadActiveSlot = false;
		}

		public void RequestLoadGame(int slotId)
		{
			ActiveSlotId = ValidateSlot(slotId);
			ShouldLoadActiveSlot = true;
		}

		public void ConsumeLoadRequest()
		{
			ShouldLoadActiveSlot = false;
		}

		private int ValidateSlot(int slotId)
		{
			int slotCount = _saveRepository.SlotCount;

			return slotId < 1 || slotId > slotCount
				? throw new ArgumentOutOfRangeException(nameof(slotId), slotId, $"Save slot must be between 1 and {slotCount}.")
				: slotId;
		}
	}
}
