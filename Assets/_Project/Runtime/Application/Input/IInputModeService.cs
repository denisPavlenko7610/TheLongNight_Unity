using System;
using TLN.Core.Input;
using TLN.Core.Lifetime;

namespace TLN.Application.Input
{
	public interface IInputModeService : IGameService
	{
		InputModeId CurrentMode { get; }

		bool CanUseGameplayInput { get; }
		bool CanUseMovementInput { get; }
		bool CanUseLookInput { get; }

		event Action<InputModeId, InputModeId> ModeChanged;

		void SetGameplayMode();
		void SetUIMode();
		void SetBlockedMode();
	}
}
