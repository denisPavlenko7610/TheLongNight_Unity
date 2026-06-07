using TLN.Application.GameStates;
using TLN.Core.GameStates;
using TLN.Core.Lifetime;

namespace TLN.Application.Time
{
	public sealed class GameStateTimeScaleController : IGameService, IInitializable, IDisposableService, VContainer.Unity.IInitializable, System.IDisposable
	{
		private readonly IGameStateMachine _gameStateMachine;
		private readonly IGameTimeScaleService _timeScaleService;

		public GameStateTimeScaleController(IGameStateMachine gameStateMachine, IGameTimeScaleService timeScaleService)
		{
			_gameStateMachine = gameStateMachine;
			_timeScaleService = timeScaleService;
		}

		public void Initialize()
		{
			_gameStateMachine.StateChanged += OnGameStateChanged;
		}

		public void Dispose()
		{
			_gameStateMachine.StateChanged -= OnGameStateChanged;
		}

		private void OnGameStateChanged(GameStateId previousState, GameStateId nextState)
		{
			switch (nextState)
			{
				case GameStateId.Paused:
				case GameStateId.Loading:
					_timeScaleService.SetPaused();
					break;

				case GameStateId.Playing:
				case GameStateId.MainMenu:
				case GameStateId.Boot:
				case GameStateId.GameOver:
					_timeScaleService.SetNormal();
					break;
			}
		}
	}
}