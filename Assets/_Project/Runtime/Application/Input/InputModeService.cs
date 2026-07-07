using System;
using System.Collections.Generic;
using TLN.Core.Input;

namespace TLN.Application.Input
{
	public sealed class InputModeService : IInputModeService
	{
		private readonly HashSet<InputModeScope> _uiModeScopes = new();
		private readonly ICursorService _cursorService;

		private InputModeId _baseMode = InputModeId.None;

		public bool CanUseGameplayInput => IsCurrent(InputModeId.Gameplay);
		public bool CanUseMovementInput => CanUseGameplayInput;
		public bool CanUseLookInput => CanUseGameplayInput;

		private InputModeId CurrentMode { get; set; } = InputModeId.None;

		public InputModeService(ICursorService cursorService)
		{
			_cursorService = cursorService;
		}

		public bool IsCurrent(InputModeId mode)
		{
			return CurrentMode == mode;
		}

		public IDisposable AcquireUIMode()
		{
			InputModeScope scope = new InputModeScope(this);
			_uiModeScopes.Add(scope);
			RefreshCurrentMode();

			return scope;
		}

		public void SetGameplayMode()
		{
			SetBaseMode(InputModeId.Gameplay);
		}

		public void SetUIMode()
		{
			SetBaseMode(InputModeId.UI);
		}

		public void SetBlockedMode()
		{
			SetBaseMode(InputModeId.Blocked);
		}

		private void SetBaseMode(InputModeId nextMode)
		{
			if (nextMode == InputModeId.None)
			{
				throw new ArgumentException("Cannot set None input mode.", nameof(nextMode));
			}

			if (_baseMode == nextMode)
			{
				return;
			}

			_baseMode = nextMode;
			RefreshCurrentMode();
		}

		private void ReleaseUIMode(InputModeScope scope)
		{
			if (!_uiModeScopes.Remove(scope))
			{
				return;
			}

			RefreshCurrentMode();
		}

		private void RefreshCurrentMode()
		{
			InputModeId nextMode = ResolveCurrentMode();

			if (CurrentMode == nextMode)
			{
				return;
			}

			CurrentMode = nextMode;

			ApplyCursorMode(nextMode);
		}

		private InputModeId ResolveCurrentMode()
		{
			if (_baseMode == InputModeId.Blocked)
			{
				return InputModeId.Blocked;
			}

			if (_baseMode == InputModeId.UI || _uiModeScopes.Count > 0)
			{
				return InputModeId.UI;
			}

			return _baseMode;
		}

		private void ApplyCursorMode(InputModeId mode)
		{
			switch (mode)
			{
				case InputModeId.Gameplay:
					_cursorService.LockGameplayCursor();
					break;

				case InputModeId.None:
				case InputModeId.UI:
				case InputModeId.Blocked:
					_cursorService.UnlockUICursor();
					break;

				default:
					throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
			}
		}

		private sealed class InputModeScope : IDisposable
		{
			private InputModeService _owner;

			public InputModeScope(InputModeService owner)
			{
				_owner = owner;
			}

			public void Dispose()
			{
				InputModeService owner = _owner;
				if (owner == null)
				{
					return;
				}

				_owner = null;
				owner.ReleaseUIMode(this);
			}
		}
	}
}
