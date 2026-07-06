using System;

namespace TLN.Gameplay.Time
{
	[Serializable]
	public readonly struct GameTime
	{
		public const int MinutesPerHour = 60;
		public const int HoursPerDay = 24;
		public const int MinutesPerDay = MinutesPerHour * HoursPerDay;

		public int Day { get; }
		public int Hour { get; }
		public int Minute { get; }

		public GameTime(int day, int hour, int minute)
		{
			Day = Math.Max(1, day);
			Hour = Math.Clamp(hour, 0, HoursPerDay - 1);
			Minute = Math.Clamp(minute, 0, MinutesPerHour - 1);
		}

		public static GameTime FromTotalMinutes(int totalMinutes)
		{
			int safeTotalMinutes = Math.Max(0, totalMinutes);
			int day = safeTotalMinutes / MinutesPerDay + 1;
			int minutesInCurrentDay = safeTotalMinutes % MinutesPerDay;

			return new GameTime(
				day,
				minutesInCurrentDay / MinutesPerHour,
				minutesInCurrentDay % MinutesPerHour
			);
		}

		public static int ToTotalMinutes(int day, int hour, int minute)
		{
			int safeDay = Math.Max(1, day);
			int safeHour = Math.Clamp(hour, 0, HoursPerDay - 1);
			int safeMinute = Math.Clamp(minute, 0, MinutesPerHour - 1);

			return (safeDay - 1) * MinutesPerDay + safeHour * MinutesPerHour + safeMinute;
		}

		public override string ToString()
		{
			return $"Day {Day}, {Hour:00}:{Minute:00}";
		}
	}
}
