using TLN.Application.Audio;
using TLN.Application.Settings;
using UnityEngine;

namespace TLN.Infrastructure.Audio
{
	public sealed class UnityAudioMixerService : IAudioMixerService
	{
		private readonly AudioMixerConfig _config;

		public UnityAudioMixerService(AudioMixerConfig config)
		{
			_config = config;
		}

		public void Apply(GameSettings settings)
		{
			_config?.Apply(settings);
		}

		public void AssignMixerGroup(AudioSource source, AudioBusId busId)
		{
			if (source == null || _config == null)
			{
				return;
			}

			source.outputAudioMixerGroup = _config.GetGroup(busId);
		}
	}
}
