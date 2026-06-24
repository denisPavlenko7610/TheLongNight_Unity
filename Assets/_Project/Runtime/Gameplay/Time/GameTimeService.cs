using System;

namespace TLN.Gameplay.Time
{
	public sealed class GameTimeService : IGameTimeService
	{
		private readonly GameTimeConfig _config;
		private float _accumulatedGameMinutes;

		public int TotalMinutes { get; private set; }
		public float TotalMinutesExact => TotalMinutes + _accumulatedGameMinutes;
		public GameTime CurrentTime => ConvertTotalMinutesToGameTime(TotalMinutes);

		public event Action Changed;

		public GameTimeService(GameTimeConfig config)
		{
			_config = config ?? throw new ArgumentNullException(nameof(config));

			int startDay = _config.StartDay;
			int startHour = _config.StartHour;
			int startMinute = _config.StartMinute;

			if (_config.RandomStartTime)
			{
				startHour = UnityEngine.Random.Range(_config.MinRandomHour, _config.MaxRandomHour);
				startMinute = UnityEngine.Random.Range(0, GameTime.MinutesPerHour - 1);
			}

			TotalMinutes = ConvertStartTimeToTotalMinutes(startDay, startHour, startMinute);
		}

		public void Tick(float deltaTime)
		{
			if (deltaTime <= 0f)
			{
				return;
			}

			_accumulatedGameMinutes += deltaTime * _config.GameMinutesPerRealSecond;

			if (_accumulatedGameMinutes < 1f)
			{
				return;
			}

			int wholeMinutes = (int)_accumulatedGameMinutes;
			_accumulatedGameMinutes -= wholeMinutes;

			AdvanceMinutes(wholeMinutes);
		}

		public void SetTotalMinutes(int totalMinutes)
		{
			TotalMinutes = Math.Max(0, totalMinutes);
			_accumulatedGameMinutes = 0f;
			Changed?.Invoke();
		}

		public void AdvanceHours(int hours)
		{
			if (hours <= 0)
			{
				return;
			}

			AdvanceMinutes(hours * GameTime.MinutesPerHour);
		}

		public void AdvanceMinutes(int minutes)
		{
			if (minutes <= 0)
			{
				return;
			}

			TotalMinutes += minutes;
			Changed?.Invoke();
		}

		private static int ConvertStartTimeToTotalMinutes(int day, int hour, int minute)
		{
			int safeDay = Math.Max(1, day);
			int safeHour = Math.Clamp(hour, 0, GameTime.HoursPerDay - 1);
			int safeMinute = Math.Clamp(minute, 0, GameTime.MinutesPerHour - 1);

			return (safeDay - 1) * GameTime.MinutesPerDay + safeHour * GameTime.MinutesPerHour + safeMinute;
		}

		private static GameTime ConvertTotalMinutesToGameTime(int totalMinutes)
		{
			int day = totalMinutes / GameTime.MinutesPerDay + 1;
			int minutesInCurrentDay = totalMinutes % GameTime.MinutesPerDay;

			int hour = minutesInCurrentDay / GameTime.MinutesPerHour;
			int minute = minutesInCurrentDay % GameTime.MinutesPerHour;

			return new GameTime(day, hour, minute);
		}
	}
}
