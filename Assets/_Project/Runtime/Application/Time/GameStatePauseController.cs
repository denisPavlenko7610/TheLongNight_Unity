using System;
using TLN.Application.GameStates;
using VContainer.Unity;

namespace TLN.Application.Time
{
	public sealed class GameStatePauseController : IInitializable, IDisposable
	{
		private readonly IGameStateMachine _gameStateMachine;
		private readonly IGamePauseService _pauseService;

		public GameStatePauseController(IGameStateMachine gameStateMachine, IGamePauseService pauseService)
		{
			_gameStateMachine = gameStateMachine ?? throw new ArgumentNullException(nameof(gameStateMachine));
			_pauseService = pauseService ?? throw new ArgumentNullException(nameof(pauseService));
		}

		public void Initialize()
		{
			_gameStateMachine.StateChanged += OnGameStateChanged;
			ApplyState(_gameStateMachine.CurrentState);
		}

		public void Dispose()
		{
			_gameStateMachine.StateChanged -= OnGameStateChanged;
			_pauseService.Reset();
		}

		private void OnGameStateChanged(GameStateId _, GameStateId nextState)
		{
			ApplyState(nextState);
		}

		private void ApplyState(GameStateId state)
		{
			_pauseService.SetSimulationPaused(ShouldPauseSimulation(state));
			_pauseService.SetAudioPaused(ShouldPauseAudio(state));
		}

		private static bool ShouldPauseSimulation(GameStateId state)
		{
			return state is GameStateId.Paused or GameStateId.Loading;
		}

		private static bool ShouldPauseAudio(GameStateId state)
		{
			return state == GameStateId.Paused;
		}
	}
}
