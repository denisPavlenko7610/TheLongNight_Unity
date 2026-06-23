using System;
using TLN.Application.GameStates;
using TLN.Core.GameStates;
using VContainer.Unity;

namespace TLN.Application.Input
{
	public sealed class GameStateInputModeController : IInitializable, IDisposable
	{
		private readonly IGameStateMachine _gameStateMachine;
		private readonly IInputModeService _inputModeService;

		public GameStateInputModeController(IGameStateMachine gameStateMachine, IInputModeService inputModeService)
		{
			_gameStateMachine = gameStateMachine;
			_inputModeService = inputModeService;
		}

		public void Initialize()
		{
			_gameStateMachine.StateChanged += OnGameStateChanged;

			if (_gameStateMachine.CurrentState != GameStateId.None)
			{
				OnGameStateChanged(GameStateId.None, _gameStateMachine.CurrentState);
			}
		}

		public void Dispose()
		{
			_gameStateMachine.StateChanged -= OnGameStateChanged;
		}

		private void OnGameStateChanged(GameStateId previousState, GameStateId nextState)
		{
			switch (nextState)
			{
				case GameStateId.Boot:
				case GameStateId.Loading:
					_inputModeService.SetBlockedMode();
					break;

				case GameStateId.MainMenu:
				case GameStateId.Paused:
				case GameStateId.GameOver:
					_inputModeService.SetUIMode();
					break;

				case GameStateId.Playing:
					_inputModeService.SetGameplayMode();
					break;
			}
		}
	}
}
