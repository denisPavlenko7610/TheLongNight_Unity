using TLN.Application.GameStates;
using UnityEngine;
using VContainer;

namespace TLN.Gameplay.Player.Input
{
	[RequireComponent(typeof(PlayerInputReader))]
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
			if (_gameStateMachine.IsCurrent(GameStateId.Playing))
			{
				EnterState(GameStateId.Paused);
				return;
			}

			if (_gameStateMachine.IsCurrent(GameStateId.Paused))
			{
				EnterState(GameStateId.Playing);
			}
		}

		private void EnterState(GameStateId nextState)
		{
			_inputReader.ClearGameplayInput();
			_gameStateMachine.Enter(nextState);
		}
	}
}
