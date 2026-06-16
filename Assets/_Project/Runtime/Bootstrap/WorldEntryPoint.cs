using Assign;
using TLN.Application.GameStates;
using TLN.Application.Notifications;
using TLN.Core.GameStates;
using TLN.Gameplay.Placement;
using TLN.Gameplay.Player;
using TLN.Gameplay.Survival;
using TLN.Gameplay.Time;
using TLN.UI.World;
using UnityEngine;
using VContainer;

namespace TLN.Gameplay.World
{
    public sealed class WorldEntryPoint : MonoBehaviour
    {
        [SerializeField, Assign(Mode.Scene)]
        private WorldUIRoot _uiRoot;

        [SerializeField, Assign(Mode.Scene)]
        private PlayerRoot _playerPrefab;

        [SerializeField, Assign(Mode.Scene)]
        private Transform _spawnPoint;

        private IGameStateMachine _gameStateMachine;
        private INotificationService _notificationService;
        private PlacementService _placementService;
        private IGameTimeService _gameTimeService;
        private ISurvivalService _survivalService;
        private IPlayerFactory _playerFactory;

        private PlayerRoot _playerInstance;

        [Inject]
        public void Construct(
            IGameStateMachine gameStateMachine,
            INotificationService notificationService,
            PlacementService placementService,
            IGameTimeService gameTimeService,
            ISurvivalService survivalService,
            IPlayerFactory playerFactory)
        {
            _gameStateMachine = gameStateMachine;
            _notificationService = notificationService;
            _placementService = placementService;
            _gameTimeService = gameTimeService;
            _survivalService = survivalService;
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
            SpawnPlayer();
            EnsureGameplayState();
        }

        private void ConstructHUD()
        {
            _notificationService.SetView(_uiRoot.HUD);

            _uiRoot.HUD.Construct(
                _survivalService,
                _gameTimeService);
        }

        private void SpawnPlayer()
        {
            _playerInstance = _playerFactory.CreatePlayer(
                _playerPrefab,
                _spawnPoint);

            _placementService.SetPlayerRoot(_playerInstance);
        }

        private void EnsureGameplayState()
        {
            if (_gameStateMachine == null)
            {
                return;
            }

            if (_gameStateMachine.CurrentState == GameStateId.Playing)
            {
                return;
            }

            _gameStateMachine.Enter(GameStateId.Playing);
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
                Debug.LogError(
                    "Some UI references in WorldUIRoot are missing.");

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
