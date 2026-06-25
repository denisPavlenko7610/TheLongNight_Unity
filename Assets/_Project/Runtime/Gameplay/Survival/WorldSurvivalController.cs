using TLN.Application.GameStates;
using UnityEngine;
using VContainer;

namespace TLN.Gameplay.Survival
{
	public sealed class WorldSurvivalController : MonoBehaviour
	{
		private const float SurvivalTickIntervalSeconds = 0.25f;
		private const float WarningTickIntervalSeconds = 0.5f;

		private ISurvivalService _survivalService;
		private IGameStateMachine _gameStateMachine;
		private SurvivalWarningService _warningService;
		private float _survivalTickAccumulator;
		private float _nextWarningTickTime;

		[Inject]
		public void Construct(
			ISurvivalService survivalService,
			SurvivalWarningService survivalWarningService,
			IGameStateMachine gameStateMachine
		)
		{
			_survivalService = survivalService;
			_gameStateMachine = gameStateMachine;
			_warningService = survivalWarningService;
		}

		private void Update()
		{
			if (_survivalService == null)
			{
				return;
			}

			if (_gameStateMachine != null && _gameStateMachine.CurrentState != GameStateId.Playing)
			{
				return;
			}

			_survivalTickAccumulator += UnityEngine.Time.deltaTime;

			if (_survivalTickAccumulator >= SurvivalTickIntervalSeconds)
			{
				_survivalService.Tick(_survivalTickAccumulator);
				_survivalTickAccumulator = 0f;
			}

			float unscaledTime = UnityEngine.Time.unscaledTime;

			if (unscaledTime >= _nextWarningTickTime)
			{
				_warningService?.Tick(unscaledTime);
				_nextWarningTickTime = unscaledTime + WarningTickIntervalSeconds;
			}
		}
	}
}
