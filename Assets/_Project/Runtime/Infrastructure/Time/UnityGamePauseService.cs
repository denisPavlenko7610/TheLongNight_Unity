using TLN.Application.Time;
using UnityEngine;

namespace TLN.Infrastructure.Time
{
	public sealed class UnityGamePauseService : IGamePauseService
	{
		public void SetSimulationPaused(bool isPaused)
		{
			UnityEngine.Time.timeScale = isPaused ? 0f : 1f;
		}

		public void SetAudioPaused(bool isPaused)
		{
			AudioListener.pause = isPaused;
		}

		public void Reset()
		{
			SetSimulationPaused(false);
			SetAudioPaused(false);
		}
	}
}
