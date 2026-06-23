namespace TLN.Application.Saves
{
	public readonly struct SaveSlotSummary
	{
		public int SlotId { get; }
		public bool HasSave { get; }
		public string SavedAtUtc { get; }
		public string SaveReason { get; }

		public SaveSlotSummary(int slotId, bool hasSave, string savedAtUtc, string saveReason)
		{
			SlotId = slotId;
			HasSave = hasSave;
			SavedAtUtc = savedAtUtc ?? string.Empty;
			SaveReason = saveReason ?? string.Empty;
		}
	}
}
