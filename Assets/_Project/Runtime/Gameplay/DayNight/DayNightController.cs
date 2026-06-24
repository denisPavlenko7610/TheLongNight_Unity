using TLN.Application.GameStates;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using VContainer;

namespace TLN.Gameplay.DayNight
{
	public sealed class DayNightController : MonoBehaviour
	{
		private const float ShadowThreshold = 0.01f;

		[SerializeField] private Light _sunLight;
		[SerializeField] private Volume _skyVolume;

		private IDayNightService _dayNightService;
		private DayNightConfig _config;
		private IGameStateMachine _gameStateMachine;

		private Exposure _exposure;
		private ColorAdjustments _colorAdjustments;

		private HDAdditionalLightData _sunLightData;
		private bool _isInitialized;
		private bool _lastShadowsEnabled;

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
		}

		private void Update()
		{
			if (!_isInitialized)
			{
				return;
			}

			if (_gameStateMachine != null && _gameStateMachine.CurrentState != GameStateId.Playing)
			{
				return;
			}

			if (_dayNightService == null)
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

			ApplySun(
				Color.Lerp(currentPhase.SunColor, nextPhase.SunColor, t),
				Mathf.Lerp(currentPhase.SunIntensity, nextPhase.SunIntensity, t),
				Mathf.Lerp(currentPhase.SunShadowStrength, nextPhase.SunShadowStrength, t)
			);

			ApplyExposure(Mathf.Lerp(currentPhase.Exposure, nextPhase.Exposure, t));
			ApplyColorFilter(Color.Lerp(currentPhase.ColorFilter, nextPhase.ColorFilter, t));
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

		private PhaseSettings GetCurrentPhaseSettings()
		{
			if (_dayNightService == null || _config == null)
			{
				return _config.Day;
			}

			return _config.GetPhaseSettings(_dayNightService.CurrentPhase);
		}

		private PhaseSettings GetNextPhaseSettings()
		{
			if (_dayNightService == null || _config == null)
			{
				return _config.Day;
			}

			DayNightPhase nextPhase = _config.GetNextPhase(_dayNightService.CurrentPhase);
			return _config.GetPhaseSettings(nextPhase);
		}
	}
}
