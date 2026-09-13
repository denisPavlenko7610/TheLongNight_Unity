using System;
using TLN.Gameplay.Time;
using UnityEngine;

namespace TLN.Gameplay.Weather
{
	public sealed class WeatherService : IWeatherService, IDisposable
	{
		private const float BaseWindIntensity = 0.4f;
		private const float ClearWindIntensity = 0.15f;

		private readonly WeatherConfig _config;
		private readonly IGameTimeService _gameTimeService;
		private readonly System.Random _random = new();

		private WeatherStateId _currentState = WeatherStateId.Clear;
		private WeatherStateId _previousState = WeatherStateId.Clear;
		private float _remainingStateMinutes;
		private float _stateBlend = 1f;

		private float _lastTotalMinutesExact;
		private float _snowIntensity;
		private float _windIntensity;
		private float _coldRateMultiplier;

		public WeatherStateId CurrentState => _currentState;
		public float ColdRateMultiplier => _coldRateMultiplier;
		public float SnowIntensity => _snowIntensity;
		public float WindIntensity => _windIntensity;

		public event Action<WeatherStateId> WeatherChanged;

		public WeatherService(WeatherConfig config, IGameTimeService gameTimeService)
		{
			_config = config ?? throw new ArgumentNullException(nameof(config));
			_gameTimeService = gameTimeService ?? throw new ArgumentNullException(nameof(gameTimeService));

			_gameTimeService.Changed += OnGameTimeChanged;
			ApplyStateInstant(WeatherStateId.Clear);
		}

		public void Dispose()
		{
			_gameTimeService.Changed -= OnGameTimeChanged;
		}

		private void OnGameTimeChanged()
		{
			float totalMinutesExact = _gameTimeService.TotalMinutesExact;
			float deltaMinutes = totalMinutesExact - _lastTotalMinutesExact;
			_lastTotalMinutesExact = totalMinutesExact;

			if (deltaMinutes <= 0f)
			{
				return;
			}

			Advance(deltaMinutes);
		}

		private void Advance(float deltaMinutes)
		{
			_remainingStateMinutes -= deltaMinutes;

			while (_remainingStateMinutes <= 0f)
			{
				PickNextState();
			}

			UpdateBlendedValues();
		}

		private void PickNextState()
		{
			WeatherStateId next = SelectNextState();
			ApplyStateInstant(next);
		}

		private WeatherStateId SelectNextState()
		{
			float clearWeight = _config.ClearWeight;
			float snowfallWeight = _config.SnowfallWeight;
			float blizzardWeight = _config.BlizzardWeight;

			// Avoid repeating the same weather twice in a row when possible.
			if (_currentState == WeatherStateId.Clear)
			{
				clearWeight = 0f;
			}
			else if (_currentState == WeatherStateId.Snowfall)
			{
				snowfallWeight *= 0.35f;
			}

			float totalWeight = clearWeight + snowfallWeight + blizzardWeight;

			if (totalWeight <= 0f)
			{
				return WeatherStateId.Clear;
			}

			float roll = (float)_random.NextDouble() * totalWeight;

			if (roll < clearWeight)
			{
				return WeatherStateId.Clear;
			}

			if (roll < clearWeight + snowfallWeight)
			{
				return WeatherStateId.Snowfall;
			}

			return WeatherStateId.Blizzard;
		}

		private void ApplyStateInstant(WeatherStateId state)
		{
			_previousState = _currentState == state ? _previousState : _currentState;
			_currentState = state;
			_remainingStateMinutes += GetStateDurationMinutes(state);
			_stateBlend = 0f;

			_snowIntensity = GetStateSnowIntensity(state);
			_windIntensity = GetStateWindIntensity(state);

			WeatherChanged?.Invoke(state);
		}

		private float GetStateDurationMinutes(WeatherStateId state)
		{
			return state switch
			{
				WeatherStateId.Snowfall => RandomRange(_config.SnowfallGameHoursMin, _config.SnowfallGameHoursMax) * 60f,
				WeatherStateId.Blizzard => RandomRange(_config.BlizzardGameHoursMin, _config.BlizzardGameHoursMax) * 60f,
				_ => RandomRange(_config.ClearGameHoursMin, _config.ClearGameHoursMax) * 60f
			};
		}

		private float RandomRange(float min, float max)
		{
			return min + (float)_random.NextDouble() * (max - min);
		}

		private void UpdateBlendedValues()
		{
			if (_stateBlend < 1f)
			{
				_stateBlend = Math.Min(1f, _stateBlend + _config.IntensityLerpPerGameMinute);
			}

			_coldRateMultiplier = Mathf.Lerp(
				GetStateColdMultiplier(_previousState),
				GetStateColdMultiplier(_currentState),
				_stateBlend
			);
		}

		private float GetStateSnowIntensity(WeatherStateId state)
		{
			return state switch
			{
				WeatherStateId.Snowfall => _config.SnowfallIntensity,
				WeatherStateId.Blizzard => _config.BlizzardIntensity,
				_ => 0f
			};
		}

		private float GetStateWindIntensity(WeatherStateId state)
		{
			return state switch
			{
				WeatherStateId.Snowfall => Mathf.Clamp01(BaseWindIntensity * _config.SnowfallWindMultiplier),
				WeatherStateId.Blizzard => Mathf.Clamp01(BaseWindIntensity * _config.BlizzardWindMultiplier),
				_ => ClearWindIntensity
			};
		}

		private float GetStateColdMultiplier(WeatherStateId state)
		{
			return state switch
			{
				WeatherStateId.Snowfall => _config.SnowfallColdMultiplier,
				WeatherStateId.Blizzard => _config.BlizzardColdMultiplier,
				_ => 1f
			};
		}
	}
}
