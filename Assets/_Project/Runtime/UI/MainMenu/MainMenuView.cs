using TLN.Application.Localization;
using TLN.Application.Saves;
using TLN.Application.Scenes;
using TLN.Application.Settings;
using TLN.Core.Logging;
using TLN.UI.Common;
using TLN.UI.Options;
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
		private const int DefaultNewGameSlotId = 1;

		private VisualElement _root;
		private VisualElement _navigationPanel;
		private VisualElement _settingsPanel;

		private Button _newGameButton;
		private Button _loadGameButton;
		private Button _optionsButton;
		private Button _quitButton;

		private ISceneLoader _sceneLoader;
		private ILocalizationService _localizationService;
		private ISaveRepository _saveRepository;
		private SaveSessionService _saveSessionService;
		private IGameSettingsService _settingsService;
		private SaveSlotsPanel _saveSlotsPanel;

		private OptionsView _optionsView;

		[Inject]
		public void Construct(
			ISceneLoader sceneLoader,
			ILocalizationService localizationService,
			ISaveRepository saveRepository,
			SaveSessionService saveSessionService,
			IGameSettingsService settingsService
		)
		{
			_sceneLoader = sceneLoader;
			_localizationService = localizationService;
			_saveRepository = saveRepository;
			_saveSessionService = saveSessionService;
			_settingsService = settingsService;

			InitializeOptionsView();
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

			InitializeSaveSlotsPanel();
			InitializeOptionsView();
			SubscribeToUI();

			RefreshLoadGameButton();

			ShowNavigationPanel();
		}

		private void OnDestroy()
		{
			UnsubscribeFromUI();

			_saveSlotsPanel?.Dispose();
			_optionsView?.Dispose();
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
				_localizationService,
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

		private void ShowNavigationPanel()
		{
			_navigationPanel.SetVisible(true);
			_settingsPanel.SetVisible(false);
			_saveSlotsPanel?.Hide();
		}

		private void ShowSettingsPanel()
		{
			_navigationPanel.SetVisible(false);
			_settingsPanel.SetVisible(true);
		}

		private void InitializeOptionsView()
		{
			if (_settingsPanel == null || _settingsService == null || _optionsView != null)
			{
				return;
			}

			_optionsView = new OptionsView(
				_settingsPanel,
				_settingsService,
				_localizationService,
				ShowNavigationPanel
			);
		}

		private void OnOptionsClicked()
		{
			ShowSettingsPanel();
		}

		private void OnNewGameClicked()
		{
			OnNewGameSlotSelected(DefaultNewGameSlotId);
		}

		private void OnLoadGameClicked()
		{
			ShowSaveSlotsForLoadGame();
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

		private async void OnNewGameSlotSelected(int slotId)
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
			await _sceneLoader.LoadWorld();
		}

		private async void OnLoadGameSlotSelected(int slotId)
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
			await _sceneLoader.LoadWorld();
		}

		private void OnQuitClicked()
		{
			#if UNITY_EDITOR
			EditorApplication.isPlaying = false;
			#else
			UnityEngine.Application.Quit();
			#endif
		}
	}
}
