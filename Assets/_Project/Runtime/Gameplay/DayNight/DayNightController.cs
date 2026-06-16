using TLN.Application.GameStates;
using TLN.Core.GameStates;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using VContainer;

namespace TLN.Gameplay.DayNight
{
	public sealed class DayNightController : MonoBehaviour
	{
		[SerializeField] private Light _sunLight;
		[SerializeField] private Volume _skyVolume;
		[SerializeField] private StarFieldGenerator _starFieldGenerator;

		private IDayNightService _dayNightService;
		private DayNightConfig _config;
		private IGameStateMachine _gameStateMachine;

		private PhysicallyBasedSky _physicallyBasedSky;
		private Fog _fog;
		private Exposure _exposure;
		private ColorAdjustments _colorAdjustments;

		private HDAdditionalLightData _sunLightData;
		private bool _isInitialized;
		private bool _starFieldApplied;

		[Inject]
		public void Construct(IDayNightService dayNightService, DayNightConfig config, IGameStateMachine gameStateMachine)
		{
			_dayNightService = dayNightService;
			_config = config;
			_gameStateMachine = gameStateMachine;
		}

		private void Start()
		{
			Initialize();
		}

		private void Initialize()
		{
			if (_isInitialized)
				return;

			if (_sunLight != null)
			{
				_sunLightData = _sunLight.GetComponent<HDAdditionalLightData>();
			}

			if (_skyVolume != null && _skyVolume.profile != null)
			{
				VolumeProfile profile = _skyVolume.profile;

				if (profile.TryGet(out PhysicallyBasedSky sky))
				{
					_physicallyBasedSky = sky;
					if (!_starFieldApplied && _starFieldGenerator != null)
					{
						_starFieldGenerator.ApplyToSky(_physicallyBasedSky);
						_starFieldApplied = true;
					}
				}

				profile.TryGet(out _fog);
				profile.TryGet(out _exposure);
				profile.TryGet(out _colorAdjustments);
			}

			_isInitialized = true;
		}

		private void Update()
		{
			if (!_isInitialized)
				return;

			if (_gameStateMachine != null && _gameStateMachine.CurrentState != GameStateId.Playing)
				return;

			if (_dayNightService == null)
				return;

			ApplyDayNightSettings();
		}

		private void ApplyDayNightSettings()
		{
			PhaseSettings currentPhase = GetCurrentPhaseSettings();
			PhaseSettings nextPhase = GetNextPhaseSettings();
			float t = _dayNightService.PhaseProgress01;

			Color sunColor = Color.Lerp(currentPhase.SunColor, nextPhase.SunColor, t);
			float sunIntensity = Mathf.Lerp(currentPhase.SunIntensity, nextPhase.SunIntensity, t);
			float shadowStrength = Mathf.Lerp(currentPhase.SunShadowStrength, nextPhase.SunShadowStrength, t);
			Color skyTint = Color.Lerp(currentPhase.SkyTint, nextPhase.SkyTint, t);
			Color groundTint = Color.Lerp(currentPhase.GroundTint, nextPhase.GroundTint, t);
			Color fogColor = Color.Lerp(currentPhase.FogColor, nextPhase.FogColor, t);
			float fogDensity = Mathf.Lerp(currentPhase.FogDensity, nextPhase.FogDensity, t);
			float exposureValue = Mathf.Lerp(currentPhase.Exposure, nextPhase.Exposure, t);
			Color colorFilter = Color.Lerp(currentPhase.ColorFilter, nextPhase.ColorFilter, t);

			ApplySun(sunColor, sunIntensity, shadowStrength);
			ApplySky(skyTint, groundTint);
			ApplyFog(fogColor, fogDensity);
			ApplyExposure(exposureValue);
			ApplyColorFilter(colorFilter);
		}

		private void ApplySun(Color color, float intensity, float shadowStrength)
		{
			if (_sunLight == null)
				return;

			_sunLight.color = color;
			_sunLight.intensity = intensity;
			_sunLight.shadowStrength = shadowStrength;

			Quaternion rotation = Quaternion.Euler(
				_dayNightService.SunElevation,
				_dayNightService.SunAzimuth,
				0f);

			_sunLight.transform.rotation = rotation;

			if (_sunLightData != null)
			{
				_sunLightData.EnableShadows(shadowStrength > 0.01f);
			}
		}

		private void ApplySky(Color skyTint, Color groundTint)
		{
			if (_physicallyBasedSky == null)
				return;

			_physicallyBasedSky.zenithTint.value = skyTint;
			_physicallyBasedSky.groundTint.value = groundTint;

			float starVisibility = _dayNightService.StarVisibility;
			_physicallyBasedSky.spaceEmissionMultiplier.value = Mathf.Lerp(0f, 3f, starVisibility);
		}

		private void ApplyFog(Color color, float density)
		{
			if (_fog == null)
				return;

			_fog.color.value = color;
			_fog.meanFreePath.value = density > 0.000001f ? 1f / density : 10000f;
			_fog.maximumHeight.value = Mathf.Lerp(200f, 50f, density * 4000f);
			_fog.enabled.value = density > 0.000001f;
		}

		private void ApplyExposure(float exposureValue)
		{
			if (_exposure == null)
				return;

			_exposure.mode.value = ExposureMode.Fixed;
			_exposure.fixedExposure.value = 8f - exposureValue;
		}

		private void ApplyColorFilter(Color colorFilter)
		{
			if (_colorAdjustments == null)
				return;

			if (colorFilter == Color.clear)
				colorFilter = Color.white;

			_colorAdjustments.colorFilter.value = colorFilter;
		}

		private PhaseSettings GetCurrentPhaseSettings()
		{
			if (_dayNightService == null || _config == null)
				return _config.Day;

			return _config.GetPhaseSettings(_dayNightService.CurrentPhase);
		}

		private PhaseSettings GetNextPhaseSettings()
		{
			if (_dayNightService == null || _config == null)
				return _config.Day;

			DayNightPhase nextPhase = _config.GetNextPhase(_dayNightService.CurrentPhase);
			return _config.GetPhaseSettings(nextPhase);
		}
	}
}
