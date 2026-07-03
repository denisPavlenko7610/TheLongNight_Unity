using System;
using TLN.Application.Audio;
using TLN.Application.Settings;
using UnityEngine;
using UnityEngine.Localization.Settings;

namespace TLN.Infrastructure.Settings
{
	public sealed class GameSettingsService : IGameSettingsService, IDisposable
	{
		private const string PrefsKey = "TLN_GameSettings_v1";
		private const float PrefsSaveDelaySeconds = 0.25f;
		private const int DefaultTargetFrameRate = 60;
		private const int UnappliedInt = int.MinValue;

		private readonly IAudioMixerService _audioMixerService;
		private GameSettings _originalSettings;
		private int _prefsSaveVersion;
		private bool _hasPendingPrefsSave;
		private bool _isDisposed;
		private int _lastQualityLevel = UnappliedInt;
		private int _lastTextureMipmapLimit = UnappliedInt;
		private int _lastVSyncCount = UnappliedInt;
		private int _lastResolutionWidth = UnappliedInt;
		private int _lastResolutionHeight = UnappliedInt;
		private int _lastRefreshRate = UnappliedInt;
		private FullScreenMode _lastFullScreenMode = (FullScreenMode)UnappliedInt;
		private string _lastLocaleCode;

		public GameSettings Settings { get; private set; }

		public event Action SettingsChanged;

		public GameSettingsService(IAudioMixerService audioMixerService = null)
		{
			_audioMixerService = audioMixerService;
			Settings = new GameSettings();
			_originalSettings = DeepCopy(Settings);
			Load();
		}

		public void Save()
		{
			ApplyAudioSettings();

			string json = JsonUtility.ToJson(Settings);
			PlayerPrefs.SetString(PrefsKey, json);
			SchedulePrefsSave();
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
			_originalSettings = DeepCopy(Settings);
		}

		public void Apply()
		{
			ApplyAudioSettings();
			ApplyGraphicsSettings();
			ApplyLocaleSettings();
		}

		private void ApplyGraphicsSettings()
		{
			if (_lastQualityLevel != Settings.QualityLevel)
			{
				QualitySettings.SetQualityLevel(Settings.QualityLevel, true);
				_lastQualityLevel = Settings.QualityLevel;
			}

			int textureMipmapLimit = Mathf.Clamp(3 - Settings.TextureQuality, 0, 3);
			if (_lastTextureMipmapLimit != textureMipmapLimit)
			{
				QualitySettings.globalTextureMipmapLimit = textureMipmapLimit;
				_lastTextureMipmapLimit = textureMipmapLimit;
			}

			int vSyncCount = Settings.VSyncEnabled ? 1 : 0;
			if (_lastVSyncCount != vSyncCount)
			{
				QualitySettings.vSyncCount = vSyncCount;
				UnityEngine.Application.targetFrameRate = Settings.VSyncEnabled ? -1 : DefaultTargetFrameRate;
				_lastVSyncCount = vSyncCount;
			}

			FullScreenMode fullScreenMode = ToFullScreenMode(Settings.DisplayMode);
			if (_lastResolutionWidth == Settings.ResolutionWidth &&
			    _lastResolutionHeight == Settings.ResolutionHeight &&
			    _lastRefreshRate == Settings.RefreshRate &&
			    _lastFullScreenMode == fullScreenMode)
			{
				return;
			}

			Screen.SetResolution(
				Settings.ResolutionWidth,
				Settings.ResolutionHeight,
				fullScreenMode,
				Settings.RefreshRate
			);
			_lastResolutionWidth = Settings.ResolutionWidth;
			_lastResolutionHeight = Settings.ResolutionHeight;
			_lastRefreshRate = Settings.RefreshRate;
			_lastFullScreenMode = fullScreenMode;
		}

		private void ApplyLocaleSettings()
		{
			if (string.IsNullOrWhiteSpace(Settings.LocaleCode) ||
			    _lastLocaleCode == Settings.LocaleCode)
			{
				return;
			}

			UnityEngine.Localization.Locale locale =
				LocalizationSettings.AvailableLocales.GetLocale(Settings.LocaleCode);

			if (locale != null)
			{
				LocalizationSettings.SelectedLocale = locale;
				_lastLocaleCode = Settings.LocaleCode;
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
			_isDisposed = true;
			FlushPendingPrefsSave();
		}

		private void ApplyAudioSettings()
		{
			_audioMixerService?.Apply(Settings);
		}

		private async void SchedulePrefsSave()
		{
			int version = ++_prefsSaveVersion;
			_hasPendingPrefsSave = true;

			await WaitUnscaled(PrefsSaveDelaySeconds);

			if (_isDisposed || version != _prefsSaveVersion)
			{
				return;
			}

			FlushPendingPrefsSave();
		}

		private void FlushPendingPrefsSave()
		{
			if (!_hasPendingPrefsSave)
			{
				return;
			}

			_hasPendingPrefsSave = false;
			PlayerPrefs.Save();
		}

		private static async Awaitable WaitUnscaled(float seconds)
		{
			float endTime = UnityEngine.Time.realtimeSinceStartup + seconds;

			while (UnityEngine.Time.realtimeSinceStartup < endTime)
			{
				await Awaitable.NextFrameAsync();
			}
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
