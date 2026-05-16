using Assign;
using TLN.Application.GameStates;
using TLN.Application.Input;
using TLN.Application.Notifications;
using TLN.Application.Scenes;
using TLN.Core.Logging;
using TLN.Gameplay.Interaction;
using TLN.Gameplay.Inventory;
using TLN.Gameplay.Items;
using TLN.Gameplay.Player;
using TLN.Gameplay.Sleep;
using TLN.Gameplay.Survival;
using TLN.Gameplay.Time;
using TLN.Gameplay.World;
using TLN.UI.HUD;
using TLN.UI.Inventory;
using TLN.UI.Pause;
using TLN.UI.Sleep;
using UnityEngine;

namespace TLN.Bootstrap.App
{
	public sealed class WorldEntryPoint : MonoBehaviour
	{
		[Header("Player")]
		[SerializeField] private PlayerRoot _playerPrefab;
		[SerializeField][Assign(Mode.Scene)] private PlayerSpawnPoint _spawnPoint;

		[Header("UI")]
		[SerializeField][Assign(Mode.Scene)] private PauseDebugView _pauseDebugView;
		[SerializeField][Assign(Mode.Scene)] private InteractionPromptView _interactionPromptView;
		[SerializeField][Assign(Mode.Scene)] private InventoryDebugView _inventoryDebugView;
		[SerializeField][Assign(Mode.Scene)] private WorldHUDView _worldHUDView;

		[Header("Survival")]
		[SerializeField][Assign(Mode.Scene)] private SurvivalConfig _survivalConfig;
		[SerializeField][Assign(Mode.Scene)] private WorldSurvivalController _survivalController;

		[Header("Sleep")]
		[SerializeField][Assign(Mode.Scene)] private SleepConfig _sleepConfig;
		[SerializeField][Assign(Mode.Scene)] private SleepDebugView _sleepDebugView;

		[Header("Time")]
		[SerializeField][Assign(Mode.Scene)] private GameTimeConfig _gameTimeConfig;
		[SerializeField][Assign(Mode.Scene)] private WorldTimeController _timeController;

		private IGameTimeService _gameTimeService;

		private SleepService _sleepService;

		private SurvivalWarningService _survivalWarningService;
		private ISurvivalService _survivalService;
		private IInventoryService _inventoryService;

		private ItemUseService _itemUseService;
		private PlayerRoot _playerInstance;
		private IInputModeService _inputModeService;
		private IGameStateMachine _gameStateMachine;
		private ISceneLoader _sceneLoader;
		private NotificationService _notificationConcreteService;
		private INotificationService _notificationServiceInterface;

		private void Awake()
		{
			if (!ValidateRequiredReferences())
			{
				return;
			}
			ResolveGameplayServices();

			ConstructNotificationView();

			CreateGameTimeService();
			CreateSurvivalService();
			ConstructHUD();
			CreateItemUseService();
			CreateSleepService();

			ConstructGameTime();
			ConstructPauseDebugView();
			ConstructInventoryDebugView();
			ConstructSurvival();
			ConstructSleep();
			ConstructWorldItems();

			SpawnPlayer();
		}

		private void ResolveGameplayServices()
		{
			_notificationServiceInterface = AppRoot.Instance.Services.Resolve<INotificationService>();
			_sceneLoader = AppRoot.Instance.Services.Resolve<ISceneLoader>();
			_inventoryService = AppRoot.Instance.Services.Resolve<IInventoryService>();
			_notificationConcreteService = AppRoot.Instance.Services.Resolve<NotificationService>();
			_inputModeService = AppRoot.Instance.Services.Resolve<IInputModeService>();
			_gameStateMachine = AppRoot.Instance.Services.Resolve<IGameStateMachine>();
		}

		private void ConstructPauseDebugView()
		{
			if (_pauseDebugView == null) {
				_pauseDebugView = FindFirstObjectByType<PauseDebugView>();
			}

			_pauseDebugView.Construct(_gameStateMachine, _sceneLoader);
		}

		private void CreateGameTimeService()
		{
			_gameTimeService = new GameTimeService(_gameTimeConfig);
		}

		private void ConstructGameTime()
		{
			IGameStateMachine gameStateMachine = AppRoot.Instance.Services.Resolve<IGameStateMachine>();
			_timeController.Construct(_gameTimeService, gameStateMachine);
		}

		private void CreateItemUseService()
		{
			_itemUseService = new ItemUseService(_inventoryService, _survivalService, _notificationConcreteService);
		}

		private void CreateSleepService()
		{
			_sleepService = new SleepService(_sleepConfig, _survivalService, _notificationConcreteService, _gameTimeService);
		}

		private void ConstructSleep()
		{
			_sleepDebugView.Construct(_sleepService, _inputModeService);

			BedrollActor[] bedrolls = FindObjectsByType<BedrollActor>(FindObjectsSortMode.None);

			for (int i = 0; i < bedrolls.Length; i++)
			{
				bedrolls[i].Construct(_sleepDebugView);
			}
		}

		private void CreateSurvivalService()
		{
			_survivalService = new SurvivalService(_survivalConfig);
			_survivalWarningService = new SurvivalWarningService(_survivalService, _notificationConcreteService, warningCooldownSeconds: 10f);
		}

		private void ConstructHUD()
		{
			_notificationConcreteService.SetView(_worldHUDView);
			_worldHUDView.Construct(_survivalService, _gameTimeService);
		}

		private void ConstructSurvival()
		{
			_survivalController.Construct(_survivalService, _survivalWarningService, _gameStateMachine);
		}

		private void ConstructNotificationView()
		{
			_notificationConcreteService.SetView(_worldHUDView);
		}

		private void SpawnPlayer()
		{
			_playerInstance = Instantiate(_playerPrefab, _spawnPoint.transform.position, _spawnPoint.transform.rotation);
			_playerInstance.Construct(_inputModeService, _gameStateMachine, _worldHUDView, _inventoryDebugView, _worldHUDView);
		}

		private void ConstructWorldItems()
		{
			WorldItemActor[] worldItems = FindObjectsByType<WorldItemActor>(FindObjectsSortMode.None);

			for (int i = 0; i < worldItems.Length; i++) {
				worldItems[i].Construct(_inventoryService, _notificationConcreteService);
			}
		}

		private void ConstructInventoryDebugView()
		{
			_inventoryDebugView.Construct(_inventoryService, _itemUseService);
		}

		private bool ValidateRequiredReferences()
		{
			bool isValid = true;

			if (_playerPrefab == null)
			{
				TLNLogger.Error("Player prefab is required.", this);
				isValid = false;
			}

			if (_spawnPoint == null)
			{
				TLNLogger.Error("Player spawn point is required.", this);
				isValid = false;
			}

			if (_worldHUDView == null)
			{
				TLNLogger.Error("WorldHUDView is required.", this);
				isValid = false;
			}

			if (_survivalConfig == null)
			{
				TLNLogger.Error("SurvivalConfig is required.", this);
				isValid = false;
			}

			if (_gameTimeConfig == null)
			{
				TLNLogger.Error("GameTimeConfig is required.", this);
				isValid = false;
			}

			if (_sleepConfig == null)
			{
				TLNLogger.Error("SleepConfig is required.", this);
				isValid = false;
			}

			return isValid;
		}
	}
}
