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

		public override string ToString()
		{
			return $"Day {Day}, {Hour:00}:{Minute:00}";
		}
	}
}