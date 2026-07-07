using System;
using TLN.Core.Input;
using TLN.Core.Lifetime;

namespace TLN.Application.Input
{
	public interface IInputModeService : IGameService
	{
		bool CanUseGameplayInput { get; }
		bool CanUseMovementInput { get; }
		bool CanUseLookInput { get; }

		bool IsCurrent(InputModeId mode);
		IDisposable AcquireUIMode();
		void SetGameplayMode();
		void SetUIMode();
		void SetBlockedMode();
	}
}
