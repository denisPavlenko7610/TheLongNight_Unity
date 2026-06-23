namespace TLN.Gameplay.Saves
{
	public interface IWorldSaveable
	{
		string SaveTypeId { get; }

		string CaptureStateJson();

		void RestoreStateJson(string json);
	}
}
