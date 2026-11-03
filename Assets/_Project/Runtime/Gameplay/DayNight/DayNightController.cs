using TLN.Application.GameStates;
using TLN.Gameplay.Weather;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using VContainer;

namespace TLN.Gameplay.DayNight
{
	public sealed class DayNightController : MonoBehaviour
	{
		private const float ShadowThreshold = 0.01f;
		private const float StormSunScale = 0.4f;
		private const float StormShadowScale = 0.3f;
		private const float StormDesaturation = 0.65f;
		private const float StormFogScale = 0.18f;

		[SerializeField] private Light _sunLight;
		[SerializeField] private Volume _skyVolume;

		private IDayNightService _dayNightService;
		private DayNightConfig _config;
		private IGameStateMachine _gameStateMachine;
		private IWeatherService _weatherService;

		private Exposure _exposure;
		private ColorAdjustments _colorAdjustments;
		private PhysicallyBasedSky _physicallyBasedSky;
		private Fog _fog;

		private HDAdditionalLightData _sunLightData;
		private bool _isInitialized;
		private bool _lastShadowsEnabled;

		[Inject]
		public void Construct(
			IDayNightService dayNightService,
			DayNightConfig config,
			IGameStateMachine gameStateMachine,
			IWeatherService weatherService)
		{
			_dayNightService = dayNightService;
			_config = config;
			_gameStateMachine = gameStateMachine;
			_weatherService = weatherService;
		}

		private void Start()
		{
			Initialize();
		}

		private void Initialize()
		{
			if (_isInitialized)
			{
				return;
			}

			if (_sunLight != null)
			{
				_sunLightData = _sunLight.GetComponent<HDAdditionalLightData>();
			}

			if (_skyVolume != null && _skyVolume.profile != null)
			{
				VolumeProfile profile = _skyVolume.profile;
				profile.TryGet(out _exposure);
				profile.TryGet(out _colorAdjustments);
				profile.TryGet(out _physicallyBasedSky);
				profile.TryGet(out _fog);

				SetupVolumeOverrides();
			}

			_isInitialized = true;
		}

		private void SetupVolumeOverrides()
		{
			if (_exposure != null)
			{
				_exposure.mode.overrideState = true;
				_exposure.mode.value = ExposureMode.Fixed;
				_exposure.fixedExposure.overrideState = true;
			}

			if (_colorAdjustments != null)
			{
				_colorAdjustments.colorFilter.overrideState = true;
			}

			if (_physicallyBasedSky != null)
			{
				_physicallyBasedSky.horizonTint.overrideState = true;
				_physicallyBasedSky.zenithTint.overrideState = true;
				_physicallyBasedSky.groundTint.overrideState = true;
			}

			if (_fog != null)
			{
				_fog.enabled.overrideState = true;
				_fog.enabled.value = true;
				_fog.color.overrideState = true;
				_fog.meanFreePath.overrideState = true;
			}
		}

		private void Update()
		{
			if (!_isInitialized)
			{
				return;
			}

			if (_gameStateMachine != null && !_gameStateMachine.IsCurrent(GameStateId.Playing))
			{
				return;
			}

			if (_dayNightService == null || _config == null)
			{
				return;
			}

			_dayNightService.Refresh();
			ApplyDayNightSettings();
		}

		private void ApplyDayNightSettings()
		{
			PhaseSettings currentPhase = GetCurrentPhaseSettings();
			PhaseSettings nextPhase = GetNextPhaseSettings();
			float t = _dayNightService.PhaseBlend01;
			float storm = _weatherService != null ? Mathf.Clamp01(_weatherService.SnowIntensity) : 0f;

			ApplySun(
				Color.Lerp(currentPhase.SunColor, nextPhase.SunColor, t),
				Mathf.Lerp(currentPhase.SunIntensity, nextPhase.SunIntensity, t) * Mathf.Lerp(1f, StormSunScale, storm),
				Mathf.Lerp(currentPhase.SunShadowStrength, nextPhase.SunShadowStrength, t) * Mathf.Lerp(1f, StormShadowScale, storm)
			);

			ApplyExposure(Mathf.Lerp(currentPhase.Exposure, nextPhase.Exposure, t));
			ApplyColorFilter(Color.Lerp(currentPhase.ColorFilter, nextPhase.ColorFilter, t));
			ApplySky(
				Desaturate(Color.Lerp(currentPhase.SkyTint, nextPhase.SkyTint, t), storm * StormDesaturation),
				Desaturate(Color.Lerp(currentPhase.GroundTint, nextPhase.GroundTint, t), storm * StormDesaturation)
			);
			ApplyFog(
				Desaturate(Color.Lerp(currentPhase.FogColor, nextPhase.FogColor, t), storm * StormDesaturation),
				Mathf.Lerp(currentPhase.FogMeanFreePath, nextPhase.FogMeanFreePath, t) * Mathf.Lerp(1f, StormFogScale, storm)
			);
		}

		private static Color Desaturate(Color color, float amount)
		{
			float gray = color.grayscale;
			return Color.Lerp(color, new Color(gray, gray, gray, color.a), amount);
		}

		private void ApplySun(Color color, float intensity, float shadowStrength)
		{
			if (_sunLight == null)
			{
				return;
			}

			_sunLight.color = color;
			_sunLight.intensity = intensity;
			_sunLight.shadowStrength = shadowStrength;
			_sunLight.transform.rotation = Quaternion.Euler(
				_dayNightService.SunElevation,
				_dayNightService.SunAzimuth,
				0f
			);

			if (_sunLightData == null)
			{
				return;
			}

			bool shadowsEnabled = shadowStrength > ShadowThreshold;
			if (_lastShadowsEnabled == shadowsEnabled)
			{
				return;
			}

			_lastShadowsEnabled = shadowsEnabled;
			_sunLightData.EnableShadows(shadowsEnabled);
		}

		private void ApplyExposure(float exposureValue)
		{
			if (_exposure == null)
			{
				return;
			}

			_exposure.fixedExposure.value = exposureValue;
		}

		private void ApplyColorFilter(Color colorFilter)
		{
			if (_colorAdjustments == null)
			{
				return;
			}

			if (colorFilter == Color.clear)
			{
				colorFilter = Color.white;
			}

			_colorAdjustments.colorFilter.value = colorFilter;
		}

		private void ApplySky(Color skyTint, Color groundTint)
		{
			if (_physicallyBasedSky == null)
			{
				return;
			}

			_physicallyBasedSky.horizonTint.value = skyTint;
			_physicallyBasedSky.zenithTint.value = skyTint;
			_physicallyBasedSky.groundTint.value = groundTint;
		}

		private void ApplyFog(Color fogColor, float meanFreePath)
		{
			if (_fog == null)
			{
				return;
			}

			_fog.color.value = fogColor;
			_fog.meanFreePath.value = meanFreePath;
		}

		private PhaseSettings GetCurrentPhaseSettings()
		{
			return _config.GetPhaseSettings(_dayNightService.CurrentPhase);
		}

		private PhaseSettings GetNextPhaseSettings()
		{
			DayNightPhase nextPhase = _config.GetNextPhase(_dayNightService.CurrentPhase);
			return _config.GetPhaseSettings(nextPhase);
		}
	}
}
