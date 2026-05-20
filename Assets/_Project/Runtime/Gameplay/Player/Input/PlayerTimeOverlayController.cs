using Assign;
using TLN.Application.Input;
using UnityEngine;

namespace TLN.Gameplay.Player.Input
{
	public sealed class PlayerTimeOverlayController : MonoBehaviour
	{
		[SerializeField][Assign] private PlayerInputReader _inputReader;

		private ITimeOverlayView _timeOverlayView;
		private bool _wasStatusHeldPreviousFrame;
		private IInputModeService _inputModeService;

		public void Construct(ITimeOverlayView timeOverlayView, IInputModeService inputModeService)
		{
			_timeOverlayView = timeOverlayView;
			_inputModeService = inputModeService;
		}

		private void Update()
		{
			if (_timeOverlayView == null)
			{
				return;
			}

			if (_inputModeService != null && !_inputModeService.CanUseGameplayInput)
			{
				_wasStatusHeldPreviousFrame = false;
				return;
			}

			bool isStatusHeld = _inputReader.IsStatusHeld;

			if (isStatusHeld && !_wasStatusHeldPreviousFrame)
			{
				_timeOverlayView.ShowTimeOverlay();
			}

			_wasStatusHeldPreviousFrame = isStatusHeld;
		}
	}
}
