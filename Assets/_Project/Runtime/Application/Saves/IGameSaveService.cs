using UnityEngine;

namespace TLN.Application.Saves
{
	public interface IGameSaveService
	{
		bool CanSaveManually { get; }

		Awaitable<bool> SaveManual();
		Awaitable<bool> SaveCheckpoint(SaveTrigger trigger);

		bool LoadActiveSlotIfRequested();
	}
}
