using TLN.Application.Time;

namespace TLN.Infrastructure.Time
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
