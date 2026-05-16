using TLN.Application.GameStates;
using TLN.Core.GameStates;
using UnityEngine;

namespace TLN.Gameplay.Time
{
	public sealed class WorldTimeController : MonoBehaviour
	{
		private IGameTimeService _gameTimeService;
		private IGameStateMachine _gameStateMachine;

		public void Construct(IGameTimeService gameTimeService, IGameStateMachine gameStateMachine)
		{
			_gameTimeService = gameTimeService;
			_gameStateMachine = gameStateMachine;
		}

		private void Update()
		{
			if (_gameTimeService == null)
			{
				return;
			}

			if (_gameStateMachine != null &&
				_gameStateMachine.CurrentState != GameStateId.Playing)
			{
				return;
			}

			_gameTimeService.Tick(UnityEngine.Time.deltaTime);
		}
	}
}
