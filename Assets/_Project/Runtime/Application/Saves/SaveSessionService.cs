using System;

namespace TLN.Application.Saves
{
    public sealed class SaveSessionService
    {
        private const int DefaultSlotId = 1;

        public int ActiveSlotId { get; private set; } = DefaultSlotId;

        public bool ShouldLoadActiveSlot { get; private set; }

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

        private static int ValidateSlot(int slotId)
        {
            return slotId is < 1 or > 3
                ? throw new ArgumentOutOfRangeException(nameof(slotId), slotId, "Save slot must be between 1 and 3.")
                : slotId;
        }
    }
}
