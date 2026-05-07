using TLN.Application.GameStates;
using TLN.Core.GameStates;
using TLN.Gameplay.Player.Input;
using UnityEngine;

namespace TLN.Gameplay.Player.Input
{
	public sealed class PlayerPauseController : MonoBehaviour
	{
		[SerializeField] private PlayerInputReader _inputReader;

		private IGameStateMachine _gameStateMachine;

		public void Construct(IGameStateMachine gameStateMachine)
		{
			_gameStateMachine = gameStateMachine;
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
			GameStateId currentState = _gameStateMachine.CurrentState;

			if (currentState == GameStateId.Playing)
			{
				_gameStateMachine.Enter(GameStateId.Paused);
				return;
			}

			if (currentState == GameStateId.Paused)
			{
				_gameStateMachine.Enter(GameStateId.Playing);
			}
		}
	}
}
