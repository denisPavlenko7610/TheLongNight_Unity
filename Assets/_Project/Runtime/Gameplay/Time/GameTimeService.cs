using System;

namespace TLN.Gameplay.Time
{
	public sealed class GameTimeService : IGameTimeService
	{
		private readonly GameTimeConfig _config;
		private float _accumulatedGameMinutes;

		public int TotalMinutes { get; private set; }
		public float TotalMinutesExact => TotalMinutes + _accumulatedGameMinutes;
		public GameTime CurrentTime => GameTime.FromTotalMinutes(TotalMinutes);

		public event Action Changed;

		public GameTimeService(GameTimeConfig config)
		{
			_config = config ?? throw new ArgumentNullException(nameof(config));

			int startDay = _config.StartDay;
			int startHour = _config.StartHour;
			int startMinute = _config.StartMinute;

			if (_config.RandomStartTime)
			{
				int minRandomHour = Math.Clamp(_config.MinRandomHour, 0, GameTime.HoursPerDay - 1);
				int maxRandomHour = Math.Clamp(_config.MaxRandomHour, minRandomHour, GameTime.HoursPerDay - 1);

				startHour = UnityEngine.Random.Range(minRandomHour, maxRandomHour + 1);
				startMinute = UnityEngine.Random.Range(0, GameTime.MinutesPerHour);
			}

			TotalMinutes = GameTime.ToTotalMinutes(startDay, startHour, startMinute);
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

	}
}
