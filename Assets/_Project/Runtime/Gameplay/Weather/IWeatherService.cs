using System;

namespace TLN.Gameplay.Weather
{
	public interface IWeatherService
	{
		WeatherStateId CurrentState { get; }
		float ColdRateMultiplier { get; }
		float SnowIntensity { get; }
		float WindIntensity { get; }

		event Action<WeatherStateId> WeatherChanged;
	}
}
