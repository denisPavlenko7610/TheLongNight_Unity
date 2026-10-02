using TLN.Application.GameStates;
using UnityEngine;
using VContainer;

namespace TLN.Gameplay.Survival
{
	public sealed class WorldSurvivalController : MonoBehaviour
	{
		private ISurvivalService _survivalService;
		private IGameStateMachine _gameStateMachine;
		private SurvivalWarningService _warningService;
		private SurvivalConfig _survivalConfig;

		private float _survivalTickAccumulator;
		private float _nextWarningTickTime;

		[Inject]
		public void Construct(
			ISurvivalService survivalService,
			SurvivalWarningService survivalWarningService,
			IGameStateMachine gameStateMachine,
			SurvivalConfig survivalConfig
		)
		{
			_survivalService = survivalService;
			_gameStateMachine = gameStateMachine;
			_warningService = survivalWarningService;
			_survivalConfig = survivalConfig;
		}

		private void Update()
		{
			if (_gameStateMachine != null &&
			    !_gameStateMachine.IsCurrent(GameStateId.Playing))
			{
				return;
			}

			TickSurvival();
			TickWarnings();
			CheckDeath();
		}

		private void TickSurvival()
		{
			if (_survivalService == null)
			{
				return;
			}

			_survivalTickAccumulator += UnityEngine.Time.deltaTime;

			if (_survivalTickAccumulator < _survivalConfig.SurvivalTickIntervalSeconds)
			{
				return;
			}

			_survivalService.Tick(_survivalTickAccumulator);
			_survivalTickAccumulator = 0f;
		}

		private void TickWarnings()
		{
			float unscaledTime = UnityEngine.Time.unscaledTime;

			if (unscaledTime < _nextWarningTickTime)
			{
				return;
			}

			_warningService?.Tick(_survivalService, unscaledTime);
			_nextWarningTickTime = unscaledTime + _survivalConfig.WarningTickIntervalSeconds;
		}

		private void CheckDeath()
		{
			if (_gameStateMachine == null ||
			    !_gameStateMachine.IsCurrent(GameStateId.Playing))
			{
				return;
			}

			if (_survivalService == null)
			{
				return;
			}

			if (_survivalService.Condition.Value <= 0f)
			{
				_gameStateMachine.Enter(GameStateId.GameOver);
			}
		}
	}
}
