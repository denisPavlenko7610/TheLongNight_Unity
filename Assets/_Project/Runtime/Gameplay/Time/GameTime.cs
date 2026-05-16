using System;

namespace TLN.Gameplay.Time
{
	[Serializable]
	public readonly struct GameTime
	{
		public int Day { get; }
		public int Hour { get; }
		public int Minute { get; }

		public GameTime(int day, int hour, int minute)
		{
			Day = Math.Max(1, day);
			Hour = Math.Clamp(hour, 0, 23);
			Minute = Math.Clamp(minute, 0, 59);
		}

		public override string ToString()
		{
			return $"Day {Day}, {Hour:00}:{Minute:00}";
		}
	}
}
