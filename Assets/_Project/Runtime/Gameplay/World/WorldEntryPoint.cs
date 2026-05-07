using TLN.Application.App;
using TLN.Application.GameStates;
using TLN.Application.Input;
using TLN.Application.Scenes;
using TLN.Gameplay.Player;
using TLN.UI.Pause;
using UnityEngine;

namespace TLN.Gameplay.World
{
	public sealed class WorldEntryPoint : MonoBehaviour
	{
		[Header("Player")]
		[SerializeField] private PlayerRoot _playerPrefab;
		[SerializeField] private PlayerSpawnPoint _spawnPoint;

		[Header("UI")]
		[SerializeField] private PauseDebugView _pauseDebugView;

		private PlayerRoot _playerInstance;

		private void Awake()
		{
			if (_spawnPoint == null)
			{
				_spawnPoint = FindFirstObjectByType<PlayerSpawnPoint>();
			}

			ConstructPauseDebugView();
			SpawnPlayer();
		}

		private void ConstructPauseDebugView()
		{
			if (_pauseDebugView == null)
			{
				_pauseDebugView = FindFirstObjectByType<PauseDebugView>();
			}

			if (_pauseDebugView == null)
			{
				Debug.LogWarning("PauseDebugView was not found in World scene.");
				return;
			}

			IGameStateMachine gameStateMachine = AppRoot.Instance.Services.Resolve<IGameStateMachine>();
			ISceneLoader sceneLoader = AppRoot.Instance.Services.Resolve<ISceneLoader>();

			_pauseDebugView.Construct(gameStateMachine, sceneLoader);
		}

		private void SpawnPlayer()
		{
			if (_playerPrefab == null)
			{
				Debug.LogError("Player prefab is not assigned in WorldEntryPoint.");
				return;
			}

			if (_spawnPoint == null)
			{
				Debug.LogError("PlayerSpawnPoint was not found in World scene.");
				return;
			}

			_playerInstance = Instantiate(_playerPrefab, _spawnPoint.transform.position, _spawnPoint.transform.rotation);

			IInputModeService inputModeService = AppRoot.Instance.Services.Resolve<IInputModeService>();
			IGameStateMachine gameStateMachine = AppRoot.Instance.Services.Resolve<IGameStateMachine>();

			_playerInstance.Construct(inputModeService, gameStateMachine);
		}
	}
}
