using TLN.Core.Lifetime;

namespace TLN.Application.Notifications
{
	public interface INotificationService : IGameService
	{
		void Show(string message);
	}
}
