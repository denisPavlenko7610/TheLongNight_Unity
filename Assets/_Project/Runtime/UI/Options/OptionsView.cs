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
		private static readonly string[] DisplayModeKeys =
		{
			LocalizationKeys.Settings.DisplayModeFullscreen,
			LocalizationKeys.Settings.DisplayModeWindowed,
			LocalizationKeys.Settings.DisplayModeBorderless
		};

		private static readonly string[] QualityKeys =
		{
			LocalizationKeys.Settings.QualityLow,
			LocalizationKeys.Settings.QualityMedium,
			LocalizationKeys.Settings.QualityHigh,
			LocalizationKeys.Settings.QualityUltra
		};

		private readonly VisualElement _root;
		private readonly IGameSettingsService _settingsService;
		private readonly ILocalizationService _localizationService;

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
			ILocalizationService localizationService,
			Action backClicked
		)
		{
			_root = root;
			_settingsService = settingsService;
			_localizationService = localizationService;

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

			if (_localizationService != null)
			{
				_localizationService.LocaleChanged += OnLocaleChanged;
			}

			ShowTab("audio");
		}

		public void Dispose()
		{
			if (_localizationService != null)
			{
				_localizationService.LocaleChanged -= OnLocaleChanged;
			}
		}

		public void RefreshFromSettings()
		{
			if (_settingsService == null)
			{
				return;
			}

			_isApplyingChange = true;
			GameSettings s = _settingsService.Settings;

			_masterVolume.SetValueWithoutNotify(s.MasterVolume);
			_sfxVolume.SetValueWithoutNotify(s.SfxVolume);
			_musicVolume.SetValueWithoutNotify(s.MusicVolume);
			_ambientVolume.SetValueWithoutNotify(s.AmbientVolume);
			UpdateVolumeLabel(_masterVolumeValue, s.MasterVolume);
			UpdateVolumeLabel(_sfxVolumeValue, s.SfxVolume);
			UpdateVolumeLabel(_musicVolumeValue, s.MusicVolume);
			UpdateVolumeLabel(_ambientVolumeValue, s.AmbientVolume);

			int displayModeIndex = Mathf.Clamp(s.DisplayMode, 0, _displayModeLabels.Count - 1);
			_displayModeDropdown.SetValueWithoutNotify(_displayModeLabels[displayModeIndex]);
			SelectResolutionInDropdown(s.ResolutionWidth, s.ResolutionHeight);
			_qualityDropdown.SetValueWithoutNotify(_qualityLabels[Mathf.Clamp(s.QualityLevel, 0, _qualityLabels.Count - 1)]);
			_textureQualityDropdown.SetValueWithoutNotify(_qualityLabels[Mathf.Clamp(s.TextureQuality, 0, _qualityLabels.Count - 1)]);
			_fieldOfView.SetValueWithoutNotify(s.FieldOfView);
			UpdateWholeNumberLabel(_fieldOfViewValue, s.FieldOfView);
			_brightness.SetValueWithoutNotify(s.Brightness);
			UpdateWholePercentLabel(_brightnessValue, s.Brightness);
			_vsyncToggle.SetValueWithoutNotify(s.VSyncEnabled);

			_mouseSensitivity.SetValueWithoutNotify(s.MouseSensitivity);
			UpdatePercent01Label(_mouseSensitivityValue, s.MouseSensitivity);
			_lookSmoothing.SetValueWithoutNotify(s.LookSmoothing);
			UpdatePercent01Label(_lookSmoothingValue, s.LookSmoothing);
			_invertMouseToggle.SetValueWithoutNotify(s.InvertMouseY);

			SyncLanguageDropdown();
			_autoWalkToggle.SetValueWithoutNotify(s.AutoWalkEnabled);
			_autoHarvestToggle.SetValueWithoutNotify(s.AutoHarvestEnabled);

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
			var resolutionLabels = new List<string>();

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

			foreach (var kvp in _tabButtons)
			{
				kvp.Value.EnableInClassList("options-tab-active", kvp.Key == tabName);
			}

			foreach (var kvp in _tabContents)
			{
				kvp.Value.SetVisible(kvp.Key == tabName);
			}
		}

		private void OnVolumeChanged(ChangeEvent<float> evt)
		{
			if (_isApplyingChange) return;

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
			if (_isApplyingChange) return;
			int index = _displayModeLabels.IndexOf(evt.newValue);
			if (index < 0) return;
			_settingsService.Settings.DisplayMode = index;
			_settingsService.Apply();
			_settingsService.Save();
		}

		private void OnResolutionChanged(ChangeEvent<string> evt)
		{
			if (_isApplyingChange) return;
			Resolution? parsed = ParseResolutionLabel(evt.newValue);
			if (parsed == null) return;
			Resolution res = parsed.Value;
			_settingsService.Settings.ResolutionWidth = res.width;
			_settingsService.Settings.ResolutionHeight = res.height;
			_settingsService.Settings.RefreshRate = res.refreshRate;
			_settingsService.Apply();
			_settingsService.Save();
		}

		private void OnQualityChanged(ChangeEvent<string> evt)
		{
			if (_isApplyingChange) return;
			int index = _qualityLabels.IndexOf(evt.newValue);
			if (index < 0) return;
			_settingsService.Settings.QualityLevel = index;
			_settingsService.Apply();
			_settingsService.Save();
		}

		private void OnTextureQualityChanged(ChangeEvent<string> evt)
		{
			if (_isApplyingChange) return;
			int index = _qualityLabels.IndexOf(evt.newValue);
			if (index < 0) return;
			_settingsService.Settings.TextureQuality = index;
			_settingsService.Apply();
			_settingsService.Save();
		}

		private void OnFieldOfViewChanged(ChangeEvent<float> evt)
		{
			if (_isApplyingChange) return;
			_settingsService.Settings.FieldOfView = evt.newValue;
			UpdateWholeNumberLabel(_fieldOfViewValue, evt.newValue);
			_settingsService.Save();
		}

		private void OnBrightnessChanged(ChangeEvent<float> evt)
		{
			if (_isApplyingChange) return;
			_settingsService.Settings.Brightness = evt.newValue;
			UpdateWholePercentLabel(_brightnessValue, evt.newValue);
			_settingsService.Save();
		}

		private void OnVSyncChanged(ChangeEvent<bool> evt)
		{
			if (_isApplyingChange) return;
			_settingsService.Settings.VSyncEnabled = evt.newValue;
			_settingsService.Apply();
			_settingsService.Save();
		}

		private void OnSensitivityChanged(ChangeEvent<float> evt)
		{
			if (_isApplyingChange) return;
			_settingsService.Settings.MouseSensitivity = evt.newValue;
			UpdatePercent01Label(_mouseSensitivityValue, evt.newValue);
			_settingsService.Save();
		}

		private void OnLookSmoothingChanged(ChangeEvent<float> evt)
		{
			if (_isApplyingChange) return;
			_settingsService.Settings.LookSmoothing = evt.newValue;
			UpdatePercent01Label(_lookSmoothingValue, evt.newValue);
			_settingsService.Save();
		}

		private void OnInvertMouseChanged(ChangeEvent<bool> evt)
		{
			if (_isApplyingChange) return;
			_settingsService.Settings.InvertMouseY = evt.newValue;
			_settingsService.Save();
		}

		private void OnLanguageChanged(ChangeEvent<string> evt)
		{
			if (_localizationService == null)
			{
				SyncLanguageDropdown();
				return;
			}

			string localeCode = SettingsMenuHelper.GetLocaleCode(evt.newValue);

			if (string.IsNullOrEmpty(localeCode))
			{
				SyncLanguageDropdown();
				return;
			}

			if (_localizationService.TrySetLocale(localeCode))
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
			if (_isApplyingChange) return;
			_settingsService.Settings.AutoWalkEnabled = evt.newValue;
			_settingsService.Save();
		}

		private void OnAutoHarvestChanged(ChangeEvent<bool> evt)
		{
			if (_isApplyingChange) return;
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
			SettingsMenuHelper.SyncLanguageDropdown(_languageDropdown, _localizationService);
		}

		private void RefreshLocalizedText()
		{
			_titleLabel.text = L(LocalizationKeys.Settings.Title);
			_tabButtons["audio"].text = L(LocalizationKeys.Settings.TabAudio);
			_tabButtons["graphics"].text = L(LocalizationKeys.Settings.TabGraphics);
			_tabButtons["controls"].text = L(LocalizationKeys.Settings.TabControls);
			_tabButtons["gameplay"].text = L(LocalizationKeys.Settings.TabGameplay);
			_defaultsButton.text = L(LocalizationKeys.Settings.Defaults);
			_backButton.text = L(LocalizationKeys.Settings.Back);

			SetControlLabel(_masterVolume, LocalizationKeys.Settings.AudioMaster);
			SetControlLabel(_sfxVolume, LocalizationKeys.Settings.AudioSfx);
			SetControlLabel(_musicVolume, LocalizationKeys.Settings.AudioMusic);
			SetControlLabel(_ambientVolume, LocalizationKeys.Settings.AudioAmbient);
			SetControlLabel(_displayModeDropdown, LocalizationKeys.Settings.GraphicsDisplayMode);
			SetControlLabel(_resolutionDropdown, LocalizationKeys.Settings.GraphicsResolution);
			SetControlLabel(_qualityDropdown, LocalizationKeys.Settings.GraphicsQuality);
			SetControlLabel(_textureQualityDropdown, LocalizationKeys.Settings.GraphicsTextureQuality);
			SetControlLabel(_fieldOfView, LocalizationKeys.Settings.GraphicsFieldOfView);
			SetControlLabel(_brightness, LocalizationKeys.Settings.GraphicsBrightness);
			SetControlLabel(_vsyncToggle, LocalizationKeys.Settings.GraphicsVSync);
			SetControlLabel(_mouseSensitivity, LocalizationKeys.Settings.ControlsSensitivity);
			SetControlLabel(_lookSmoothing, LocalizationKeys.Settings.ControlsLookSmoothing);
			SetControlLabel(_invertMouseToggle, LocalizationKeys.Settings.ControlsInvertMouse);
			SetControlLabel(_languageDropdown, LocalizationKeys.Settings.GameplayLanguage);
			SetControlLabel(_autoWalkToggle, LocalizationKeys.Settings.GameplayAutoWalk);
			SetControlLabel(_autoHarvestToggle, LocalizationKeys.Settings.GameplayAutoHarvest);

			SetDropdownChoices(_displayModeDropdown, _displayModeLabels, DisplayModeKeys);
			SetDropdownChoices(_qualityDropdown, _qualityLabels, QualityKeys);
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
				label.text = L(key);
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

		private void SetDropdownChoices(DropdownField dropdown, List<string> target, IReadOnlyList<string> keys)
		{
			target.Clear();
			foreach (string key in keys)
			{
				target.Add(L(key));
			}

			dropdown.choices = target;
		}

		private string L(string key)
		{
			return _localizationService != null ? _localizationService.Get(key) : key;
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

			var seen = new HashSet<string>();
			var unique = new List<Resolution>();

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
				if (parts.Length < 1) return null;

				string[] dimensions = parts[0].Split('x');
				if (dimensions.Length != 2) return null;

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
