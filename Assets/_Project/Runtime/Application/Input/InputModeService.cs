using System;
using TLN.Core.Input;

namespace TLN.Application.Input
{
	public sealed class InputModeService : IInputModeService
	{
		private readonly ICursorService _cursorService;

		public InputModeId CurrentMode { get; private set; } = InputModeId.None;

		public bool CanUseGameplayInput => CurrentMode == InputModeId.Gameplay;
		public bool CanUseMovementInput => CurrentMode == InputModeId.Gameplay;
		public bool CanUseLookInput => CurrentMode == InputModeId.Gameplay;

		public event Action<InputModeId, InputModeId> ModeChanged;

		public InputModeService(ICursorService cursorService)
		{
			_cursorService = cursorService;
		}

		public void SetGameplayMode()
		{
			SetMode(InputModeId.Gameplay);
		}

		public void SetUIMode()
		{
			SetMode(InputModeId.UI);
		}

		public void SetBlockedMode()
		{
			SetMode(InputModeId.Blocked);
		}

		private void SetMode(InputModeId nextMode)
		{
			if (nextMode == InputModeId.None)
			{
				throw new ArgumentException("Cannot set None input mode.", nameof(nextMode));
			}

			if (CurrentMode == nextMode)
			{
				return;
			}

			InputModeId previousMode = CurrentMode;
			CurrentMode = nextMode;

			ApplyCursorMode(nextMode);

			ModeChanged?.Invoke(previousMode, nextMode);
		}

		private void ApplyCursorMode(InputModeId mode)
		{
			switch (mode)
			{
				case InputModeId.Gameplay:
					_cursorService.LockGameplayCursor();
					break;

				case InputModeId.UI:
				case InputModeId.Blocked:
					_cursorService.UnlockUICursor();
					break;

				default:
					throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
			}
		}
	}
}
