using System;

namespace TLN.Gameplay.DayNight
{
	public interface IDayNightService
	{
		DayNightPhase CurrentPhase { get; }
		DayNightPhase PreviousPhase { get; }

		float DayProgress01 { get; }

		float PhaseProgress01 { get; }
		float PhaseBlend01 { get; }

		float SunAzimuth { get; }
		float SunElevation { get; }

		float StarVisibility { get; }
		float TemperatureModifier { get; }

		event Action PhaseChanged;

		void Refresh();
	}
}