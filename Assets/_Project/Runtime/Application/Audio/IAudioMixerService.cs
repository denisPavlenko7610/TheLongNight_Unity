using TLN.Application.Settings;
using UnityEngine;

namespace TLN.Application.Audio
{
	public interface IAudioMixerService
	{
		void Apply(GameSettings settings);

		void AssignMixerGroup(AudioSource source, AudioBusId busId);
	}
}
