using System;
using System.Collections.Generic;
using TLN.Application.Localization;
using TLN.Application.Multiplayer;
using TLN.Application.Saves;
using TLN.Application.Scenes;
using TLN.Application.Settings;
using TLN.Application.GameStates;
using TLN.Core.Logging;
using TLN.Core.Results;
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

		private VisualElement _root;
		private VisualElement _navigationPanel;
		private VisualElement _singlePlayerPanel;
		private VisualElement _multiplayerPanel;
		private VisualElement _settingsPanel;

		private Label _modeLabel;
		private Label _singlePlayerTitleLabel;
		private Label _singlePlayerDescriptionLabel;
		private Label _multiplayerTitleLabel;
		private Label _multiplayerDescriptionLabel;
		private Label _sessionsTitleLabel;
		private Label _sessionsStatusLabel;
		private Label _joinCodeLabel;

		private Button _singlePlayerButton;
		private Button _multiplayerButton;
		private Button _newGameButton;
		private Button _loadGameButton;
		private Button _singlePlayerBackButton;
		private Button _optionsButton;
		private Button _quitButton;

		private ISceneLoader _sceneLoader;
		private ISaveRepository _saveRepository;
		private SaveSessionService _saveSessionService;
		private IGameSettingsService _settingsService;
		private SaveSlotsPanel _saveSlotsPanel;

		private OptionsView _optionsView;

		private Button _hostGameButton;
		private Button _refreshGamesButton;
		private Button _joinGameButton;
		private Button _multiplayerBackButton;
		private TextField _joinCodeField;
		private VisualElement _sessionsList;
		private IMultiplayerSessionService _multiplayerSessionService;
		private IGameStateMachine _gameStateMachine;
		private bool _isMultiplayerOperationInProgress;
		private bool _isSessionBrowseInProgress;

		[Inject]
		public void Construct(
			ISceneLoader sceneLoader,
			ISaveRepository saveRepository,
			SaveSessionService saveSessionService,
			IGameSettingsService settingsService,
			IMultiplayerSessionService multiplayerSessionService,
			IGameStateMachine gameStateMachine
		)
		{
			_sceneLoader = sceneLoader;
			_saveRepository = saveRepository;
			_saveSessionService = saveSessionService;
			_settingsService = settingsService;
			_multiplayerSessionService = multiplayerSessionService;
			_gameStateMachine = gameStateMachine;

			InitializeOptionsView();
			InitializeSaveSlotsPanel();
			RefreshLoadGameButton();
		}

		private void Awake()
		{
			_root = GetComponent<UIDocument>()
				.rootVisualElement;

			_navigationPanel = _root.RequiredQ<VisualElement>("main-menu-navigation-panel");
			_singlePlayerPanel = _root.RequiredQ<VisualElement>("main-menu-single-player-panel");
			_multiplayerPanel = _root.RequiredQ<VisualElement>("main-menu-multiplayer-panel");
			_settingsPanel = _root.RequiredQ<VisualElement>("main-menu-settings-panel");
			_modeLabel = _root.RequiredQ<Label>("main-menu-mode-label");
			_singlePlayerTitleLabel = _root.RequiredQ<Label>("single-player-title-label");
			_singlePlayerDescriptionLabel = _root.RequiredQ<Label>("single-player-description-label");
			_multiplayerTitleLabel = _root.RequiredQ<Label>("multiplayer-title-label");
			_multiplayerDescriptionLabel = _root.RequiredQ<Label>("multiplayer-description-label");
			_sessionsTitleLabel = _root.RequiredQ<Label>("sessions-title-label");
			_sessionsStatusLabel = _root.RequiredQ<Label>("sessions-status-label");
			_joinCodeLabel = _root.RequiredQ<Label>("join-code-label");
			_singlePlayerButton = _root.RequiredQ<Button>("single-player-button");
			_multiplayerButton = _root.RequiredQ<Button>("multiplayer-button");
			_newGameButton = _root.RequiredQ<Button>("new-game-button");
			_loadGameButton = _root.RequiredQ<Button>("load-game-button");
			_singlePlayerBackButton = _root.RequiredQ<Button>("single-player-back-button");
			_optionsButton = _root.RequiredQ<Button>("options-button");
			_quitButton = _root.RequiredQ<Button>("quit-button");
			_hostGameButton = _root.RequiredQ<Button>("host-game-button");
			_refreshGamesButton = _root.RequiredQ<Button>("refresh-games-button");
			_joinGameButton = _root.RequiredQ<Button>("join-game-button");
			_multiplayerBackButton = _root.RequiredQ<Button>("multiplayer-back-button");
			_joinCodeField = _root.RequiredQ<TextField>("join-code-field");
			_sessionsList = _root.RequiredQ<VisualElement>("sessions-list");

			InitializeSaveSlotsPanel();
			InitializeOptionsView();
			SubscribeToUI();
			RefreshLocalizedText();
			RefreshJoinGameButton();

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
			_singlePlayerButton.clicked += OnSinglePlayerClicked;
			_multiplayerButton.clicked += OnMultiplayerClicked;
			_newGameButton.clicked += OnNewGameClicked;
			_loadGameButton.clicked += OnLoadGameClicked;
			_singlePlayerBackButton.clicked += ShowNavigationPanel;
			_optionsButton.clicked += OnOptionsClicked;
			_quitButton.clicked += OnQuitClicked;
			_hostGameButton.clicked += OnHostGameClicked;
			_refreshGamesButton.clicked += OnRefreshGamesClicked;
			_joinGameButton.clicked += OnJoinGameClicked;
			_multiplayerBackButton.clicked += ShowNavigationPanel;
			_joinCodeField.RegisterValueChangedCallback(OnJoinCodeChanged);
			LocaleCodes.LocaleChanged += RefreshLocalizedText;
		}

		private void UnsubscribeFromUI()
		{
			LocaleCodes.LocaleChanged -= RefreshLocalizedText;

			if (_singlePlayerButton != null)
			{
				_singlePlayerButton.clicked -= OnSinglePlayerClicked;
			}

			if (_multiplayerButton != null)
			{
				_multiplayerButton.clicked -= OnMultiplayerClicked;
			}

			if (_newGameButton != null)
			{
				_newGameButton.clicked -= OnNewGameClicked;
			}

			if (_loadGameButton != null)
			{
				_loadGameButton.clicked -= OnLoadGameClicked;
			}

			if (_singlePlayerBackButton != null)
			{
				_singlePlayerBackButton.clicked -= ShowNavigationPanel;
			}

			if (_optionsButton != null)
			{
				_optionsButton.clicked -= OnOptionsClicked;
			}

			if (_quitButton != null)
			{
				_quitButton.clicked -= OnQuitClicked;
			}

			if (_hostGameButton != null)
			{
				_hostGameButton.clicked -= OnHostGameClicked;
			}

			if (_refreshGamesButton != null)
			{
				_refreshGamesButton.clicked -= OnRefreshGamesClicked;
			}

			if (_joinGameButton != null)
			{
				_joinGameButton.clicked -= OnJoinGameClicked;
			}

			if (_multiplayerBackButton != null)
			{
				_multiplayerBackButton.clicked -= ShowNavigationPanel;
			}

			if (_joinCodeField != null)
			{
				_joinCodeField.UnregisterValueChangedCallback(OnJoinCodeChanged);
			}
		}

		private void RefreshLocalizedText()
		{
			_modeLabel.text = Loc.MainMenuMode;
			_singlePlayerButton.text = Loc.MainMenuSinglePlayer;
			_multiplayerButton.text = Loc.MainMenuMultiplayer;
			_newGameButton.text = Loc.MainMenuNewGame;
			_loadGameButton.text = Loc.MainMenuLoadGame;
			_optionsButton.text = Loc.MainMenuOptions;
			_quitButton.text = Loc.MainMenuQuit;
			_singlePlayerTitleLabel.text = Loc.MainMenuSinglePlayer;
			_singlePlayerDescriptionLabel.text = Loc.MainMenuSinglePlayerDescription;
			_singlePlayerBackButton.text = Loc.SettingsBack;
			_multiplayerTitleLabel.text = Loc.MainMenuMultiplayer;
			_multiplayerDescriptionLabel.text = Loc.MainMenuMultiplayerDescription;
			_hostGameButton.text = Loc.MainMenuHostGame;
			_refreshGamesButton.text = "REFRESH GAMES";
			_sessionsTitleLabel.text = "AVAILABLE GAMES";
			_sessionsStatusLabel.text = "Refresh to search public games.";
			_joinCodeLabel.text = Loc.MainMenuJoinCode;
			_joinGameButton.text = Loc.MainMenuJoinGame;
			_multiplayerBackButton.text = Loc.SettingsBack;
		}

		private void OnJoinCodeChanged(ChangeEvent<string> changeEvent)
		{
			RefreshJoinGameButton();
		}

		private void RefreshJoinGameButton()
		{
			if (_joinGameButton == null)
			{
				return;
			}

			bool hasJoinCode =
				_joinCodeField != null &&
				!string.IsNullOrWhiteSpace(_joinCodeField.value);

			bool isEnabled = hasJoinCode && !_isMultiplayerOperationInProgress;
			_joinGameButton.SetEnabled(isEnabled);
			_joinGameButton.EnableInClassList(DisabledClassName, !isEnabled);
		}

		private async void OnRefreshGamesClicked()
		{
			await RefreshAvailableSessions();
		}

		private async Awaitable RefreshAvailableSessions()
		{
			if (_isMultiplayerOperationInProgress ||
			    _isSessionBrowseInProgress)
			{
				return;
			}

			if (_multiplayerSessionService == null)
			{
				SetSessionsStatus("Multiplayer service is missing.");
				return;
			}

			SetSessionBrowseInProgress(true);
			SetSessionsStatus("Searching public games...");
			_sessionsList?.Clear();

			try
			{
				OperationResult<IReadOnlyList<MultiplayerSessionInfo>> result =
					await _multiplayerSessionService.BrowseSessions();

				if (!result.IsSuccess)
				{
					TLNLogger.LogError(result.Message);
					SetSessionsStatus(result.Message);
					return;
				}

				PopulateSessionList(result.Value);
			}
			finally
			{
				SetSessionBrowseInProgress(false);
			}
		}

		private void PopulateSessionList(IReadOnlyList<MultiplayerSessionInfo> sessions)
		{
			_sessionsList?.Clear();

			if (_sessionsList == null)
			{
				return;
			}

			if (sessions == null || sessions.Count == 0)
			{
				SetSessionsStatus("No public games found.");
				return;
			}

			SetSessionsStatus(string.Empty);

			for (int i = 0; i < sessions.Count; i++)
			{
				MultiplayerSessionInfo session = sessions[i];
				Button sessionButton = new Button(() => OnSessionClicked(session))
				{
					text = CreateSessionButtonText(session)
				};

				sessionButton.AddToClassList("main-menu-session-button");
				sessionButton.SetEnabled(!_isMultiplayerOperationInProgress);
				sessionButton.EnableInClassList(DisabledClassName, _isMultiplayerOperationInProgress);

				_sessionsList.Add(sessionButton);
			}
		}

		private async void OnSessionClicked(MultiplayerSessionInfo session)
		{
			if (_isMultiplayerOperationInProgress)
			{
				return;
			}

			if (_multiplayerSessionService == null)
			{
				TLNLogger.LogError("Cannot join game because multiplayer session service is missing.");
				return;
			}

			if (string.IsNullOrWhiteSpace(session.Id))
			{
				TLNLogger.LogError("Cannot join game because selected session id is empty.");
				return;
			}

			SetMultiplayerOperationInProgress(true);

			try
			{
				OperationResult result = await _multiplayerSessionService.JoinSessionById(session.Id);
				if (!result.IsSuccess)
				{
					TLNLogger.LogError(result.Message);
					RestoreMainMenuAfterMultiplayerOperationFailure();
					return;
				}

				await _sceneLoader.LoadWorld();
			}
			finally
			{
				if (_gameStateMachine != null &&
				    _gameStateMachine.IsCurrent(GameStateId.MainMenu))
				{
					SetMultiplayerOperationInProgress(false);
				}
			}
		}

		private static string CreateSessionButtonText(MultiplayerSessionInfo session)
		{
			string passwordSuffix = session.HasPassword ? " - PASSWORD" : string.Empty;

			return $"{session.Name}\n{session.PlayerCount}/{session.MaxPlayers} players{passwordSuffix}";
		}

		private void SetSessionsStatus(string status)
		{
			if (_sessionsStatusLabel == null)
			{
				return;
			}

			_sessionsStatusLabel.text = status ?? string.Empty;
			_sessionsStatusLabel.SetVisible(!string.IsNullOrWhiteSpace(status));
		}

		private async void OnHostGameClicked()
		{
			if (_isMultiplayerOperationInProgress)
			{
				return;
			}

			if (_multiplayerSessionService == null)
			{
				TLNLogger.LogError("Cannot host game because multiplayer session service is missing.");
				return;
			}

			SetMultiplayerOperationInProgress(true);

			try
			{
				OperationResult<string> result = await _multiplayerSessionService.CreateHostSession();
				if (!result.IsSuccess)
				{
					TLNLogger.LogError(result.Message);
					RestoreMainMenuAfterMultiplayerOperationFailure();
					return;
				}

				TLNLogger.Log($"Hosted online session. Join code: {result.Value}");

				_saveSessionService.StartNewGame(1);
				await _sceneLoader.LoadWorld();
			}
			finally
			{
				if (_gameStateMachine != null &&
				    _gameStateMachine.IsCurrent(GameStateId.MainMenu))
				{
					SetMultiplayerOperationInProgress(false);
				}
			}
		}

		private async void OnJoinGameClicked()
		{
			if (_isMultiplayerOperationInProgress)
			{
				return;
			}

			if (_multiplayerSessionService == null)
			{
				TLNLogger.LogError("Cannot join game because multiplayer session service is missing.");
				return;
			}

			string joinCode = _joinCodeField.value?.Trim();

			SetMultiplayerOperationInProgress(true);

			try
			{
				OperationResult result = await _multiplayerSessionService.JoinSessionByCode(joinCode);
				if (!result.IsSuccess)
				{
					TLNLogger.LogError(result.Message);
					RestoreMainMenuAfterMultiplayerOperationFailure();
					return;
				}

				await _sceneLoader.LoadWorld();
			}
			finally
			{
				if (_gameStateMachine != null &&
				    _gameStateMachine.IsCurrent(GameStateId.MainMenu))
				{
					SetMultiplayerOperationInProgress(false);
				}
			}
		}

		private void SetMultiplayerOperationInProgress(bool isInProgress)
		{
			_isMultiplayerOperationInProgress = isInProgress;

			if (isInProgress)
			{
				_gameStateMachine?.Enter(GameStateId.Loading);
			}

			if (_hostGameButton != null)
			{
				_hostGameButton.SetEnabled(!isInProgress);
				_hostGameButton.EnableInClassList(DisabledClassName, isInProgress);
			}

			if (_joinCodeField != null)
			{
				_joinCodeField.SetEnabled(!isInProgress);
			}

			if (_refreshGamesButton != null)
			{
				bool canRefresh = !isInProgress && !_isSessionBrowseInProgress;
				_refreshGamesButton.SetEnabled(canRefresh);
				_refreshGamesButton.EnableInClassList(DisabledClassName, !canRefresh);
			}

			SetSessionButtonsEnabled(!isInProgress);

			if (_multiplayerBackButton != null)
			{
				_multiplayerBackButton.SetEnabled(!isInProgress);
				_multiplayerBackButton.EnableInClassList(DisabledClassName, isInProgress);
			}

			RefreshJoinGameButton();
		}

		private void SetSessionBrowseInProgress(bool isInProgress)
		{
			_isSessionBrowseInProgress = isInProgress;

			if (_refreshGamesButton != null)
			{
				bool canRefresh = !isInProgress && !_isMultiplayerOperationInProgress;
				_refreshGamesButton.SetEnabled(canRefresh);
				_refreshGamesButton.EnableInClassList(DisabledClassName, !canRefresh);
			}
		}

		private void SetSessionButtonsEnabled(bool isEnabled)
		{
			if (_sessionsList == null)
			{
				return;
			}

			for (int i = 0; i < _sessionsList.childCount; i++)
			{
				if (_sessionsList[i] is not Button sessionButton)
				{
					continue;
				}

				sessionButton.SetEnabled(isEnabled);
				sessionButton.EnableInClassList(DisabledClassName, !isEnabled);
			}
		}

		private void RestoreMainMenuAfterMultiplayerOperationFailure()
		{
			_gameStateMachine?.Enter(GameStateId.MainMenu);
			SetMultiplayerOperationInProgress(false);
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
			_singlePlayerPanel.SetVisible(false);
			_multiplayerPanel.SetVisible(false);
			_settingsPanel.SetVisible(false);
			_saveSlotsPanel?.Hide();
		}

		private void ShowSinglePlayerPanel()
		{
			_navigationPanel.SetVisible(false);
			_singlePlayerPanel.SetVisible(true);
			_multiplayerPanel.SetVisible(false);
			_settingsPanel.SetVisible(false);
			_saveSlotsPanel?.Hide();
		}

		private void ShowMultiplayerPanel()
		{
			_navigationPanel.SetVisible(false);
			_singlePlayerPanel.SetVisible(false);
			_multiplayerPanel.SetVisible(true);
			_settingsPanel.SetVisible(false);
			_saveSlotsPanel?.Hide();
			RefreshJoinGameButton();
			_ = RefreshAvailableSessions();
		}

		private void ShowSettingsPanel()
		{
			_navigationPanel.SetVisible(false);
			_singlePlayerPanel.SetVisible(false);
			_multiplayerPanel.SetVisible(false);
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
				ShowNavigationPanel
			);
		}

		private void OnSinglePlayerClicked()
		{
			ShowSinglePlayerPanel();
		}

		private void OnMultiplayerClicked()
		{
			ShowMultiplayerPanel();
		}

		private void OnOptionsClicked()
		{
			ShowSettingsPanel();
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
			_singlePlayerPanel.SetVisible(false);
			_multiplayerPanel.SetVisible(false);
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
			_singlePlayerPanel.SetVisible(false);
			_multiplayerPanel.SetVisible(false);
			_settingsPanel.SetVisible(false);
			_saveSlotsPanel.ShowLoadGame();
		}

		private async void OnNewGameSlotSelected(int slotId)
		{
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

			UnityEngine.Cursor.visible = false;
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

			UnityEngine.Cursor.visible = false;
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
