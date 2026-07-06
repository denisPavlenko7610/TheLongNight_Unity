using TLN.Application.GameStates;
using TLN.Application.Multiplayer;
using TLN.Gameplay.Player;
using UnityEngine;
using VContainer;

namespace TLN.Gameplay.Survival
{
	public sealed class WorldSurvivalController : MonoBehaviour
	{
		private ISurvivalService _survivalService;
		private IGameStateMachine _gameStateMachine;
		private IMultiplayerSessionService _multiplayerSessionService;
		private LocalPlayerService _localPlayerService;
		private SurvivalWarningService _warningService;
		private SurvivalConfig _survivalConfig;

		private float _survivalTickAccumulator;
		private float _nextWarningTickTime;

		[Inject]
		public void Construct(
			ISurvivalService survivalService,
			SurvivalWarningService survivalWarningService,
			IGameStateMachine gameStateMachine,
			IMultiplayerSessionService multiplayerSessionService,
			LocalPlayerService localPlayerService,
			SurvivalConfig survivalConfig
		)
		{
			_survivalService = survivalService;
			_gameStateMachine = gameStateMachine;
			_multiplayerSessionService = multiplayerSessionService;
			_localPlayerService = localPlayerService;
			_warningService = survivalWarningService;
			_survivalConfig = survivalConfig;
		}

		private void Update()
		{
			if (_gameStateMachine != null &&
			    _gameStateMachine.CurrentState != GameStateId.Playing)
			{
				return;
			}

			if (ShouldSimulateOfflineSurvival())
			{
				TickSurvival();
			}

			TickWarnings();
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

			_warningService?.Tick(GetWarningSurvivalService(), unscaledTime);
			_nextWarningTickTime = unscaledTime + _survivalConfig.WarningTickIntervalSeconds;
		}

		private bool ShouldSimulateOfflineSurvival()
		{
			return _multiplayerSessionService is not { IsMultiplayer: true };
		}

		private ISurvivalService GetWarningSurvivalService()
		{
			if (_multiplayerSessionService is { IsMultiplayer: true })
			{
				return _localPlayerService?.SurvivalService;
			}

			return _survivalService;
		}
	}
}
