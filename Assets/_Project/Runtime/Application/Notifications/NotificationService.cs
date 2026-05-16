using TLN.Core.Logging;
using UnityEngine;

namespace TLN.Application.Notifications
{
	public sealed class NotificationService : INotificationService
	{
		private INotificationView _view;

		public void SetView(INotificationView view)
		{
			_view = view;
		}

		public void Show(string message)
		{
			if (string.IsNullOrWhiteSpace(message))
			{
				return;
			}

			TLNLogger.Info($"Notification: {message}");

			_view?.Show(message);
		}
	}
}
