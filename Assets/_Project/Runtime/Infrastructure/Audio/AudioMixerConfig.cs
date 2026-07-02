using TLN.Application.Audio;
using TLN.Application.Settings;
using UnityEngine;
using UnityEngine.Audio;

namespace TLN.Infrastructure.Audio
{
	[CreateAssetMenu(fileName = "AudioMixerConfig", menuName = "TLN/Audio/Audio Mixer Config")]
	public sealed class AudioMixerConfig : ScriptableObject
	{
		private const float MinDecibels = -80f;
		private const float MinAudibleLinear = 0.0001f;

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

		public AudioMixer Mixer => _mixer;
		public AudioMixerGroup MasterGroup => _masterGroup;
		public AudioMixerGroup MusicGroup => _musicGroup;
		public AudioMixerGroup AmbientGroup => _ambientGroup;
		public AudioMixerGroup SfxGroup => _sfxGroup;

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

			SetLinearVolume(_masterVolumeParameter, settings.MasterVolume);
			SetLinearVolume(_musicVolumeParameter, settings.MusicVolume);
			SetLinearVolume(_ambientVolumeParameter, settings.AmbientVolume);
			SetLinearVolume(_sfxVolumeParameter, settings.SfxVolume);
		}

		private void SetLinearVolume(string exposedParameter, float linearVolume)
		{
			if (string.IsNullOrWhiteSpace(exposedParameter))
			{
				return;
			}

			_mixer.SetFloat(
				exposedParameter,
				LinearToDecibels(Mathf.Clamp01(linearVolume))
			);
		}

		private static float LinearToDecibels(float linearVolume)
		{
			return linearVolume <= MinAudibleLinear
				? MinDecibels
				: Mathf.Log10(linearVolume) * 20f;
		}
	}
}
