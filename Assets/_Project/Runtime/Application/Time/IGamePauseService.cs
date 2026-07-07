using TLN.Core.Lifetime;

namespace TLN.Application.Time
{
	public interface IGamePauseService : IGameService
	{
		void SetSimulationPaused(bool isPaused);
		void SetAudioPaused(bool isPaused);
		void Reset();
	}
}
