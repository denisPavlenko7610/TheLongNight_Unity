using Assign;
using TLN.Application.GameStates;
using TLN.Application.Input;
using TLN.Application.Notifications;
using TLN.Application.Scenes;
using TLN.Gameplay.Inventory;
using TLN.Gameplay.Items;
using TLN.Gameplay.Placement;
using TLN.Gameplay.Player;
using TLN.Gameplay.Sleep;
using TLN.Gameplay.Survival;
using TLN.Gameplay.Time;
using TLN.UI.World;
using UnityEngine;
using VContainer;

namespace TLN.Gameplay.World
{
	public sealed class WorldEntryPoint : MonoBehaviour
	{
		[SerializeField, Assign(Mode.Scene)] private WorldUIRoot _uiRoot;
		[SerializeField, Assign(Mode.Scene)] private PlayerRoot _playerPrefab;
		[SerializeField, Assign(Mode.Scene)] private Transform _spawnPoint;

		private IGameStateMachine _gameStateMachine;
		private IInputModeService _inputModeService;
		private ISceneLoader _sceneLoader;
		private INotificationService _notificationService;

		private IInventoryService _inventoryService;
		private IItemUseService _itemUseService;
		private PlacementService _placementService;
		private IGameTimeService _gameTimeService;
		private ISurvivalService _survivalService;
		private SleepService _sleepService;
		private IPlayerFactory _playerFactory;

		private PlayerRoot _playerInstance;

		[Inject]
		public void Construct(
			IGameStateMachine gameStateMachine,
			IInputModeService inputModeService,
			ISceneLoader sceneLoader,
			INotificationService notificationService,
			IInventoryService inventoryService,
			IItemUseService itemUseService,
			PlacementService placementService,
			IGameTimeService gameTimeService,
			ISurvivalService survivalService,
			SleepService sleepService,
			IPlayerFactory playerFactory)
		{
			_gameStateMachine = gameStateMachine;
			_inputModeService = inputModeService;
			_sceneLoader = sceneLoader;
			_notificationService = notificationService;

			_inventoryService = inventoryService;
			_itemUseService = itemUseService;
			_placementService = placementService;
			_gameTimeService = gameTimeService;
			_survivalService = survivalService;
			_sleepService = sleepService;
			_playerFactory = playerFactory;
		}

		private void Start()
		{
			if (!ValidateRequiredReferences())
			{
				enabled = false;
				return;
			}

			ConstructHUD();
			ConstructPauseMenu();
			ConstructInventoryWindow();
			ConstructSleepWindow();
			SpawnPlayer();
		}

		private void ConstructHUD()
		{
			_notificationService.SetView(_uiRoot.HUD);
			_uiRoot.HUD.Construct(_survivalService, _gameTimeService);
		}

		private void ConstructPauseMenu()
		{
			_uiRoot.PauseMenu.Construct(_gameStateMachine, _inputModeService, _sceneLoader);
		}

		private void ConstructInventoryWindow()
		{
			_uiRoot.InventoryWindow.Construct(_inventoryService, _itemUseService);
		}

		private void ConstructSleepWindow()
		{
			_uiRoot.SleepWindow.Construct(_sleepService, _inputModeService, _inventoryService, _notificationService);
		}

		private void SpawnPlayer()
		{
			_playerInstance = _playerFactory.CreatePlayer(_playerPrefab, _spawnPoint);
			_placementService.SetPlayerRoot(_playerInstance);
		}

		private bool ValidateRequiredReferences()
		{
			if (_uiRoot == null)
			{
				Debug.LogError("WorldUIRoot is required.");
				return false;
			}

			if (!_uiRoot.HasAllRequiredReferences())
			{
				Debug.LogError("Some UI references in WorldUIRoot are missing.");
				return false;
			}

			if (_playerPrefab == null)
			{
				Debug.LogError("PlayerPrefab is required.");
				return false;
			}

			if (_spawnPoint == null)
			{
				Debug.LogError("Player spawn point is required.");
				return false;
			}

			return true;
		}
	}
}
