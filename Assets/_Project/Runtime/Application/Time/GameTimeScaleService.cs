using UnityEngine;

namespace TLN.Application.Time
{
	public sealed class GameTimeScaleService : IGameTimeScaleService
	{
		public void SetNormal()
		{
			UnityEngine.Time.timeScale = 1f;
		}

		public void SetPaused()
		{
			UnityEngine.Time.timeScale = 0f;
		}
	}
}
