using TLN.Application.GameStates;
using TLN.Application.Input;
using TLN.Core.GameStates;
using TLN.Gameplay.Player.Input;
using UnityEngine;

namespace TLN.Gameplay.Player.Input
{
	public sealed class PlayerPauseController : MonoBehaviour
	{
		[SerializeField] private PlayerInputReader _inputReader;

		private IGameStateMachine _gameStateMachine;
		private IInputModeService _inputModeService;
		private IPauseMenuView _pauseMenuView;

		public void Construct(IGameStateMachine gameStateMachine, IInputModeService inputModeService, IPauseMenuView pauseMenuView)
		{
			_gameStateMachine = gameStateMachine;
			_inputModeService = inputModeService;
			_pauseMenuView = pauseMenuView;
		}

		private void Awake()
		{
			if (_inputReader == null)
			{
				_inputReader = GetComponent<PlayerInputReader>();
			}
		}

		private void Update()
		{
			if (_gameStateMachine == null)
			{
				return;
			}

			if (!_inputReader.WasPausePressedThisFrame)
			{
				return;
			}

			TogglePause();
		}

		private void TogglePause()
		{
			if (_gameStateMachine.CurrentState == GameStateId.Playing)
			{
				_gameStateMachine.Enter(GameStateId.Paused);
				_inputModeService.SetUIMode();
				_pauseMenuView.Show();
				return;
			}

			if (_gameStateMachine.CurrentState == GameStateId.Paused)
			{
				_gameStateMachine.Enter(GameStateId.Playing);
				_inputModeService.SetGameplayMode();
				_pauseMenuView.Hide();
			}
		}
	}
}
