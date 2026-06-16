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

			RecalculateFromCurrentTime();
		}

		private void OnGameTimeChanged()
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
			GameTime time = _gameTimeService.CurrentTime;

			float hour = time.Hour + time.Minute / 60f;
			float totalHours = hour;

			CurrentPhase = _config.GetPhaseForHour(hour);

			DayProgress01 = totalHours / 24f;

			float phaseStart = _config.GetPhaseStartHour(CurrentPhase);
			float phaseEnd = _config.GetPhaseEndHour(CurrentPhase);

			if (phaseEnd <= phaseStart)
				phaseEnd += 24f;

			float normalizedHour = hour;
			if (normalizedHour < phaseStart)
				normalizedHour += 24f;

			PhaseProgress01 = Mathf.Approximately(phaseEnd, phaseStart)
				? 0f
				: Mathf.Clamp01((normalizedHour - phaseStart) / (phaseEnd - phaseStart));

			CalculateSunPosition(hour);

			PhaseSettings currentSettings = _config.GetPhaseSettings(CurrentPhase);
			PhaseSettings nextSettings = _config.GetPhaseSettings(_config.GetNextPhase(CurrentPhase));

			StarVisibility = Mathf.Lerp(currentSettings.StarVisibility, nextSettings.StarVisibility, PhaseProgress01);
			TemperatureModifier = Mathf.Lerp(currentSettings.TemperatureModifier, nextSettings.TemperatureModifier, PhaseProgress01);
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
				SunElevation = Mathf.Sin(nightProgress * Mathf.PI) * 25f;
			}
		}
	}
}
