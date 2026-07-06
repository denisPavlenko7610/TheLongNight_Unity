using System;
using TLN.Application.GameStates;
using UnityEngine;
using VContainer.Unity;

namespace TLN.Application.Time
{
	public sealed class GameStatePauseController : IInitializable, IDisposable
	{
		private readonly IGameStateMachine _gameStateMachine;
		private readonly IGameTimeScaleService _timeScaleService;

		public GameStatePauseController(IGameStateMachine gameStateMachine, IGameTimeScaleService timeScaleService)
		{
			_gameStateMachine = gameStateMachine;
			_timeScaleService = timeScaleService;
		}

		public void Initialize()
		{
			_gameStateMachine.StateChanged += OnGameStateChanged;

			if (_gameStateMachine.CurrentState != GameStateId.None)
			{
				ApplyState(_gameStateMachine.CurrentState);
			}
		}

		public void Dispose()
		{
			_gameStateMachine.StateChanged -= OnGameStateChanged;

			_timeScaleService.SetNormal();
			AudioListener.pause = false;
		}

		private void OnGameStateChanged(GameStateId _, GameStateId nextState)
		{
			ApplyState(nextState);
		}

		private void ApplyState(GameStateId state)
		{
			bool shouldStopSimulation = state is GameStateId.Paused or GameStateId.Loading;

			if (shouldStopSimulation)
			{
				_timeScaleService.SetPaused();
			}
			else
			{
				_timeScaleService.SetNormal();
			}

			AudioListener.pause = state == GameStateId.Paused;
		}
	}
}
