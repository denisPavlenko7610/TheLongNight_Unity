using System;

namespace TLN.Gameplay.World.Streaming
{
	public interface ILocationTitleService
	{
		/// <summary>Currently displayed title, or null when nothing is shown.</summary>
		string CurrentTitle { get; }

		/// <summary>True while the current title is within its visibility window.</summary>
		bool IsVisible { get; }

		/// <summary>Shows a location title; replaces the current one if one is already visible.</summary>
		void Show(string title);
	}

	public sealed class LocationTitleService : ILocationTitleService
	{
		private const float VisibleSeconds = 4f;

		public string CurrentTitle { get; private set; }

		public bool IsVisible => CurrentTitle != null &&
			UnityEngine.Time.unscaledTime < _visibleUntil;

		private float _visibleUntil;

		public void Show(string title)
		{
			if (string.IsNullOrWhiteSpace(title))
			{
				return;
			}

			CurrentTitle = title.Trim();
			_visibleUntil = UnityEngine.Time.unscaledTime + VisibleSeconds;
		}
	}
}
