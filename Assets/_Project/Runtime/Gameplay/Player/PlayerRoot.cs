using Assign;
using TLN.Application.GameStates;
using TLN.Application.Input;
using TLN.Gameplay.Interaction;
using TLN.Gameplay.Inventory;
using TLN.Gameplay.Player.Input;
using TLN.Gameplay.Player.Look;
using TLN.Gameplay.Player.Movement;
using UnityEngine;

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

		public void Construct(IInputModeService inputModeService, IGameStateMachine gameStateMachine, IPauseMenuView pauseMenuView,
			IInteractionPromptView interactionPromptView, IInventoryWindow inventoryWindow, ITimeOverlayView timeOverlayView)
		{
			Motor.Construct(inputModeService);
			Look.Construct(inputModeService);
			PauseController.Construct(gameStateMachine, inputModeService, pauseMenuView);
			InteractionController.Construct(inputModeService, interactionPromptView);
			InventoryController.Construct(inventoryWindow, inputModeService, gameStateMachine);
			TimeOverlayController.Construct(timeOverlayView, inputModeService);
		}
	}
}
