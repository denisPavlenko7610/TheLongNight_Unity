using TLN.Application.GameStates;
using TLN.Application.Input;
using TLN.Core.GameStates;
using TLN.Gameplay.Inventory;
using UnityEngine;

namespace TLN.Gameplay.Player.Input
{
	public sealed class PlayerInventoryController : MonoBehaviour
	{
		[SerializeField] private PlayerInputReader _inputReader;

		private IInventoryWindow _inventoryWindow;
		private IInputModeService _inputModeService;
		private bool _isInventoryOpen;
		private IGameStateMachine _gameStateMachine;

		private void Awake()
		{
			if (_inputReader == null) {
				_inputReader = GetComponent<PlayerInputReader>();
			}
		}

		private void Update()
		{
			if (_inventoryWindow == null) {
				return;
			}

			if (_gameStateMachine != null && _gameStateMachine.CurrentState != GameStateId.Playing) {
				return;
			}

			if (!_inputReader.WasInventoryPressedThisFrame) {
				return;
			}

			ToggleInventory();
		}

		public void Construct(
			IInventoryWindow inventoryWindow,
			IInputModeService inputModeService,
			IGameStateMachine gameStateMachine
		)
		{
			_inventoryWindow = inventoryWindow;
			_inputModeService = inputModeService;
			_gameStateMachine = gameStateMachine;
		}

		private void ToggleInventory()
		{
			_isInventoryOpen = !_isInventoryOpen;

			if (_isInventoryOpen) {
				_inventoryWindow.Toggle();
				_inputModeService?.SetUIMode();
				return;
			}

			_inventoryWindow.Hide();
			_inputModeService?.SetGameplayMode();
		}
	}
}
