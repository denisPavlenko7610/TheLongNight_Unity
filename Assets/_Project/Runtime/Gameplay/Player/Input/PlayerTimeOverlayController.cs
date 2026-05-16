using Assign;
using UnityEngine;

namespace TLN.Gameplay.Player.Input
{
	public sealed class PlayerTimeOverlayController : MonoBehaviour
	{
		[SerializeField][Assign] private PlayerInputReader _inputReader;

		private ITimeOverlayView _timeOverlayView;
		private bool _wasStatusHeldPreviousFrame;

		public void Construct(ITimeOverlayView timeOverlayView)
		{
			_timeOverlayView = timeOverlayView;
		}

		private void Update()
		{
			if (_timeOverlayView == null)
			{
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
