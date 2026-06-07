using TLN.Core.Lifetime;

namespace TLN.Application.Notifications
{
	public interface INotificationService : IGameService
	{
		void SetView(INotificationView view);
		void Show(string message);
	}
}
