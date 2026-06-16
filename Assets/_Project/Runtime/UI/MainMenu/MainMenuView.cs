using System;
using TLN.Application.Localization;
using TLN.Application.Scenes;
using TLN.UI.Common;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TLN.UI.MainMenu
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class MainMenuView : MonoBehaviour
    {
        private const string DisabledClassName = "main-menu-button-disabled";

        private VisualElement _navigationPanel;
        private VisualElement _settingsPanel;

        private Button _newGameButton;
        private Button _loadGameButton;
        private Button _optionsButton;
        private Button _quitButton;
        private Button _settingsBackButton;

        private DropdownField _languageDropdown;

        private ISceneLoader _sceneLoader;
        private ILocalizationService _localizationService;

        [Inject]
        public void Construct(ISceneLoader sceneLoader, ILocalizationService localizationService)
        {
            _sceneLoader = sceneLoader;

            if (_localizationService != null)
            {
                _localizationService.LocaleChanged -= OnLocaleChanged;
            }

            _localizationService = localizationService;

            if (_localizationService != null)
            {
                _localizationService.LocaleChanged += OnLocaleChanged;
            }

            SyncLanguageDropdown();
        }

        private void Awake()
        {
            VisualElement root = GetComponent<UIDocument>()
                .rootVisualElement;

            _navigationPanel = root.RequiredQ<VisualElement>("main-menu-navigation-panel");
            _settingsPanel = root.RequiredQ<VisualElement>("main-menu-settings-panel");
            _newGameButton = root.RequiredQ<Button>("new-game-button");
            _loadGameButton = root.RequiredQ<Button>("load-game-button");
            _optionsButton = root.RequiredQ<Button>("options-button");
            _quitButton = root.RequiredQ<Button>("quit-button");
            _settingsBackButton = root.RequiredQ<Button>("settings-back-button");
            _languageDropdown = root.RequiredQ<DropdownField>("language-dropdown");

            SettingsMenuHelper.ConfigureLanguageDropdown(_languageDropdown);
            SyncLanguageDropdown();
            SubscribeToUI();

            SetComingSoon(_loadGameButton);

            ShowNavigationPanel();
        }

        private void OnDestroy()
        {
            UnsubscribeFromUI();

            if (_localizationService != null)
            {
                _localizationService.LocaleChanged -= OnLocaleChanged;
            }
        }

        private void SubscribeToUI()
        {
            _newGameButton.clicked += OnNewGameClicked;
            _loadGameButton.clicked += OnLoadGameClicked;
            _optionsButton.clicked += OnOptionsClicked;
            _quitButton.clicked += OnQuitClicked;
            _settingsBackButton.clicked += OnSettingsBackClicked;

            _languageDropdown.RegisterValueChangedCallback(OnLanguageChanged);
        }

        private void UnsubscribeFromUI()
        {
            if (_newGameButton != null)
            {
                _newGameButton.clicked -= OnNewGameClicked;
            }

            if (_loadGameButton != null)
            {
                _loadGameButton.clicked -= OnLoadGameClicked;
            }

            if (_optionsButton != null)
            {
                _optionsButton.clicked -= OnOptionsClicked;
            }

            if (_quitButton != null)
            {
                _quitButton.clicked -= OnQuitClicked;
            }

            if (_settingsBackButton != null)
            {
                _settingsBackButton.clicked -= OnSettingsBackClicked;
            }

            if (_languageDropdown != null)
            {
                _languageDropdown.UnregisterValueChangedCallback(OnLanguageChanged);
            }
        }

        private static void SetComingSoon(Button button)
        {
            button.SetEnabled(false);
            button.AddToClassList(DisabledClassName);
        }

        private void ShowNavigationPanel()
        {
            _navigationPanel.SetVisible(true);
            _settingsPanel.SetVisible(false);
        }

        private void ShowSettingsPanel()
        {
            SyncLanguageDropdown();

            _navigationPanel.SetVisible(false);
            _settingsPanel.SetVisible(true);
        }

        private void OnOptionsClicked()
        {
            ShowSettingsPanel();
        }

        private void OnSettingsBackClicked()
        {
            ShowNavigationPanel();
        }

        private void OnLanguageChanged(ChangeEvent<string> changeEvent)
        {
            if (_localizationService == null)
            {
                SyncLanguageDropdown();
                return;
            }

            string localeCode = SettingsMenuHelper.GetLocaleCode(changeEvent.newValue);

            if (string.IsNullOrEmpty(localeCode))
            {
                SyncLanguageDropdown();
                return;
            }

            bool wasChanged = _localizationService.TrySetLocale(localeCode);

            if (!wasChanged)
            {
                SyncLanguageDropdown();
            }
        }

        private void OnLocaleChanged()
        {
            SyncLanguageDropdown();
        }

        private void SyncLanguageDropdown()
        {
            SettingsMenuHelper.SyncLanguageDropdown(
                _languageDropdown, _localizationService);
        }

        private void OnNewGameClicked()
        {
            UnityEngine.Cursor.visible = false;

            if (_sceneLoader == null)
            {
                Debug.LogError("Cannot start a new game because " +
                    "scene loader is not available. " +
                    "Start the game through Boot scene.");

                return;
            }

            _sceneLoader.LoadWorld();
        }

        private static void OnLoadGameClicked()
        {
            Debug.Log("Load Game is not implemented yet.");
        }

        private static void OnQuitClicked()
        {
#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
