using System;
using System.Collections.Generic;
using TLN.Application.Localization;
using TLN.Application.Settings;
using TLN.UI.Common;
using UnityEngine;
using UnityEngine.UIElements;

namespace TLN.UI.Options
{
	public sealed class OptionsView : IDisposable
	{
		private static readonly Func<string>[] DisplayModeTextFactories =
		{
			() => Loc.SettingsDisplayModeFullscreen,
			() => Loc.SettingsDisplayModeWindowed,
			() => Loc.SettingsDisplayModeBorderless
		};

		private static readonly Func<string>[] QualityTextFactories =
		{
			() => Loc.SettingsQualityLow,
			() => Loc.SettingsQualityMedium,
			() => Loc.SettingsQualityHigh,
			() => Loc.SettingsQualityUltra
		};

		private readonly VisualElement _root;
		private readonly IGameSettingsService _settingsService;

		private readonly Dictionary<string, Button> _tabButtons = new();
		private readonly Dictionary<string, VisualElement> _tabContents = new();
		private readonly List<string> _displayModeLabels = new();
		private readonly List<string> _qualityLabels = new();
		private string _activeTab;

		private readonly Label _titleLabel;
		private readonly Button _defaultsButton;
		private readonly Button _backButton;

		private readonly Slider _masterVolume;
		private readonly Slider _sfxVolume;
		private readonly Slider _musicVolume;
		private readonly Slider _ambientVolume;
		private readonly Label _masterVolumeValue;
		private readonly Label _sfxVolumeValue;
		private readonly Label _musicVolumeValue;
		private readonly Label _ambientVolumeValue;

		private readonly DropdownField _displayModeDropdown;
		private readonly DropdownField _resolutionDropdown;
		private readonly DropdownField _qualityDropdown;
		private readonly DropdownField _textureQualityDropdown;
		private readonly Slider _fieldOfView;
		private readonly Label _fieldOfViewValue;
		private readonly Slider _brightness;
		private readonly Label _brightnessValue;
		private readonly Toggle _vsyncToggle;

		private readonly Slider _mouseSensitivity;
		private readonly Label _mouseSensitivityValue;
		private readonly Slider _lookSmoothing;
		private readonly Label _lookSmoothingValue;
		private readonly Toggle _invertMouseToggle;

		private readonly DropdownField _languageDropdown;
		private readonly Toggle _autoWalkToggle;
		private readonly Toggle _autoHarvestToggle;

		private bool _isApplyingChange;
		private readonly List<Resolution> _availableResolutions;

		public OptionsView(
			VisualElement root,
			IGameSettingsService settingsService,
			Action backClicked
		)
		{
			_root = root;
			_settingsService = settingsService;

			_availableResolutions = GetUniqueResolutions();
			_titleLabel = root.RequiredQ<Label>("options-title");

			string[] tabNames = { "audio", "graphics", "controls", "gameplay" };

			foreach (string name in tabNames)
			{
				_tabButtons[name] = root.RequiredQ<Button>($"options-tab-{name}");
				_tabContents[name] = root.RequiredQ<VisualElement>($"options-{name}-content");

				string tabName = name;
				_tabButtons[name].clicked += () => ShowTab(tabName);
			}

			_defaultsButton = root.RequiredQ<Button>("options-defaults-button");
			_backButton = root.RequiredQ<Button>("options-back-button");

			_defaultsButton.clicked += OnDefaultsClicked;
			_backButton.clicked += () => backClicked?.Invoke();

			_masterVolume = root.RequiredQ<Slider>("options-master-volume");
			_sfxVolume = root.RequiredQ<Slider>("options-sfx-volume");
			_musicVolume = root.RequiredQ<Slider>("options-music-volume");
			_ambientVolume = root.RequiredQ<Slider>("options-ambient-volume");

			_masterVolumeValue = root.RequiredQ<Label>("options-master-volume-value");
			_sfxVolumeValue = root.RequiredQ<Label>("options-sfx-volume-value");
			_musicVolumeValue = root.RequiredQ<Label>("options-music-volume-value");
			_ambientVolumeValue = root.RequiredQ<Label>("options-ambient-volume-value");

			_displayModeDropdown = root.RequiredQ<DropdownField>("options-display-mode");
			_resolutionDropdown = root.RequiredQ<DropdownField>("options-resolution");
			_qualityDropdown = root.RequiredQ<DropdownField>("options-quality");
			_textureQualityDropdown = root.RequiredQ<DropdownField>("options-texture-quality");
			_fieldOfView = root.RequiredQ<Slider>("options-fov");
			_fieldOfViewValue = root.RequiredQ<Label>("options-fov-value");
			_brightness = root.RequiredQ<Slider>("options-brightness");
			_brightnessValue = root.RequiredQ<Label>("options-brightness-value");
			_vsyncToggle = root.RequiredQ<Toggle>("options-vsync");

			_mouseSensitivity = root.RequiredQ<Slider>("options-mouse-sensitivity");
			_mouseSensitivityValue = root.RequiredQ<Label>("options-mouse-sensitivity-value");
			_lookSmoothing = root.RequiredQ<Slider>("options-look-smoothing");
			_lookSmoothingValue = root.RequiredQ<Label>("options-look-smoothing-value");
			_invertMouseToggle = root.RequiredQ<Toggle>("options-invert-mouse");

			_languageDropdown = root.RequiredQ<DropdownField>("options-language-dropdown");
			_autoWalkToggle = root.RequiredQ<Toggle>("options-auto-walk");
			_autoHarvestToggle = root.RequiredQ<Toggle>("options-auto-harvest");

			InitializeDropdowns();
			WireControls();
			RefreshLocalizedText();
			RefreshFromSettings();

			LocaleCodes.LocaleChanged += OnLocaleChanged;

			ShowTab("audio");
		}

		public void Dispose()
		{
			LocaleCodes.LocaleChanged -= OnLocaleChanged;
		}

		public void RefreshFromSettings()
		{
			if (_settingsService == null)
			{
				return;
			}

			_isApplyingChange = true;
			GameSettings gameSettings = _settingsService.Settings;

			_masterVolume.SetValueWithoutNotify(gameSettings.MasterVolume);
			_sfxVolume.SetValueWithoutNotify(gameSettings.SfxVolume);
			_musicVolume.SetValueWithoutNotify(gameSettings.MusicVolume);
			_ambientVolume.SetValueWithoutNotify(gameSettings.AmbientVolume);
			UpdateVolumeLabel(_masterVolumeValue, gameSettings.MasterVolume);
			UpdateVolumeLabel(_sfxVolumeValue, gameSettings.SfxVolume);
			UpdateVolumeLabel(_musicVolumeValue, gameSettings.MusicVolume);
			UpdateVolumeLabel(_ambientVolumeValue, gameSettings.AmbientVolume);

			int displayModeIndex = Mathf.Clamp(gameSettings.DisplayMode, 0, _displayModeLabels.Count - 1);
			_displayModeDropdown.SetValueWithoutNotify(_displayModeLabels[displayModeIndex]);
			SelectResolutionInDropdown(gameSettings.ResolutionWidth, gameSettings.ResolutionHeight);
			_qualityDropdown.SetValueWithoutNotify(_qualityLabels[Mathf.Clamp(gameSettings.QualityLevel, 0, _qualityLabels.Count - 1)]);
			_textureQualityDropdown.SetValueWithoutNotify(_qualityLabels[Mathf.Clamp(gameSettings.TextureQuality, 0, _qualityLabels.Count - 1)]);
			_fieldOfView.SetValueWithoutNotify(gameSettings.FieldOfView);
			UpdateWholeNumberLabel(_fieldOfViewValue, gameSettings.FieldOfView);
			_brightness.SetValueWithoutNotify(gameSettings.Brightness);
			UpdateWholePercentLabel(_brightnessValue, gameSettings.Brightness);
			_vsyncToggle.SetValueWithoutNotify(gameSettings.VSyncEnabled);

			_mouseSensitivity.SetValueWithoutNotify(gameSettings.MouseSensitivity);
			UpdatePercent01Label(_mouseSensitivityValue, gameSettings.MouseSensitivity);
			_lookSmoothing.SetValueWithoutNotify(gameSettings.LookSmoothing);
			UpdatePercent01Label(_lookSmoothingValue, gameSettings.LookSmoothing);
			_invertMouseToggle.SetValueWithoutNotify(gameSettings.InvertMouseY);

			SyncLanguageDropdown();
			_autoWalkToggle.SetValueWithoutNotify(gameSettings.AutoWalkEnabled);
			_autoHarvestToggle.SetValueWithoutNotify(gameSettings.AutoHarvestEnabled);

			_isApplyingChange = false;
		}

		private void InitializeDropdowns()
		{
			SettingsMenuHelper.ConfigureLanguageDropdown(_languageDropdown);

			PopulateResolutionDropdown();

			_masterVolume.lowValue = 0f;
			_masterVolume.highValue = 1f;
			_sfxVolume.lowValue = 0f;
			_sfxVolume.highValue = 1f;
			_musicVolume.lowValue = 0f;
			_musicVolume.highValue = 1f;
			_ambientVolume.lowValue = 0f;
			_ambientVolume.highValue = 1f;

			_fieldOfView.lowValue = 60f;
			_fieldOfView.highValue = 110f;
			_brightness.lowValue = 0f;
			_brightness.highValue = 100f;

			_mouseSensitivity.lowValue = 0.05f;
			_mouseSensitivity.highValue = 1f;
			_lookSmoothing.lowValue = 0f;
			_lookSmoothing.highValue = 1f;
		}

		private void PopulateResolutionDropdown()
		{
			List<string> resolutionLabels = new List<string>();

			foreach (Resolution res in _availableResolutions)
			{
				resolutionLabels.Add($"{res.width}x{res.height} ({res.refreshRate}Hz)");
			}

			_resolutionDropdown.choices = resolutionLabels;

			if (resolutionLabels.Count > 0)
			{
				_resolutionDropdown.SetValueWithoutNotify(resolutionLabels[0]);
			}
		}

		private void SelectResolutionInDropdown(int width, int height)
		{
			for (int i = 0; i < _availableResolutions.Count; i++)
			{
				Resolution res = _availableResolutions[i];

				if (res.width == width && res.height == height)
				{
					_resolutionDropdown.SetValueWithoutNotify(
						$"{res.width}x{res.height} ({res.refreshRate}Hz)"
					);
					return;
				}
			}

			if (_availableResolutions.Count > 0)
			{
				Resolution first = _availableResolutions[0];
				_resolutionDropdown.SetValueWithoutNotify(
					$"{first.width}x{first.height} ({first.refreshRate}Hz)"
				);
			}
		}

		private void WireControls()
		{
			_masterVolume.RegisterValueChangedCallback(OnVolumeChanged);
			_sfxVolume.RegisterValueChangedCallback(OnVolumeChanged);
			_musicVolume.RegisterValueChangedCallback(OnVolumeChanged);
			_ambientVolume.RegisterValueChangedCallback(OnVolumeChanged);

			_displayModeDropdown.RegisterValueChangedCallback(OnDisplayModeChanged);
			_resolutionDropdown.RegisterValueChangedCallback(OnResolutionChanged);
			_qualityDropdown.RegisterValueChangedCallback(OnQualityChanged);
			_textureQualityDropdown.RegisterValueChangedCallback(OnTextureQualityChanged);
			_fieldOfView.RegisterValueChangedCallback(OnFieldOfViewChanged);
			_brightness.RegisterValueChangedCallback(OnBrightnessChanged);
			_vsyncToggle.RegisterValueChangedCallback(OnVSyncChanged);

			_mouseSensitivity.RegisterValueChangedCallback(OnSensitivityChanged);
			_lookSmoothing.RegisterValueChangedCallback(OnLookSmoothingChanged);
			_invertMouseToggle.RegisterValueChangedCallback(OnInvertMouseChanged);

			_languageDropdown.RegisterValueChangedCallback(OnLanguageChanged);
			_autoWalkToggle.RegisterValueChangedCallback(OnAutoWalkChanged);
			_autoHarvestToggle.RegisterValueChangedCallback(OnAutoHarvestChanged);
		}

		private void ShowTab(string tabName)
		{
			_activeTab = tabName;

			foreach (KeyValuePair<string, Button> kvp in _tabButtons)
			{
				kvp.Value.EnableInClassList("options-tab-active", kvp.Key == tabName);
			}

			foreach (KeyValuePair<string, VisualElement> kvp in _tabContents)
			{
				kvp.Value.SetVisible(kvp.Key == tabName);
			}
		}

		private void OnVolumeChanged(ChangeEvent<float> evt)
		{
			if (_isApplyingChange)
			{
				return;
			}

			GameSettings s = _settingsService.Settings;
			s.MasterVolume = _masterVolume.value;
			s.SfxVolume = _sfxVolume.value;
			s.MusicVolume = _musicVolume.value;
			s.AmbientVolume = _ambientVolume.value;

			UpdateVolumeLabel(_masterVolumeValue, s.MasterVolume);
			UpdateVolumeLabel(_sfxVolumeValue, s.SfxVolume);
			UpdateVolumeLabel(_musicVolumeValue, s.MusicVolume);
			UpdateVolumeLabel(_ambientVolumeValue, s.AmbientVolume);

			_settingsService.Save();
		}

		private void OnDisplayModeChanged(ChangeEvent<string> evt)
		{
			if (_isApplyingChange)
			{
				return;
			}
			int index = _displayModeLabels.IndexOf(evt.newValue);
			if (index < 0)
			{
				return;
			}
			_settingsService.Settings.DisplayMode = index;
			_settingsService.Apply();
			_settingsService.Save();
		}

		private void OnResolutionChanged(ChangeEvent<string> evt)
		{
			if (_isApplyingChange)
			{
				return;
			}
			Resolution? parsed = ParseResolutionLabel(evt.newValue);
			if (parsed == null)
			{
				return;
			}
			Resolution res = parsed.Value;
			_settingsService.Settings.ResolutionWidth = res.width;
			_settingsService.Settings.ResolutionHeight = res.height;
			_settingsService.Settings.RefreshRate = res.refreshRate;
			_settingsService.Apply();
			_settingsService.Save();
		}

		private void OnQualityChanged(ChangeEvent<string> evt)
		{
			if (_isApplyingChange)
			{
				return;
			}
			int index = _qualityLabels.IndexOf(evt.newValue);
			if (index < 0)
			{
				return;
			}
			_settingsService.Settings.QualityLevel = index;
			_settingsService.Apply();
			_settingsService.Save();
		}

		private void OnTextureQualityChanged(ChangeEvent<string> evt)
		{
			if (_isApplyingChange)
			{
				return;
			}
			int index = _qualityLabels.IndexOf(evt.newValue);
			if (index < 0)
			{
				return;
			}
			_settingsService.Settings.TextureQuality = index;
			_settingsService.Apply();
			_settingsService.Save();
		}

		private void OnFieldOfViewChanged(ChangeEvent<float> evt)
		{
			if (_isApplyingChange)
			{
				return;
			}
			_settingsService.Settings.FieldOfView = evt.newValue;
			UpdateWholeNumberLabel(_fieldOfViewValue, evt.newValue);
			_settingsService.Save();
		}

		private void OnBrightnessChanged(ChangeEvent<float> evt)
		{
			if (_isApplyingChange)
			{
				return;
			}
			_settingsService.Settings.Brightness = evt.newValue;
			UpdateWholePercentLabel(_brightnessValue, evt.newValue);
			_settingsService.Save();
		}

		private void OnVSyncChanged(ChangeEvent<bool> evt)
		{
			if (_isApplyingChange)
			{
				return;
			}
			_settingsService.Settings.VSyncEnabled = evt.newValue;
			_settingsService.Apply();
			_settingsService.Save();
		}

		private void OnSensitivityChanged(ChangeEvent<float> evt)
		{
			if (_isApplyingChange)
			{
				return;
			}
			_settingsService.Settings.MouseSensitivity = evt.newValue;
			UpdatePercent01Label(_mouseSensitivityValue, evt.newValue);
			_settingsService.Save();
		}

		private void OnLookSmoothingChanged(ChangeEvent<float> evt)
		{
			if (_isApplyingChange)
			{
				return;
			}
			_settingsService.Settings.LookSmoothing = evt.newValue;
			UpdatePercent01Label(_lookSmoothingValue, evt.newValue);
			_settingsService.Save();
		}

		private void OnInvertMouseChanged(ChangeEvent<bool> evt)
		{
			if (_isApplyingChange)
			{
				return;
			}
			_settingsService.Settings.InvertMouseY = evt.newValue;
			_settingsService.Save();
		}

		private void OnLanguageChanged(ChangeEvent<string> evt)
		{
			string localeCode = SettingsMenuHelper.GetLocaleCode(evt.newValue);

			if (string.IsNullOrEmpty(localeCode))
			{
				SyncLanguageDropdown();
				return;
			}

			if (LocaleCodes.TrySetLocale(localeCode))
			{
				_settingsService.Settings.LocaleCode = localeCode;
				_settingsService.Save();
			}
		}

		private void OnLocaleChanged()
		{
			RefreshLocalizedText();
			RefreshFromSettings();
		}

		private void OnAutoWalkChanged(ChangeEvent<bool> evt)
		{
			if (_isApplyingChange)
			{
				return;
			}
			_settingsService.Settings.AutoWalkEnabled = evt.newValue;
			_settingsService.Save();
		}

		private void OnAutoHarvestChanged(ChangeEvent<bool> evt)
		{
			if (_isApplyingChange)
			{
				return;
			}
			_settingsService.Settings.AutoHarvestEnabled = evt.newValue;
			_settingsService.Save();
		}

		private void OnDefaultsClicked()
		{
			_settingsService.ResetToDefaults();
			RefreshFromSettings();
		}

		private void SyncLanguageDropdown()
		{
			SettingsMenuHelper.SyncLanguageDropdown(_languageDropdown);
		}

		private void RefreshLocalizedText()
		{
			_titleLabel.text = Loc.SettingsTitle;
			_tabButtons["audio"].text = Loc.SettingsTabAudio;
			_tabButtons["graphics"].text = Loc.SettingsTabGraphics;
			_tabButtons["controls"].text = Loc.SettingsTabControls;
			_tabButtons["gameplay"].text = Loc.SettingsTabGameplay;
			_defaultsButton.text = Loc.SettingsDefaults;
			_backButton.text = Loc.SettingsBack;

			SetControlLabel(_masterVolume, Loc.SettingsAudioMaster);
			SetControlLabel(_sfxVolume, Loc.SettingsAudioSfx);
			SetControlLabel(_musicVolume, Loc.SettingsAudioMusic);
			SetControlLabel(_ambientVolume, Loc.SettingsAudioAmbient);
			SetControlLabel(_displayModeDropdown, Loc.SettingsGraphicsDisplayMode);
			SetControlLabel(_resolutionDropdown, Loc.SettingsGraphicsResolution);
			SetControlLabel(_qualityDropdown, Loc.SettingsGraphicsQuality);
			SetControlLabel(_textureQualityDropdown, Loc.SettingsGraphicsTextureQuality);
			SetControlLabel(_fieldOfView, Loc.SettingsGraphicsFieldOfView);
			SetControlLabel(_brightness, Loc.SettingsGraphicsBrightness);
			SetControlLabel(_vsyncToggle, Loc.SettingsGraphicsVSync);
			SetControlLabel(_mouseSensitivity, Loc.SettingsControlsSensitivity);
			SetControlLabel(_lookSmoothing, Loc.SettingsControlsLookSmoothing);
			SetControlLabel(_invertMouseToggle, Loc.SettingsControlsInvertMouse);
			SetControlLabel(_languageDropdown, Loc.SettingsGameplayLanguage);
			SetControlLabel(_autoWalkToggle, Loc.SettingsGameplayAutoWalk);
			SetControlLabel(_autoHarvestToggle, Loc.SettingsGameplayAutoHarvest);

			SetDropdownChoices(_displayModeDropdown, _displayModeLabels, DisplayModeTextFactories);
			SetDropdownChoices(_qualityDropdown, _qualityLabels, QualityTextFactories);
			_textureQualityDropdown.choices = _qualityLabels;
		}

		private void SetControlLabel(VisualElement control, string key)
		{
			VisualElement row = FindAncestorWithClass(control, "options-slider-row")
				?? FindAncestorWithClass(control, "options-dropdown-row")
				?? FindAncestorWithClass(control, "options-toggle-row");
			Label label = row?.Q<Label>(className: "tln-settings-label");
			if (label != null)
			{
				label.text = key;
			}
		}

		private static VisualElement FindAncestorWithClass(VisualElement element, string className)
		{
			VisualElement current = element.parent;
			while (current != null)
			{
				if (current.ClassListContains(className))
				{
					return current;
				}

				current = current.parent;
			}

			return null;
		}

		private void SetDropdownChoices(DropdownField dropdown, List<string> target, IReadOnlyList<Func<string>> textFactories)
		{
			target.Clear();
			foreach (Func<string> textFactory in textFactories)
			{
				target.Add(textFactory());
			}

			dropdown.choices = target;
		}

		private static void UpdateVolumeLabel(Label label, float volume)
		{
			int percent = Mathf.RoundToInt(volume * 100f);
			label.text = $"{percent}%";
		}

		private static void UpdatePercent01Label(Label label, float value)
		{
			int percent = Mathf.RoundToInt(value * 100f);
			label.text = $"{percent}%";
		}

		private static void UpdateWholePercentLabel(Label label, float value)
		{
			label.text = $"{Mathf.RoundToInt(value)}%";
		}

		private static void UpdateWholeNumberLabel(Label label, float value)
		{
			label.text = Mathf.RoundToInt(value).ToString();
		}

		private static List<Resolution> GetUniqueResolutions()
		{
			Resolution[] all = Screen.resolutions;

			if (all == null || all.Length == 0)
			{
				return new List<Resolution>
				{
					new Resolution { width = 1920, height = 1080, refreshRate = 60 },
					new Resolution { width = 2560, height = 1440, refreshRate = 60 },
					new Resolution { width = 3840, height = 2160, refreshRate = 60 }
				};
			}

			HashSet<string> seen = new HashSet<string>();
			List<Resolution> unique = new List<Resolution>();

			for (int i = all.Length - 1; i >= 0; i--)
			{
				Resolution res = all[i];
				string key = $"{res.width}x{res.height}x{res.refreshRate}";

				if (seen.Add(key))
				{
					unique.Add(res);
				}
			}

			unique.Reverse();
			return unique;
		}

		private static Resolution? ParseResolutionLabel(string label)
		{
			try
			{
				string[] parts = label.Split(' ');
				if (parts.Length < 1)
				{
					return null;
				}

				string[] dimensions = parts[0].Split('x');
				if (dimensions.Length != 2)
				{
					return null;
				}

				int width = int.Parse(dimensions[0]);
				int height = int.Parse(dimensions[1]);

				int refreshRate = 60;
				if (parts.Length >= 2)
				{
					string hz = parts[1].Trim('(', ')', 'H', 'z');
					refreshRate = int.Parse(hz);
				}

				return new Resolution
				{
					width = width,
					height = height,
					refreshRate = refreshRate
				};
			}
			catch
			{
				return null;
			}
		}
	}
}
