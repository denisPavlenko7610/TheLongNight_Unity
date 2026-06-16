using UnityEngine;

namespace TLN.Gameplay.DayNight
{
	[CreateAssetMenu(fileName = "DayNightConfig", menuName = "TLN/DayNight Config")]
	public sealed class DayNightConfig : ScriptableObject
	{
		[Header("Sun Arc")]
		[SerializeField] private float _sunriseHour = 6f;
		[SerializeField] private float _sunsetHour = 20f;
		[SerializeField] private AnimationCurve _sunElevationCurve = AnimationCurve.EaseInOut(0f, -10f, 1f, 90f);
		[SerializeField] private float _northAngle = 0f;

		[Header("Dawn (4:00-7:00)")]
		[SerializeField] private PhaseSettings _dawn = new PhaseSettings
		{
			StartHour = 4f,
			SunColor = new Color(1f, 0.75f, 0.55f),
			SunIntensity = 8000f,
			SunShadowStrength = 0.5f,
			SkyTint = new Color(0.55f, 0.55f, 0.7f),
			GroundTint = new Color(0.7f, 0.7f, 0.8f),
			FogColor = new Color(0.8f, 0.75f, 0.7f),
			FogDensity = 0.00012f,
			Exposure = 0f,
			ColorFilter = Color.white,
			TemperatureModifier = -1.5f,
			StarVisibility = 0.3f
		};

		[Header("Morning (7:00-11:00)")]
		[SerializeField] private PhaseSettings _morning = new PhaseSettings
		{
			StartHour = 7f,
			SunColor = new Color(1f, 0.92f, 0.85f),
			SunIntensity = 60000f,
			SunShadowStrength = 0.75f,
			SkyTint = new Color(0.5f, 0.55f, 0.75f),
			GroundTint = new Color(0.92f, 0.92f, 0.95f),
			FogColor = new Color(0.65f, 0.68f, 0.78f),
			FogDensity = 0.00006f,
			Exposure = 0f,
			ColorFilter = Color.white,
			TemperatureModifier = -0.5f,
			StarVisibility = 0f
		};

		[Header("Day (11:00-16:00)")]
		[SerializeField] private PhaseSettings _day = new PhaseSettings
		{
			StartHour = 11f,
			SunColor = new Color(0.95f, 0.95f, 1f),
			SunIntensity = 130000f,
			SunShadowStrength = 0.7f,
			SkyTint = new Color(0.45f, 0.5f, 0.7f),
			GroundTint = new Color(0.95f, 0.95f, 1f),
			FogColor = new Color(0.6f, 0.63f, 0.75f),
			FogDensity = 0.00004f,
			Exposure = 0f,
			ColorFilter = Color.white,
			TemperatureModifier = 0f,
			StarVisibility = 0f
		};

		[Header("Afternoon (16:00-19:00)")]
		[SerializeField] private PhaseSettings _afternoon = new PhaseSettings
		{
			StartHour = 16f,
			SunColor = new Color(1f, 0.88f, 0.8f),
			SunIntensity = 60000f,
			SunShadowStrength = 0.75f,
			SkyTint = new Color(0.5f, 0.55f, 0.75f),
			GroundTint = new Color(0.9f, 0.9f, 0.95f),
			FogColor = new Color(0.65f, 0.65f, 0.72f),
			FogDensity = 0.00006f,
			Exposure = 0f,
			ColorFilter = Color.white,
			TemperatureModifier = -0.3f,
			StarVisibility = 0f
		};

		[Header("Dusk (19:00-22:00)")]
		[SerializeField] private PhaseSettings _dusk = new PhaseSettings
		{
			StartHour = 19f,
			SunColor = new Color(1f, 0.65f, 0.4f),
			SunIntensity = 8000f,
			SunShadowStrength = 0.3f,
			SkyTint = new Color(0.55f, 0.45f, 0.55f),
			GroundTint = new Color(0.6f, 0.55f, 0.65f),
			FogColor = new Color(0.65f, 0.55f, 0.6f),
			FogDensity = 0.00015f,
			Exposure = 0f,
			ColorFilter = Color.white,
			TemperatureModifier = -2f,
			StarVisibility = 0.7f
		};

		[Header("Night (22:00-4:00)")]
		[SerializeField] private PhaseSettings _night = new PhaseSettings
		{
			StartHour = 22f,
			SunColor = new Color(0.2f, 0.22f, 0.5f),
			SunIntensity = 300f,
			SunShadowStrength = 0.05f,
			SkyTint = new Color(0.06f, 0.08f, 0.2f),
			GroundTint = new Color(0.15f, 0.17f, 0.3f),
			FogColor = new Color(0.08f, 0.1f, 0.25f),
			FogDensity = 0.0002f,
			Exposure = 1.5f,
			ColorFilter = Color.white,
			TemperatureModifier = -3.5f,
			StarVisibility = 1f
		};

		public float SunriseHour => _sunriseHour;
		public float SunsetHour => _sunsetHour;
		public AnimationCurve SunElevationCurve => _sunElevationCurve;
		public float NorthAngle => _northAngle;

		public PhaseSettings Dawn => _dawn;
		public PhaseSettings Morning => _morning;
		public PhaseSettings Day => _day;
		public PhaseSettings Afternoon => _afternoon;
		public PhaseSettings Dusk => _dusk;
		public PhaseSettings Night => _night;

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
			float h = Mathf.Repeat(hour, 24f);

			if (h >= _dawn.StartHour && h < _morning.StartHour)
				return DayNightPhase.Dawn;
			if (h >= _morning.StartHour && h < _day.StartHour)
				return DayNightPhase.Morning;
			if (h >= _day.StartHour && h < _afternoon.StartHour)
				return DayNightPhase.Day;
			if (h >= _afternoon.StartHour && h < _dusk.StartHour)
				return DayNightPhase.Afternoon;
			if (h >= _dusk.StartHour && h < _night.StartHour)
				return DayNightPhase.Dusk;

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
				DayNightPhase.Night => _dawn.StartHour + 24f,
				_ => 24f
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

		[Range(-5f, 5f)]
		public float Exposure = 0f;

		[ColorUsage(true, true)]
		public Color ColorFilter = Color.white;

		[Tooltip("Per-game-hour temperature shift (negative = colder)")]
		public float TemperatureModifier;

		[Range(0f, 1f)]
		public float StarVisibility;
	}
}
