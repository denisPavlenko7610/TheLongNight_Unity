using TLN.Core.GameStates;
using TLN.Core.Lifetime;
using UnityEngine;

namespace TLN.Application.GameStates
{
	public sealed class GameStateDebugLogger : IGameService, IInitializable, IDisposableService
	{
		private readonly IGameStateMachine _gameStateMachine;

		public GameStateDebugLogger(IGameStateMachine gameStateMachine)
		{
			_gameStateMachine = gameStateMachine;
		}

		public void Initialize()
		{
			_gameStateMachine.StateChanged += OnStateChanged;
		}

		public void Dispose()
		{
			_gameStateMachine.StateChanged -= OnStateChanged;
		}

		private void OnStateChanged(GameStateId previousState, GameStateId nextState)
		{
			Debug.Log($"Game state changed: {previousState} -> {nextState}");
		}
	}
}
