using UnityEngine;

namespace TLN.Gameplay.Weather
{
	[CreateAssetMenu(fileName = "WeatherConfig", menuName = "TLN/World/Weather Config")]
	public sealed class WeatherConfig : ScriptableObject
	{
		[Header("Clear")]
		[SerializeField] private float _clearGameHoursMin = 2f;
		[SerializeField] private float _clearGameHoursMax = 6f;
		[SerializeField, Min(0f)] private float _clearWeight = 5f;

		[Header("Snowfall")]
		[SerializeField] private float _snowfallGameHoursMin = 1f;
		[SerializeField] private float _snowfallGameHoursMax = 4f;
		[SerializeField, Min(0f)] private float _snowfallWeight = 3f;
		[SerializeField, Range(0f, 1f)] private float _snowfallIntensity = 0.5f;
		[SerializeField, Min(0f)] private float _snowfallWindMultiplier = 1f;

		[Header("Blizzard")]
		[SerializeField] private float _blizzardGameHoursMin = 0.5f;
		[SerializeField] private float _blizzardGameHoursMax = 2f;
		[SerializeField, Min(0f)] private float _blizzardWeight = 1f;
		[SerializeField, Range(0f, 1f)] private float _blizzardIntensity = 1f;
		[SerializeField, Min(0f)] private float _blizzardWindMultiplier = 2.5f;

		[Header("Effects")]
		[SerializeField, Min(0f)] private float _snowfallColdMultiplier = 1.25f;
		[SerializeField, Min(0f)] private float _blizzardColdMultiplier = 2f;
		[SerializeField, Min(0f)] private float _intensityLerpPerGameMinute = 0.15f;

		public float ClearGameHoursMin => Mathf.Max(0.25f, _clearGameHoursMin);
		public float ClearGameHoursMax => Mathf.Max(ClearGameHoursMin, _clearGameHoursMax);
		public float ClearWeight => Mathf.Max(0f, _clearWeight);

		public float SnowfallGameHoursMin => Mathf.Max(0.25f, _snowfallGameHoursMin);
		public float SnowfallGameHoursMax => Mathf.Max(SnowfallGameHoursMin, _snowfallGameHoursMax);
		public float SnowfallWeight => Mathf.Max(0f, _snowfallWeight);
		public float SnowfallIntensity => Mathf.Clamp01(_snowfallIntensity);
		public float SnowfallWindMultiplier => Mathf.Max(0f, _snowfallWindMultiplier);

		public float BlizzardGameHoursMin => Mathf.Max(0.25f, _blizzardGameHoursMin);
		public float BlizzardGameHoursMax => Mathf.Max(BlizzardGameHoursMin, _blizzardGameHoursMax);
		public float BlizzardWeight => Mathf.Max(0f, _blizzardWeight);
		public float BlizzardIntensity => Mathf.Clamp01(_blizzardIntensity);
		public float BlizzardWindMultiplier => Mathf.Max(0f, _blizzardWindMultiplier);

		public float SnowfallColdMultiplier => Mathf.Max(0f, _snowfallColdMultiplier);
		public float BlizzardColdMultiplier => Mathf.Max(0f, _blizzardColdMultiplier);
		public float IntensityLerpPerGameMinute => Mathf.Max(0.01f, _intensityLerpPerGameMinute);
	}
}
