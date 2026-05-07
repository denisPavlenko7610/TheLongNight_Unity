using TLN.Core.Lifetime;

namespace TLN.Application.Time
{
	public interface IGameTimeScaleService : IGameService
	{
		void SetNormal();
		void SetPaused();
	}
}
