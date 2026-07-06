using TLN.Gameplay.Time;
using UnityEngine;

namespace TLN.Gameplay.DayNight
{
	[CreateAssetMenu(fileName = "DayNightConfig", menuName = "TLN/DayNight Config")]
	public sealed class DayNightConfig : ScriptableObject
	{
		[Header("Sun Arc")]
		[SerializeField] private float _sunriseHour = 5.5f;
		[SerializeField] private float _sunsetHour = 20f;
		[SerializeField] private AnimationCurve _sunElevationCurve = AnimationCurve.EaseInOut(0f, -10f, 1f, 90f);
		[SerializeField] private float _northAngle = 0f;

		[Header("Dawn (4:00-7:00)")]
		[SerializeField] private PhaseSettings _dawn = new PhaseSettings
		{
			StartHour = 4f,
			SunColor = new Color(0.78f, 0.86f, 1f),
			SunIntensity = 2500f,
			SunShadowStrength = 0.5f,
			SkyTint = new Color(0.32f, 0.42f, 0.66f),
			GroundTint = new Color(0.58f, 0.68f, 0.82f),
			FogColor = new Color(0.5f, 0.62f, 0.78f),
			FogDensity = 0.00012f,
			Exposure = 8.2f,
			ColorFilter = new Color(0.86f, 0.93f, 1f),
			TemperatureModifier = -1.5f,
			StarVisibility = 0.45f
		};

		[Header("Morning (7:00-11:00)")]
		[SerializeField] private PhaseSettings _morning = new PhaseSettings
		{
			StartHour = 7f,
			SunColor = new Color(0.82f, 0.9f, 1f),
			SunIntensity = 5500f,
			SunShadowStrength = 0.75f,
			SkyTint = new Color(0.42f, 0.56f, 0.82f),
			GroundTint = new Color(0.78f, 0.86f, 0.96f),
			FogColor = new Color(0.55f, 0.68f, 0.86f),
			FogDensity = 0.00006f,
			Exposure = 8.1f,
			ColorFilter = new Color(0.9f, 0.96f, 1f),
			TemperatureModifier = -0.5f,
			StarVisibility = 0f
		};

		[Header("Day (11:00-16:00)")]
		[SerializeField] private PhaseSettings _day = new PhaseSettings
		{
			StartHour = 11f,
			SunColor = new Color(0.78f, 0.88f, 1f),
			SunIntensity = 8000f,
			SunShadowStrength = 0.7f,
			SkyTint = new Color(0.36f, 0.5f, 0.78f),
			GroundTint = new Color(0.82f, 0.9f, 1f),
			FogColor = new Color(0.5f, 0.64f, 0.84f),
			FogDensity = 0.00004f,
			Exposure = 8.6f,
			ColorFilter = new Color(0.9f, 0.96f, 1f),
			TemperatureModifier = 0f,
			StarVisibility = 0f
		};

		[Header("Afternoon (16:00-19:00)")]
		[SerializeField] private PhaseSettings _afternoon = new PhaseSettings
		{
			StartHour = 16f,
			SunColor = new Color(0.74f, 0.84f, 1f),
			SunIntensity = 5500f,
			SunShadowStrength = 0.75f,
			SkyTint = new Color(0.38f, 0.5f, 0.76f),
			GroundTint = new Color(0.78f, 0.86f, 0.97f),
			FogColor = new Color(0.52f, 0.62f, 0.8f),
			FogDensity = 0.00006f,
			Exposure = 8.2f,
			ColorFilter = new Color(0.88f, 0.94f, 1f),
			TemperatureModifier = -0.3f,
			StarVisibility = 0f
		};

		[Header("Dusk (19:00-22:00)")]
		[SerializeField] private PhaseSettings _dusk = new PhaseSettings
		{
			StartHour = 19f,
			SunColor = new Color(0.58f, 0.66f, 0.95f),
			SunIntensity = 2800f,
			SunShadowStrength = 0.3f,
			SkyTint = new Color(0.24f, 0.3f, 0.54f),
			GroundTint = new Color(0.36f, 0.42f, 0.62f),
			FogColor = new Color(0.28f, 0.34f, 0.56f),
			FogDensity = 0.00015f,
			Exposure = 8f,
			ColorFilter = new Color(0.78f, 0.86f, 1f),
			TemperatureModifier = -2f,
			StarVisibility = 0.45f
		};

		[Header("Night (22:00-4:00)")]
		[SerializeField] private PhaseSettings _night = new PhaseSettings
		{
			StartHour = 22f,
			SunColor = new Color(0.44f, 0.56f, 0.9f),
			SunIntensity = 75f,
			SunShadowStrength = 0f,
			SkyTint = new Color(0.065f, 0.09f, 0.19f),
			GroundTint = new Color(0.085f, 0.105f, 0.18f),
			FogColor = new Color(0.065f, 0.085f, 0.165f),
			FogDensity = 0.000035f,
			Exposure = 6.6f,
			ColorFilter = new Color(0.82f, 0.9f, 1f),
			TemperatureModifier = -3.5f,
			StarVisibility = 1f
		};

		public float SunriseHour => _sunriseHour;
		public float SunsetHour => _sunsetHour;
		public AnimationCurve SunElevationCurve => _sunElevationCurve;
		public float NorthAngle => _northAngle;

		public PhaseSettings GetPhaseSettings(DayNightPhase phase)
		{
			return phase switch
			{
				DayNightPhase.Dawn => _dawn,
				DayNightPhase.Morning => _morning,
				DayNightPhase.Day => _day,
				DayNightPhase.Afternoon => _afternoon,
				DayNightPhase.Dusk => _dusk,
				DayNightPhase.Night => _night,
				_ => _day
			};
		}

		public DayNightPhase GetPhaseForHour(float hour)
		{
			hour %= GameTime.HoursPerDay;

			if (hour >= _dawn.StartHour && hour < _morning.StartHour)
			{
				return DayNightPhase.Dawn;
			}
			if (hour >= _morning.StartHour && hour < _day.StartHour)
			{
				return DayNightPhase.Morning;
			}
			if (hour >= _day.StartHour && hour < _afternoon.StartHour)
			{
				return DayNightPhase.Day;
			}
			if (hour >= _afternoon.StartHour && hour < _dusk.StartHour)
			{
				return DayNightPhase.Afternoon;
			}
			if (hour >= _dusk.StartHour && hour < _night.StartHour)
			{
				return DayNightPhase.Dusk;
			}

			return DayNightPhase.Night;
		}

		public DayNightPhase GetNextPhase(DayNightPhase phase)
		{
			return phase switch
			{
				DayNightPhase.Dawn => DayNightPhase.Morning,
				DayNightPhase.Morning => DayNightPhase.Day,
				DayNightPhase.Day => DayNightPhase.Afternoon,
				DayNightPhase.Afternoon => DayNightPhase.Dusk,
				DayNightPhase.Dusk => DayNightPhase.Night,
				DayNightPhase.Night => DayNightPhase.Dawn,
				_ => DayNightPhase.Day
			};
		}

		public float GetPhaseEndHour(DayNightPhase phase)
		{
			return phase switch
			{
				DayNightPhase.Dawn => _morning.StartHour,
				DayNightPhase.Morning => _day.StartHour,
				DayNightPhase.Day => _afternoon.StartHour,
				DayNightPhase.Afternoon => _dusk.StartHour,
				DayNightPhase.Dusk => _night.StartHour,
				DayNightPhase.Night => _dawn.StartHour + GameTime.HoursPerDay,
				_ => GameTime.HoursPerDay
			};
		}

		public float GetPhaseStartHour(DayNightPhase phase)
		{
			return phase switch
			{
				DayNightPhase.Dawn => _dawn.StartHour,
				DayNightPhase.Morning => _morning.StartHour,
				DayNightPhase.Day => _day.StartHour,
				DayNightPhase.Afternoon => _afternoon.StartHour,
				DayNightPhase.Dusk => _dusk.StartHour,
				DayNightPhase.Night => _night.StartHour,
				_ => 0f
			};
		}
	}

	[System.Serializable]
	public sealed class PhaseSettings
	{
		[Tooltip("Hour (0-24) when this phase begins")]
		public float StartHour = 0f;

		[ColorUsage(true, true)]
		public Color SunColor = Color.white;

		public float SunIntensity = 13000f;

		[Range(0f, 1f)]
		public float SunShadowStrength = 1f;

		[ColorUsage(true, true)]
		public Color SkyTint = new Color(0.5f, 0.6f, 0.8f);

		[ColorUsage(true, true)]
		public Color GroundTint = new Color(0.95f, 0.95f, 1f);

		[ColorUsage(true, true)]
		public Color FogColor = new Color(0.6f, 0.65f, 0.7f);

		[Range(0f, 0.001f)]
		public float FogDensity = 0.0001f;

		[Tooltip("HDRP fixed camera exposure in EV100. Higher values darken the camera exposure.")]
		[Range(0f, 14f)]
		public float Exposure = 8f;

		[ColorUsage(true, true)]
		public Color ColorFilter = Color.white;

		[Tooltip("Per-game-hour temperature shift (negative = colder)")]
		public float TemperatureModifier;

		[Range(0f, 1f)]
		public float StarVisibility;
	}
}
