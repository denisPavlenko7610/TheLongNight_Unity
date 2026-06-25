using TLN.Application.GameStates;
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

namespace TLN.UI.Pause
{
	[RequireComponent(typeof(UIDocument))]
	public sealed class PauseMenuView : MonoBehaviour
	{
		private const string VisibleClassName = "pause-menu-root-visible";

		private VisualElement _root;
		private VisualElement _navigationPanel;
		private VisualElement _settingsPanel;

		private Button _resumeButton;
		private Button _saveButton;
		private Button _loadButton;
		private Button _settingsButton;
		private Button _quitButton;

		private OptionsView _optionsView;

		private IGameStateMachine _gameStateMachine;
		private ISceneLoader _sceneLoader;
		private IGameSaveService _gameSaveService;
		private ISaveRepository _saveRepository;
		private SaveSessionService _saveSessionService;
		private IGameSettingsService _settingsService;
		private SaveSlotsPanel _saveSlotsPanel;

		private bool _isInitialized;
		private bool _isStateSubscribed;

		[Inject]
		public void Construct(
			IGameStateMachine gameStateMachine,
			ISceneLoader sceneLoader,
			IGameSaveService gameSaveService,
			ISaveRepository saveRepository,
			SaveSessionService saveSessionService,
			IGameSettingsService settingsService
		)
		{
			UnsubscribeFromGameState();

			_gameStateMachine = gameStateMachine;
			_sceneLoader = sceneLoader;
			_gameSaveService = gameSaveService;
			_saveRepository = saveRepository;
			_saveSessionService = saveSessionService;
			_settingsService = settingsService;

			EnsureInitialized();
			InitializeSaveSlotsPanel();
			InitializeOptionsView();
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

			_saveSlotsPanel?.Dispose();
			_optionsView?.Dispose();
		}

		private void OnLoadClicked()
		{
			ShowLoadSlotsPanel();
		}

		private void ShowLoadSlotsPanel()
		{
			InitializeSaveSlotsPanel();

			if (_saveSlotsPanel == null)
			{
				return;
			}

			_navigationPanel.SetVisible(false);
			_settingsPanel.SetVisible(false);
			_saveSlotsPanel.ShowLoadGame();
		}

		private async void OnLoadGameSlotSelected(int slotId)
		{
			if (_saveSessionService == null)
			{
				TLNLogger.LogError("Cannot load game because save session service is missing.");
				return;
			}

			if (_sceneLoader == null)
			{
				TLNLogger.LogError("Cannot load game because scene loader is missing.");
				return;
			}

			_saveSessionService.RequestLoadGame(slotId);
			await _sceneLoader.LoadWorld();
		}

		private void EnsureInitialized()
		{
			if (_isInitialized)
			{
				return;
			}

			VisualElement documentRoot = GetComponent<UIDocument>()
				.rootVisualElement;

			_root = documentRoot.RequiredQ<VisualElement>("pause-menu-root");

			_navigationPanel = documentRoot.RequiredQ<VisualElement>("pause-navigation-panel");
			_settingsPanel = documentRoot.RequiredQ<VisualElement>("pause-settings-panel");
			_resumeButton = documentRoot.RequiredQ<Button>("resume-button");
			_saveButton = documentRoot.RequiredQ<Button>("save-button");
			_loadButton = documentRoot.RequiredQ<Button>("load-button");
			_settingsButton = documentRoot.RequiredQ<Button>("settings-button");
			_quitButton = documentRoot.RequiredQ<Button>("quit-button");

			_resumeButton.clicked += OnResumeClicked;
			_settingsButton.clicked += OnSettingsClicked;
			_quitButton.clicked += OnQuitClicked;
			_saveButton.clicked += OnSaveClicked;
			_loadButton.clicked += OnLoadClicked;

			RefreshSaveButtonState();
			InitializeSaveSlotsPanel();

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

			if (_saveButton != null)
			{
				_saveButton.clicked -= OnSaveClicked;
			}

			if (_loadButton != null)
			{
				_loadButton.clicked -= OnLoadClicked;
			}

			if (_settingsButton != null)
			{
				_settingsButton.clicked -= OnSettingsClicked;
			}

			if (_quitButton != null)
			{
				_quitButton.clicked -= OnQuitClicked;
			}
		}

		private void RefreshSaveButtonState()
		{
			bool canSave = _gameSaveService != null && _gameSaveService.CanSaveManually;

			_saveButton?.SetEnabled(canSave);

			bool canLoad = _saveRepository != null && _saveRepository.TryGetMostRecentSlot(out _);

			_loadButton?.SetEnabled(canLoad);
		}

		private void OnSaveClicked()
		{
			if (_gameSaveService != null)
			{
				_ = _gameSaveService.SaveManual();
			}
		}

		private void OnGameStateChanged(GameStateId previousState, GameStateId nextState)
		{
			ApplyGameState(nextState);
		}

		private void ApplyGameState(GameStateId state)
		{
			bool isPaused = state == GameStateId.Paused;

			if (isPaused)
			{
				ShowNavigationPanel();
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

			_saveSlotsPanel?.Hide();
			_navigationPanel.SetVisible(true);
			_settingsPanel.SetVisible(false);
		}

		private void ShowSettingsPanel()
		{
			if (_navigationPanel == null || _settingsPanel == null)
			{
				return;
			}

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

		private async void OnQuitClicked()
		{
			if (_sceneLoader != null)
			{
				await _sceneLoader.LoadMainMenu();
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
				null,
				OnLoadGameSlotSelected,
				ShowNavigationPanel
			);
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
				ShowNavigationPanel
			);
		}
	}
}
