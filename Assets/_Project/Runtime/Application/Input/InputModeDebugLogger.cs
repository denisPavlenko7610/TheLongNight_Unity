using TLN.Core.Input;
using TLN.Core.Lifetime;
using UnityEngine;

namespace TLN.Application.Input
{
	public sealed class InputModeDebugLogger : IGameService, IInitializable, IDisposableService
	{
		private readonly IInputModeService _inputModeService;

		public InputModeDebugLogger(IInputModeService inputModeService)
		{
			_inputModeService = inputModeService;
		}

		public void Initialize()
		{
			_inputModeService.ModeChanged += OnModeChanged;
		}

		public void Dispose()
		{
			_inputModeService.ModeChanged -= OnModeChanged;
		}

		private void OnModeChanged(InputModeId previousMode, InputModeId nextMode)
		{
			Debug.Log($"Input mode changed: {previousMode} -> {nextMode}");
		}
	}
}
