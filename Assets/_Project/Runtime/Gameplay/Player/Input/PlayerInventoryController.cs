using TLN.Application.GameStates;
using TLN.Gameplay.Inventory;
using UnityEngine;
using VContainer;

namespace TLN.Gameplay.Player.Input
{
	public sealed class PlayerInventoryController : MonoBehaviour
	{
		[SerializeField] private PlayerInputReader _inputReader;

		private IInventoryWindow _inventoryWindow;
		private IGameStateMachine _gameStateMachine;

		private void Awake()
		{
			if (_inputReader == null)
			{
				_inputReader = GetComponent<PlayerInputReader>();
			}
		}

		private void Update()
		{
			if (_inventoryWindow == null)
			{
				return;
			}

			if (_gameStateMachine != null && _gameStateMachine.CurrentState != GameStateId.Playing)
			{
				return;
			}

			if (!_inputReader.WasInventoryPressedThisFrame)
			{
				return;
			}

			_inventoryWindow.Toggle();
		}

		[Inject]
		public void Construct(IInventoryWindow inventoryWindow, IGameStateMachine gameStateMachine)
		{
			_inventoryWindow = inventoryWindow;
			_gameStateMachine = gameStateMachine;
		}
	}
}
