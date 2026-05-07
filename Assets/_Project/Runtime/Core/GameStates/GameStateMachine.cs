using System;
using TLN.Core.GameStates;

namespace TLN.Application.GameStates
{
	public sealed class GameStateMachine : IGameStateMachine
	{
		public GameStateId CurrentState { get; private set; } = GameStateId.None;

		public event Action<GameStateId, GameStateId> StateChanged;

		public void Enter(GameStateId nextState)
		{
			if (nextState == GameStateId.None)
			{
				throw new ArgumentException("Cannot enter None game state.", nameof(nextState));
			}

			if (CurrentState == nextState)
			{
				return;
			}

			GameStateId previousState = CurrentState;
			CurrentState = nextState;

			StateChanged?.Invoke(previousState, nextState);
		}
	}
}
