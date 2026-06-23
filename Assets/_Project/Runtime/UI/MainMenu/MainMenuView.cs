using System;
using TLN.Application.Localization;
using TLN.Application.Saves;
using TLN.Application.Scenes;
using TLN.Core.Logging;
using TLN.UI.Common;
using TLN.UI.Saves;
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

		private VisualElement _root;
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
		private ISaveRepository _saveRepository;
		private SaveSessionService _saveSessionService;
		private SaveSlotsPanel _saveSlotsPanel;

		[Inject]
		public void Construct(
			ISceneLoader sceneLoader,
			ILocalizationService localizationService,
			ISaveRepository saveRepository,
			SaveSessionService saveSessionService
		)
		{
			_sceneLoader = sceneLoader;
			_saveRepository = saveRepository;
			_saveSessionService = saveSessionService;

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
			InitializeSaveSlotsPanel();
			RefreshLoadGameButton();
		}

		private void Awake()
		{
			_root = GetComponent<UIDocument>()
				.rootVisualElement;

			_navigationPanel = _root.RequiredQ<VisualElement>("main-menu-navigation-panel");
			_settingsPanel = _root.RequiredQ<VisualElement>("main-menu-settings-panel");
			_newGameButton = _root.RequiredQ<Button>("new-game-button");
			_loadGameButton = _root.RequiredQ<Button>("load-game-button");
			_optionsButton = _root.RequiredQ<Button>("options-button");
			_quitButton = _root.RequiredQ<Button>("quit-button");
			_settingsBackButton = _root.RequiredQ<Button>("settings-back-button");
			_languageDropdown = _root.RequiredQ<DropdownField>("language-dropdown");

			SettingsMenuHelper.ConfigureLanguageDropdown(_languageDropdown);
			SyncLanguageDropdown();
			InitializeSaveSlotsPanel();
			SubscribeToUI();

			RefreshLoadGameButton();

			ShowNavigationPanel();
		}

		private void OnDestroy()
		{
			UnsubscribeFromUI();

			_saveSlotsPanel?.Dispose();

			if (_localizationService != null)
			{
				_localizationService.LocaleChanged -= OnLocaleChanged;
			}
		}

		private void InitializeSaveSlotsPanel()
		{
			if (_root == null || _saveSlotsPanel != null || _saveRepository == null)
			{
				return;
			}

			_saveSlotsPanel = new SaveSlotsPanel(
				_root,
				_saveRepository,
				OnNewGameSlotSelected,
				OnLoadGameSlotSelected,
				ShowNavigationPanel
			);
		}

		private bool TryEnsureSaveSlotsPanel()
		{
			InitializeSaveSlotsPanel();

			if (_saveSlotsPanel != null)
			{
				return true;
			}

			TLNLogger.LogError("Cannot show save slots because save repository is missing.");
			ShowNavigationPanel();
			return false;
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

		private void RefreshLoadGameButton()
		{
			if (_loadGameButton == null)
			{
				return;
			}

			bool hasSave =
				_saveRepository != null &&
				_saveRepository.TryGetMostRecentSlot(out _);

			_loadGameButton.SetEnabled(hasSave);

			if (hasSave)
			{
				_loadGameButton.RemoveFromClassList(DisabledClassName);
				return;
			}

			_loadGameButton.AddToClassList(DisabledClassName);
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
			_saveSlotsPanel?.Hide();
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
			SettingsMenuHelper.SyncLanguageDropdown(_languageDropdown, _localizationService);
		}

		private void OnNewGameClicked()
		{
			ShowSaveSlotsForNewGame();
		}

		private void OnLoadGameClicked()
		{
			ShowSaveSlotsForLoadGame();
		}

		private void ShowSaveSlotsForNewGame()
		{
			if (!TryEnsureSaveSlotsPanel())
			{
				return;
			}

			_navigationPanel.SetVisible(false);
			_settingsPanel.SetVisible(false);
			_saveSlotsPanel.ShowNewGame();
		}

		private void ShowSaveSlotsForLoadGame()
		{
			if (!TryEnsureSaveSlotsPanel())
			{
				return;
			}

			_navigationPanel.SetVisible(false);
			_settingsPanel.SetVisible(false);
			_saveSlotsPanel.ShowLoadGame();
		}

		private void OnNewGameSlotSelected(int slotId)
		{
			UnityEngine.Cursor.visible = false;

			if (_sceneLoader == null)
			{
				TLNLogger.LogError("Cannot start a new game because scene loader is missing.");

				return;
			}

			if (_saveSessionService == null)
			{
				TLNLogger.LogError("Cannot start a new game because save session service is missing.");

				return;
			}

			_saveRepository?.Delete(slotId);
			_saveSessionService.StartNewGame(slotId);
			_sceneLoader.LoadWorld();
		}

		private void OnLoadGameSlotSelected(int slotId)
		{
			if (_sceneLoader == null)
			{
				TLNLogger.LogError("Cannot load game because scene loader is missing.");
				return;
			}

			if (_saveSessionService == null)
			{
				TLNLogger.LogError("Cannot load game because save session service is missing.");
				return;
			}

			_saveSessionService.RequestLoadGame(slotId);
			_sceneLoader.LoadWorld();
		}

		private void OnQuitClicked()
		{
			#if UNITY_EDITOR
			EditorApplication.isPlaying = false;
			#else
            Application.Quit();
			#endif
		}
	}
}
