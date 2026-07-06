using TLN.Application.Audio;
using TLN.Application.Settings;
using UnityEngine;
using UnityEngine.Audio;

namespace TLN.Infrastructure.Audio
{
	[CreateAssetMenu(fileName = "AudioMixerConfig", menuName = "TLN/Audio/Audio Mixer Config")]
	public sealed class AudioMixerConfig : ScriptableObject
	{
		private const float MinVolumeDecibels = -80f;
		private const float UnappliedVolume = -1f;

		[SerializeField] private AudioMixer _mixer;
		[SerializeField] private AudioMixerGroup _masterGroup;
		[SerializeField] private AudioMixerGroup _musicGroup;
		[SerializeField] private AudioMixerGroup _ambientGroup;
		[SerializeField] private AudioMixerGroup _sfxGroup;

		[Header("Exposed Parameters")]
		[SerializeField] private string _masterVolumeParameter = "MasterVolume";
		[SerializeField] private string _musicVolumeParameter = "MusicVolume";
		[SerializeField] private string _ambientVolumeParameter = "AmbientVolume";
		[SerializeField] private string _sfxVolumeParameter = "SfxVolume";

		private float _lastMasterVolume = UnappliedVolume;
		private float _lastMusicVolume = UnappliedVolume;
		private float _lastAmbientVolume = UnappliedVolume;
		private float _lastSfxVolume = UnappliedVolume;

		public AudioMixerGroup GetGroup(AudioBusId busId)
		{
			return busId switch
			{
				AudioBusId.Music => _musicGroup != null ? _musicGroup : _masterGroup,
				AudioBusId.Ambient => _ambientGroup != null ? _ambientGroup : _masterGroup,
				_ => _sfxGroup != null ? _sfxGroup : _masterGroup
			};
		}

		public void Apply(GameSettings settings)
		{
			if (_mixer == null || settings == null)
			{
				return;
			}

			SetLinearVolume(_masterVolumeParameter, settings.MasterVolume, ref _lastMasterVolume);
			SetLinearVolume(_musicVolumeParameter, settings.MusicVolume, ref _lastMusicVolume);
			SetLinearVolume(_ambientVolumeParameter, settings.AmbientVolume, ref _lastAmbientVolume);
			SetLinearVolume(_sfxVolumeParameter, settings.SfxVolume, ref _lastSfxVolume);
		}

		private void SetLinearVolume(string exposedParameter, float linearVolume, ref float lastLinearVolume)
		{
			if (string.IsNullOrWhiteSpace(exposedParameter))
			{
				return;
			}

			linearVolume = Mathf.Clamp01(linearVolume);

			if (Mathf.Approximately(lastLinearVolume, linearVolume))
			{
				return;
			}

			if (_mixer.SetFloat(exposedParameter, LinearToDecibels(linearVolume)))
			{
				lastLinearVolume = linearVolume;
			}
		}

		private static float LinearToDecibels(float linearVolume)
		{
			return linearVolume <= Mathf.Epsilon
				? MinVolumeDecibels
				: Mathf.Log10(linearVolume) * 20f;
		}
	}
}
