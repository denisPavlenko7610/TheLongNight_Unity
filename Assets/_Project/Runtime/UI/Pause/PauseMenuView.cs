using TLN.Application.GameStates;
using TLN.Application.Localization;
using TLN.Application.Scenes;
using TLN.Core.GameStates;
using TLN.UI.Common;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;

namespace TLN.UI.Pause
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class PauseMenuView : MonoBehaviour
    {
        private const string VisibleClassName =
            "pause-menu-root-visible";

        private VisualElement _root;
        private VisualElement _navigationPanel;
        private VisualElement _settingsPanel;

        private Button _resumeButton;
        private Button _saveButton;
        private Button _loadButton;
        private Button _settingsButton;
        private Button _quitButton;
        private Button _settingsBackButton;

        private DropdownField _languageDropdown;

        private IGameStateMachine _gameStateMachine;
        private ISceneLoader _sceneLoader;
        private ILocalizationService _localizationService;

        private bool _isInitialized;
        private bool _isStateSubscribed;

        [Inject]
        public void Construct(
            IGameStateMachine gameStateMachine,
            ISceneLoader sceneLoader,
            ILocalizationService localizationService)
        {
            UnsubscribeFromGameState();

            _gameStateMachine = gameStateMachine;
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

            EnsureInitialized();
            SubscribeToGameState();
            ApplyGameState(_gameStateMachine.CurrentState);
        }

        private void Awake()
        {
            EnsureInitialized();

            if (_gameStateMachine == null)
            {
                SetMenuVisible(false);
                return;
            }

            SubscribeToGameState();
            ApplyGameState(_gameStateMachine.CurrentState);
        }

        private void OnDestroy()
        {
            UnsubscribeFromGameState();
            UnsubscribeFromUI();

            if (_localizationService != null)
            {
                _localizationService.LocaleChanged -= OnLocaleChanged;
            }
        }

        private void EnsureInitialized()
        {
            if (_isInitialized)
            {
                return;
            }

            VisualElement documentRoot = GetComponent<UIDocument>()
                .rootVisualElement;

            _root = documentRoot.RequiredQ<VisualElement>(
                "pause-menu-root");

            _navigationPanel = documentRoot.RequiredQ<VisualElement>(
                "pause-navigation-panel");

            _settingsPanel = documentRoot.RequiredQ<VisualElement>(
                "pause-settings-panel");

            _resumeButton = documentRoot.RequiredQ<Button>(
                "resume-button");

            _saveButton = documentRoot.RequiredQ<Button>(
                "save-button");

            _loadButton = documentRoot.RequiredQ<Button>(
                "load-button");

            _settingsButton = documentRoot.RequiredQ<Button>(
                "settings-button");

            _quitButton = documentRoot.RequiredQ<Button>(
                "quit-button");

            _settingsBackButton = documentRoot.RequiredQ<Button>(
                "pause-settings-back-button");

            _languageDropdown = documentRoot.RequiredQ<DropdownField>(
                "pause-language-dropdown");

            SettingsMenuHelper.ConfigureLanguageDropdown(_languageDropdown);

            _resumeButton.clicked += OnResumeClicked;
            _settingsButton.clicked += OnSettingsClicked;
            _quitButton.clicked += OnQuitClicked;
            _settingsBackButton.clicked += OnSettingsBackClicked;

            _languageDropdown.RegisterValueChangedCallback(OnLanguageChanged);

            _saveButton.SetEnabled(false);
            _loadButton.SetEnabled(false);

            SetMenuVisible(false);
            ShowNavigationPanel();

            _isInitialized = true;
        }

        private void SubscribeToGameState()
        {
            if (_isStateSubscribed || _gameStateMachine == null)
            {
                return;
            }

            _gameStateMachine.StateChanged += OnGameStateChanged;
            _isStateSubscribed = true;
        }

        private void UnsubscribeFromGameState()
        {
            if (!_isStateSubscribed || _gameStateMachine == null)
            {
                return;
            }

            _gameStateMachine.StateChanged -= OnGameStateChanged;
            _isStateSubscribed = false;
        }

        private void UnsubscribeFromUI()
        {
            if (_resumeButton != null)
            {
                _resumeButton.clicked -= OnResumeClicked;
            }

            if (_settingsButton != null)
            {
                _settingsButton.clicked -= OnSettingsClicked;
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
                _languageDropdown.UnregisterValueChangedCallback(
                    OnLanguageChanged);
            }
        }

        private void OnGameStateChanged(
            GameStateId previousState,
            GameStateId nextState)
        {
            ApplyGameState(nextState);
        }

        private void ApplyGameState(GameStateId state)
        {
            bool isPaused = state == GameStateId.Paused;

            if (isPaused)
            {
                ShowNavigationPanel();
                SyncLanguageDropdown();
            }

            SetMenuVisible(isPaused);
        }

        private void SetMenuVisible(bool isVisible)
        {
            if (_root == null)
            {
                return;
            }

            if (isVisible)
            {
                _root.AddToClassList(VisibleClassName);
                _root.SetVisible(true);
                return;
            }

            _root.RemoveFromClassList(VisibleClassName);
            _root.SetVisible(false);
        }

        private void ShowNavigationPanel()
        {
            if (_navigationPanel == null || _settingsPanel == null)
            {
                return;
            }

            _navigationPanel.SetVisible(true);
            _settingsPanel.SetVisible(false);
        }

        private void ShowSettingsPanel()
        {
            if (_navigationPanel == null || _settingsPanel == null)
            {
                return;
            }

            SyncLanguageDropdown();

            _navigationPanel.SetVisible(false);
            _settingsPanel.SetVisible(true);
        }

        private void OnResumeClicked()
        {
            _gameStateMachine?.Enter(GameStateId.Playing);
        }

        private void OnSettingsClicked()
        {
            ShowSettingsPanel();
        }

        private void OnSettingsBackClicked()
        {
            ShowNavigationPanel();
        }

        private void OnQuitClicked()
        {
            _sceneLoader?.LoadMainMenu();
        }

        private void OnLanguageChanged(ChangeEvent<string> changeEvent)
        {
            if (_localizationService == null)
            {
                SyncLanguageDropdown();
                return;
            }

            string localeCode = SettingsMenuHelper.GetLocaleCode(
                changeEvent.newValue);

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
    }
}
