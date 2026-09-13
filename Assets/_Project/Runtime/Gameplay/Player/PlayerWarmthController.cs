using TLN.Application.GameStates;
using TLN.Application.Multiplayer;
using TLN.Gameplay.Campfire;
using TLN.Gameplay.DayNight;
using TLN.Gameplay.Equipment;
using TLN.Gameplay.Survival;
using TLN.Gameplay.Weather;
using UnityEngine;
using VContainer;

namespace TLN.Gameplay.Player
{
	public sealed class PlayerWarmthController : MonoBehaviour
	{
		private const float SecondsPerMinute = 60f;
		private const float WarmthRefreshInterval = 0.5f;
		private const float ColdExposureTickInterval = 0.25f;

		private IWarmthService _warmthService;
		private IPlayerEquipmentService _equipmentService;
		private ISurvivalService _survivalService;
		private SurvivalConfig _survivalConfig;
		private IGameStateMachine _gameStateMachine;
		private IMultiplayerSessionService _multiplayerSessionService;
		private IDayNightService _dayNightService;
		private IWeatherService _weatherService;

		private float _nextWarmthRefreshTime;
		private Vector3 _lastWarmthPosition;
		private float _cachedFireWarmth;
		private float _coldExposureAccumulator;

		[Inject]
		public void Construct(
			IWarmthService warmthService,
			IPlayerEquipmentService equipmentService,
			ISurvivalService survivalService,
			SurvivalConfig survivalConfig,
			IGameStateMachine gameStateMachine,
			IMultiplayerSessionService multiplayerSessionService,
			IDayNightService dayNightService,
			IWeatherService weatherService
		)
		{
			_warmthService = warmthService;
			_equipmentService = equipmentService;
			_survivalService = survivalService;
			_survivalConfig = survivalConfig;
			_gameStateMachine = gameStateMachine;
			_multiplayerSessionService = multiplayerSessionService;
			_dayNightService = dayNightService;
			_weatherService = weatherService;
		}

		private void Update()
		{
			if (!ShouldSimulateOfflineWarmth())
			{
				return;
			}

			if (_survivalService == null)
			{
				return;
			}

			if (_survivalConfig == null)
			{
				return;
			}

			if (_gameStateMachine != null &&
			    !_gameStateMachine.IsCurrent(GameStateId.Playing))
			{
				return;
			}

			_coldExposureAccumulator += UnityEngine.Time.deltaTime;

			if (_coldExposureAccumulator < ColdExposureTickInterval)
			{
				return;
			}

			ApplyColdExposure(_coldExposureAccumulator);
			_coldExposureAccumulator = 0f;
		}

		private bool ShouldSimulateOfflineWarmth()
		{
			return _multiplayerSessionService is not { IsMultiplayer: true };
		}

		private void ApplyColdExposure(float deltaTime)
		{
			if (deltaTime <= 0f)
			{
				return;
			}

			float gameHours = ConvertRealDeltaTimeToGameHours(deltaTime);
			float coldChangePerGameHour = CalculateColdChangePerGameHour();

			if (coldChangePerGameHour > 0f)
			{
				_survivalService.AddCold(coldChangePerGameHour * gameHours);
				return;
			}

			if (coldChangePerGameHour < 0f)
			{
				_survivalService.ReduceCold(-coldChangePerGameHour * gameHours);
			}
		}

		private float CalculateColdChangePerGameHour()
		{
			float baseColdPerGameHour = Mathf.Max(0f, _survivalConfig.ColdPerHour);
			float temperatureModifier = _dayNightService?.TemperatureModifier ?? 0f;
			float weatherMultiplier = _weatherService?.ColdRateMultiplier ?? 1f;

			float environmentColdPerGameHour =
				(baseColdPerGameHour - temperatureModifier) * weatherMultiplier;

			float fireWarmthPerGameHour = GetFireWarmthPerGameHour();
			float clothingWarmthPerGameHour = GetClothingWarmthPerGameHour();

			return environmentColdPerGameHour - fireWarmthPerGameHour - clothingWarmthPerGameHour;
		}

		private float GetFireWarmthPerGameHour()
		{
			Vector3 currentPos = transform.position;
			float sqrDist = (currentPos - _lastWarmthPosition).sqrMagnitude;

			if (UnityEngine.Time.time >= _nextWarmthRefreshTime || sqrDist > 1f)
			{
				_nextWarmthRefreshTime = UnityEngine.Time.time + WarmthRefreshInterval;
				_lastWarmthPosition = currentPos;
				_cachedFireWarmth = _warmthService?.GetWarmthAt(currentPos) ?? 0f;
			}

			return _cachedFireWarmth;
		}

		private float GetClothingWarmthPerGameHour()
		{
			return _equipmentService?.WarmthBonus ?? 0f;
		}

		private float ConvertRealDeltaTimeToGameHours(float deltaTime)
		{
			float realMinutes = deltaTime / SecondsPerMinute;
			return realMinutes * _survivalConfig.GameHoursPerRealMinute;
		}
	}
}
