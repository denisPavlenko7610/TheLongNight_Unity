using TLN.Application.GameStates;
using TLN.Core.GameStates;
using UnityEngine;
using VContainer;

namespace TLN.Gameplay.Player.Input
{
	public sealed class PlayerPauseController : MonoBehaviour
	{
		[SerializeField] private PlayerInputReader _inputReader;

		private IGameStateMachine _gameStateMachine;

		[Inject]
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
			switch (_gameStateMachine.CurrentState)
			{
				case GameStateId.Playing:
					_inputReader.ClearGameplayInput();
					_gameStateMachine.Enter(GameStateId.Paused);
					break;

				case GameStateId.Paused:
					_inputReader.ClearGameplayInput();
					_gameStateMachine.Enter(GameStateId.Playing);
					break;
			}
		}
	}
}