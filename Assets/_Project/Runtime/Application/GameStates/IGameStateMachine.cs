using System;
using TLN.Core.Lifetime;

namespace TLN.Application.GameStates
{
	public interface IGameStateMachine : IGameService
	{
		GameStateId CurrentState { get; }

		event Action<GameStateId, GameStateId> StateChanged;

		void Enter(GameStateId nextState);
	}
}
