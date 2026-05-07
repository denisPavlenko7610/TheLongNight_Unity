using TLN.Core.Lifetime;

namespace TLN.Application.Input
{
	public interface ICursorService : IGameService
	{
		void LockGameplayCursor();
		void UnlockUICursor();
	}
}
