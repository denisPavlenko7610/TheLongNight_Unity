using System;

namespace TLN.Application.Settings
{
	public interface IGameSettingsService
	{
		GameSettings Settings { get; }

		event Action SettingsChanged;

		void Save();

		void Load();

		void Apply();

		void ResetToDefaults();
	}
}
