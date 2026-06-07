using Assign;
using TLN.Application.GameStates;
using TLN.Application.Input;
using TLN.Gameplay.Flashlight;
using TLN.Gameplay.Interaction;
using TLN.Gameplay.Inventory;
using TLN.Gameplay.Player.Input;
using TLN.Gameplay.Player.Look;
using TLN.Gameplay.Player.Movement;
using UnityEngine;
using VContainer;

namespace TLN.Gameplay.Player
{
	public sealed class PlayerRoot : MonoBehaviour
	{
		[field: SerializeField][field: Assign] public PlayerInputReader InputReader { get; private set; }
		[field: SerializeField][field: Assign] public PlayerMotor Motor { get; private set; }
		[field: SerializeField][field: Assign] public PlayerLook Look { get; private set; }
		[field: SerializeField][field: Assign(Mode.Children)] public Camera Camera { get; private set; }
		[field: SerializeField][field: Assign] public PlayerPauseController PauseController { get; private set; }
		[field: SerializeField][field: Assign] public PlayerInteractionController InteractionController { get; private set; }
		[field: SerializeField][field: Assign] public PlayerInventoryController InventoryController { get; private set; }
		[field: SerializeField][field: Assign] public PlayerTimeOverlayController TimeOverlayController { get; private set; }

		[Header("Equipment")]
		[SerializeField] private FlashlightController _flashlight;

		private IInputModeService _inputModeService;
		private IGameStateMachine _gameStateMachine;
		private IPauseMenuView _pauseMenuView;
		private IInventoryWindow _inventoryWindow;
		private ITimeOverlayView _timeOverlayView;
		private IInteractionPromptView _interactionPromptView;

		[Inject]
		public void Construct(IInputModeService inputModeService, IGameStateMachine gameStateMachine, IPauseMenuView pauseMenuView,
			IInventoryWindow inventoryWindow, ITimeOverlayView timeOverlayView, IInteractionPromptView interactionPromptView)
		{
			_inputModeService = inputModeService;
			_gameStateMachine = gameStateMachine;
			_pauseMenuView = pauseMenuView;
			_inventoryWindow = inventoryWindow;
			_timeOverlayView = timeOverlayView;
			_interactionPromptView = interactionPromptView;

			Motor.Construct(_inputModeService);
			Look.Construct(_inputModeService);
			PauseController.Construct(_gameStateMachine, _inputModeService, _pauseMenuView);
			InteractionController.Construct(_inputModeService, _interactionPromptView);
			InventoryController.Construct(_inventoryWindow, _inputModeService, _gameStateMachine);
			TimeOverlayController.Construct(_timeOverlayView, _inputModeService);
		}

		public void ToggleFlashlight()
		{
			if (_flashlight != null)
			{
				_flashlight.Toggle();
			}
		}
	}
}
