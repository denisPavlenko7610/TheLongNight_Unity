using Assign;
using TLN.Application.GameStates;
using TLN.Application.Input;
using TLN.Core.Validation;
using TLN.Gameplay.Building;
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
		[field: SerializeField, Assign, Required] public PlayerInputReader InputReader { get; private set; }
		[field: SerializeField, Assign, Required] public PlayerMotor Motor { get; private set; }
		[field: SerializeField, Assign, Required] public PlayerLook Look { get; private set; }
		[field: SerializeField, Assign(Mode.Children), Required] public Camera Camera { get; private set; }
		[field: SerializeField, Assign, Required] public PlayerPauseController PauseController { get; private set; }
		[field: SerializeField, Assign, Required] public PlayerInteractionController InteractionController { get; private set; }
		[field: SerializeField, Assign, Required] public PlayerInventoryController InventoryController { get; private set; }
		[field: SerializeField, Assign, Required] public PlayerTimeOverlayController TimeOverlayController { get; private set; }
		[field: SerializeField, Assign, Required] public PlayerBuildController BuildController { get; private set; }

		[Header("Equipment")]
		//[SerializeField] private FlashlightController _flashlight;

		private IInputModeService _inputModeService;
		private IGameStateMachine _gameStateMachine;
		private IPauseMenuView _pauseMenuView;
		private IInventoryWindow _inventoryWindow;
		private ITimeOverlayView _timeOverlayView;
		private IInteractionPromptView _interactionPromptView;
		private IBuildWindow _buildWindow;

		[Inject]
		public void Construct(IInputModeService inputModeService, IGameStateMachine gameStateMachine, IPauseMenuView pauseMenuView,
			IInventoryWindow inventoryWindow, ITimeOverlayView timeOverlayView, IInteractionPromptView interactionPromptView, IBuildWindow buildWindow)
		{
			_inputModeService = inputModeService;
			_gameStateMachine = gameStateMachine;
			_pauseMenuView = pauseMenuView;
			_inventoryWindow = inventoryWindow;
			_timeOverlayView = timeOverlayView;
			_interactionPromptView = interactionPromptView;
			_buildWindow = buildWindow;

			Motor.Construct(_inputModeService);
			Look.Construct(_inputModeService);
			PauseController.Construct(_gameStateMachine, _inputModeService, _pauseMenuView);
			InteractionController.Construct(_inputModeService, _interactionPromptView);
			InventoryController.Construct(_inventoryWindow, _inputModeService, _gameStateMachine);
			TimeOverlayController.Construct(_timeOverlayView, _inputModeService);
			BuildController.Construct(_buildWindow, _gameStateMachine);
		}

		// public void ToggleFlashlight()
		// {
		// 	if (_flashlight != null)
		// 	{
		// 		_flashlight.Toggle();
		// 	}
		// }
	}
}
