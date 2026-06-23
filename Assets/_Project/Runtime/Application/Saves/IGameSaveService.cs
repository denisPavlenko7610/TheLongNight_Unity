namespace TLN.Application.Saves
{
	public interface IGameSaveService
	{
		bool CanSaveManually { get; }

		bool SaveManual();
		bool SaveCheckpoint(SaveTrigger trigger);

		bool LoadActiveSlotIfRequested();
	}
}
