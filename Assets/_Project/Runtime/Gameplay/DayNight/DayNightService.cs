using System;
using TLN.Gameplay.Time;
using UnityEngine;

namespace TLN.Gameplay.DayNight
{
	public sealed class DayNightService : IDayNightService, IDisposable
	{
		private const float HoursInDay = 24f;
		private const float MinutesPerHour = 60f;
		private const float MinutesInDay = HoursInDay * MinutesPerHour;

		private readonly DayNightConfig _config;
		private readonly IGameTimeService _gameTimeService;

		public DayNightPhase CurrentPhase { get; private set; }
		public DayNightPhase PreviousPhase { get; private set; }

		public float DayProgress01 { get; private set; }
		public float PhaseProgress01 { get; private set; }
		public float PhaseBlend01 { get; private set; }

		public float SunAzimuth { get; private set; }
		public float SunElevation { get; private set; }

		public float StarVisibility { get; private set; }
		public float TemperatureModifier { get; private set; }

		public event Action PhaseChanged;

		public DayNightService(DayNightConfig config, IGameTimeService gameTimeService)
		{
			_config = config ?? throw new ArgumentNullException(nameof(config));
			_gameTimeService = gameTimeService ?? throw new ArgumentNullException(nameof(gameTimeService));

			_gameTimeService.Changed += OnGameTimeChanged;

			Refresh();
		}

		public void Dispose()
		{
			_gameTimeService.Changed -= OnGameTimeChanged;
		}

		private void OnGameTimeChanged()
		{
			Refresh();
		}

		public void Refresh()
		{
			DayNightPhase previous = CurrentPhase;
			RecalculateFromCurrentTime();

			if (CurrentPhase != previous)
			{
				PreviousPhase = previous;
				PhaseChanged?.Invoke();
			}
		}

		private void RecalculateFromCurrentTime()
		{
			float minutesInDay = Mathf.Repeat(_gameTimeService.TotalMinutesExact, MinutesInDay);
			float hour = minutesInDay / MinutesPerHour;

			CurrentPhase = _config.GetPhaseForHour(hour);

			DayProgress01 = minutesInDay / MinutesInDay;

			float phaseStart = _config.GetPhaseStartHour(CurrentPhase);
			float phaseEnd = _config.GetPhaseEndHour(CurrentPhase);

			if (phaseEnd <= phaseStart)
			{
				phaseEnd += HoursInDay;
			}

			float normalizedHour = hour;
			if (normalizedHour < phaseStart)
			{
				normalizedHour += HoursInDay;
			}

			PhaseProgress01 = Mathf.Approximately(phaseEnd, phaseStart)
				? 0f
				: Mathf.Clamp01((normalizedHour - phaseStart) / (phaseEnd - phaseStart));
			PhaseBlend01 = CalculatePhaseBlend(CurrentPhase, PhaseProgress01);

			CalculateSunPosition(hour);

			PhaseSettings currentSettings = _config.GetPhaseSettings(CurrentPhase);
			PhaseSettings nextSettings = _config.GetPhaseSettings(_config.GetNextPhase(CurrentPhase));

			StarVisibility = Mathf.Lerp(currentSettings.StarVisibility, nextSettings.StarVisibility, PhaseBlend01);
			TemperatureModifier = Mathf.Lerp(currentSettings.TemperatureModifier, nextSettings.TemperatureModifier, PhaseBlend01);
		}

		private static float CalculatePhaseBlend(DayNightPhase phase, float phaseProgress)
		{
			if (phase == DayNightPhase.Night)
			{
				return 0f;
			}

			const float transitionStart = 0.65f;
			float t = Mathf.InverseLerp(transitionStart, 1f, phaseProgress);
			return t * t * (3f - 2f * t);
		}

		private void CalculateSunPosition(float hour)
		{
			const float moonElevation = 12f;
			const float moonAzimuth = 270f;
			const float hiddenElevation = -3f;
			const float azimuthDayEnd = 180f;

			float sunriseHour = _config.SunriseHour;
			float sunsetHour = _config.SunsetHour;

			float dayLengthHours = Mathf.Max(0.01f, sunsetHour - sunriseHour);

			if (CurrentPhase == DayNightPhase.Night)
			{
				SunAzimuth = moonAzimuth + _config.NorthAngle;
				SunElevation = moonElevation;
			}
			else if (hour >= sunriseHour && hour <= sunsetHour)
			{
				float dayProgress = (hour - sunriseHour) / dayLengthHours;
				SunAzimuth = Mathf.Lerp(0f, azimuthDayEnd, dayProgress) + _config.NorthAngle;
				SunElevation = _config.SunElevationCurve.Evaluate(dayProgress);
			}
			else
			{
				SunAzimuth = (hour < sunriseHour ? 0f : azimuthDayEnd) + _config.NorthAngle;
				SunElevation = hiddenElevation;
			}
		}
	}
}
