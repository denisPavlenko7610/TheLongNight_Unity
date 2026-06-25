using System;
using TLN.Application.Settings;
using UnityEngine;
using UnityEngine.Localization.Settings;

namespace TLN.Infrastructure.Settings
{
	public sealed class GameSettingsService : IGameSettingsService, IDisposable
	{
		private const string PrefsKey = "TLN_GameSettings_v1";

		private GameSettings _originalSettings;

		public GameSettings Settings { get; private set; }

		public event Action SettingsChanged;

		public GameSettingsService()
		{
			Settings = new GameSettings();
			_originalSettings = DeepCopy(Settings);
			Load();
		}

		public void Save()
		{
			string json = JsonUtility.ToJson(Settings);
			PlayerPrefs.SetString(PrefsKey, json);
			PlayerPrefs.Save();
			_originalSettings = DeepCopy(Settings);
			SettingsChanged?.Invoke();
		}

		public void Load()
		{
			if (PlayerPrefs.HasKey(PrefsKey))
			{
				string json = PlayerPrefs.GetString(PrefsKey);

				if (!string.IsNullOrEmpty(json))
				{
					GameSettings loaded = JsonUtility.FromJson<GameSettings>(json);

					if (loaded != null)
					{
						Settings = loaded;
					}
				}
			}

			Apply();
		}

		public void Apply()
		{
			QualitySettings.SetQualityLevel(Settings.QualityLevel, true);
			QualitySettings.globalTextureMipmapLimit = Mathf.Clamp(3 - Settings.TextureQuality, 0, 3);
			QualitySettings.vSyncCount = Settings.VSyncEnabled ? 1 : 0;
			Screen.SetResolution(
				Settings.ResolutionWidth,
				Settings.ResolutionHeight,
				ToFullScreenMode(Settings.DisplayMode),
				Settings.RefreshRate
			);

			if (!string.IsNullOrWhiteSpace(Settings.LocaleCode))
			{
				LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.GetLocale(Settings.LocaleCode);
			}
		}

		public void ResetToDefaults()
		{
			Settings = new GameSettings();
			Apply();
			Save();
		}

		public bool HasUnsavedChanges()
		{
			return !AreEqual(Settings, _originalSettings);
		}

		public void Dispose()
		{
		}

		private static GameSettings DeepCopy(GameSettings source)
		{
			string json = JsonUtility.ToJson(source);
			return JsonUtility.FromJson<GameSettings>(json);
		}

		private static bool AreEqual(GameSettings a, GameSettings b)
		{
			return JsonUtility.ToJson(a) == JsonUtility.ToJson(b);
		}

		private static FullScreenMode ToFullScreenMode(int displayMode)
		{
			return displayMode switch
			{
				1 => FullScreenMode.Windowed,
				2 => FullScreenMode.FullScreenWindow,
				_ => FullScreenMode.ExclusiveFullScreen
			};
		}
	}
}
