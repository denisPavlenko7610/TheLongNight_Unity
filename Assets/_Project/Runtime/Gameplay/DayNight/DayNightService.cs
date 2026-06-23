using System;
using TLN.Gameplay.Time;
using UnityEngine;

namespace TLN.Gameplay.DayNight
{
	public sealed class DayNightService : IDayNightService
	{
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
			float minutesInDay = Mathf.Repeat(_gameTimeService.TotalMinutesExact, 24f * 60f);
			float hour = minutesInDay / 60f;

			CurrentPhase = _config.GetPhaseForHour(hour);

			DayProgress01 = minutesInDay / (24f * 60f);

			float phaseStart = _config.GetPhaseStartHour(CurrentPhase);
			float phaseEnd = _config.GetPhaseEndHour(CurrentPhase);

			if (phaseEnd <= phaseStart)
			{
				phaseEnd += 24f;
			}

			float normalizedHour = hour;
			if (normalizedHour < phaseStart)
			{
				normalizedHour += 24f;
			}

			PhaseProgress01 = Mathf.Approximately(phaseEnd, phaseStart)
				? 0f
				: Mathf.Clamp01((normalizedHour - phaseStart) / (phaseEnd - phaseStart));
			PhaseBlend01 = CalculatePhaseBlend(PhaseProgress01);

			CalculateSunPosition(hour);

			PhaseSettings currentSettings = _config.GetPhaseSettings(CurrentPhase);
			PhaseSettings nextSettings = _config.GetPhaseSettings(_config.GetNextPhase(CurrentPhase));

			StarVisibility = Mathf.Lerp(currentSettings.StarVisibility, nextSettings.StarVisibility, PhaseBlend01);
			TemperatureModifier = Mathf.Lerp(currentSettings.TemperatureModifier, nextSettings.TemperatureModifier, PhaseBlend01);
		}

		private static float CalculatePhaseBlend(float phaseProgress)
		{
			const float transitionStart = 0.65f;
			float t = Mathf.InverseLerp(transitionStart, 1f, phaseProgress);
			return t * t * (3f - 2f * t);
		}

		private void CalculateSunPosition(float hour)
		{
			float sunriseHour = _config.SunriseHour;
			float sunsetHour = _config.SunsetHour;

			float dayLengthHours = sunsetHour - sunriseHour;

			if (hour >= sunriseHour && hour <= sunsetHour)
			{
				float dayProgress = (hour - sunriseHour) / dayLengthHours;
				SunAzimuth = Mathf.Lerp(0f, 180f, dayProgress) + _config.NorthAngle;
				SunElevation = _config.SunElevationCurve.Evaluate(dayProgress);
			}
			else
			{
				float nightHour = hour < sunriseHour ? hour + 24f : hour;
				float nightDuration = 24f - dayLengthHours;

				float nightProgress = (nightHour - sunsetHour) / nightDuration;
				SunAzimuth = Mathf.Lerp(180f, 360f, nightProgress) + _config.NorthAngle;
				SunElevation = -2f - Mathf.Sin(nightProgress * Mathf.PI) * 33f;
			}
		}
	}
}
