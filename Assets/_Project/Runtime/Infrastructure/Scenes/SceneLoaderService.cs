using System;
using TLN.Application.GameStates;
using TLN.Application.Multiplayer;
using TLN.Application.Scenes;
using TLN.Core.Logging;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TLN.Infrastructure.Scenes
{
	public sealed class SceneLoaderService : ISceneLoader
	{
		private readonly IGameStateMachine _gameStateMachine;
		private readonly IMultiplayerSessionService _multiplayerSessionService;
		private readonly NetworkManager _networkManager;

		private bool _isLoading;
		private bool _isSubscribedToNetworkSceneEvents;

		private string _pendingNetworkSceneName;
		private GameStateId _pendingNetworkStateAfterLoading;
		private AwaitableCompletionSource<bool> _networkSceneCompletion;

		public bool IsLoading => _isLoading;

		public SceneLoaderService(
			IGameStateMachine gameStateMachine,
			IMultiplayerSessionService multiplayerSessionService,
			NetworkManager networkManager
		)
		{
			_gameStateMachine = gameStateMachine ?? throw new ArgumentNullException(nameof(gameStateMachine));
			_multiplayerSessionService = multiplayerSessionService;
			_networkManager = networkManager;
		}

		public async Awaitable LoadMainMenu()
		{
			await ShutdownNetworkIfNeeded();
			await LoadRegularScene(SceneNames.MainMenu, GameStateId.MainMenu);
		}

		public async Awaitable LoadWorld()
		{
			if (ShouldUseNetworkSceneLoading(SceneNames.World))
			{
				await LoadNetworkScene(SceneNames.World, GameStateId.Playing);
				return;
			}

			if (ShouldWaitForNetworkSceneSynchronization(SceneNames.World))
			{
				WaitForNetworkSceneSynchronization(SceneNames.World, GameStateId.Playing);
				return;
			}

			await LoadRegularScene(SceneNames.World, GameStateId.Playing);
		}

		private async Awaitable LoadRegularScene(
			string sceneName,
			GameStateId stateAfterLoading
		)
		{
			if (!TryBeginLoading(sceneName))
			{
				return;
			}

			try
			{
				await SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
				ShaderPreloader.PrewarmSnowShaders();
				CompleteLoading(stateAfterLoading);
			}
			catch (Exception exception)
			{
				FailLoading(sceneName, exception);
			}
		}

		private async Awaitable LoadNetworkScene(
			string sceneName,
			GameStateId stateAfterLoading
		)
		{
			if (!TryBeginLoading(sceneName))
			{
				return;
			}

			try
			{
				EnsureNetworkSceneEventsSubscribed();

				_pendingNetworkSceneName = sceneName;
				_pendingNetworkStateAfterLoading = stateAfterLoading;
				_networkSceneCompletion = new AwaitableCompletionSource<bool>();

				SceneEventProgressStatus status =
					_networkManager.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);

				if (status != SceneEventProgressStatus.Started)
				{
					throw new InvalidOperationException(
						$"Network scene load did not start. Scene: {sceneName}. Status: {status}"
					);
				}

				await _networkSceneCompletion.Awaitable;
			}
			catch (Exception exception)
			{
				FailLoading(sceneName, exception);
			}
		}

		private void WaitForNetworkSceneSynchronization(
			string sceneName,
			GameStateId stateAfterLoading
		)
		{
			if (!TryBeginLoading(sceneName))
			{
				return;
			}

			EnsureNetworkSceneEventsSubscribed();

			_pendingNetworkSceneName = sceneName;
			_pendingNetworkStateAfterLoading = stateAfterLoading;

			if (SceneManager.GetActiveScene().name == sceneName)
			{
				CompleteLoading(stateAfterLoading);
			}
		}

		private bool TryBeginLoading(string sceneName)
		{
			if (_isLoading)
			{
				TLNLogger.LogWarning(
					$"Cannot load {sceneName} because another scene is already loading."
				);

				return false;
			}

			_isLoading = true;
			_gameStateMachine.Enter(GameStateId.Loading);

			return true;
		}

		private void CompleteLoading(GameStateId stateAfterLoading)
		{
			ShaderPreloader.PrewarmSnowShaders();

			_isLoading = false;
			_pendingNetworkSceneName = string.Empty;
			_pendingNetworkStateAfterLoading = GameStateId.None;
			_networkSceneCompletion = null;

			_gameStateMachine.Enter(stateAfterLoading);
		}

		private void FailLoading(string sceneName, Exception exception)
		{
			TLNLogger.LogError($"Failed to load {sceneName}: {exception}");

			_isLoading = false;
			_pendingNetworkSceneName = string.Empty;
			_pendingNetworkStateAfterLoading = GameStateId.None;
			_networkSceneCompletion = null;
		}

		private bool ShouldUseNetworkSceneLoading(string sceneName)
		{
			return sceneName == SceneNames.World &&
			       _networkManager != null &&
			       _networkManager.IsListening &&
			       _networkManager.IsServer &&
			       _networkManager.SceneManager != null;
		}

		private bool ShouldWaitForNetworkSceneSynchronization(string sceneName)
		{
			return sceneName == SceneNames.World &&
			       _networkManager != null &&
			       _networkManager.IsListening &&
			       _networkManager.IsClient &&
			       !_networkManager.IsServer &&
			       _networkManager.SceneManager != null;
		}

		private void EnsureNetworkSceneEventsSubscribed()
		{
			if (_isSubscribedToNetworkSceneEvents)
			{
				return;
			}

			if (_networkManager == null || _networkManager.SceneManager == null)
			{
				return;
			}

			_networkManager.SceneManager.OnSceneEvent += OnNetworkSceneEvent;
			_isSubscribedToNetworkSceneEvents = true;
		}

		private void OnNetworkSceneEvent(SceneEvent sceneEvent)
		{
			if (string.IsNullOrWhiteSpace(_pendingNetworkSceneName))
			{
				return;
			}

			if (sceneEvent.SceneName != _pendingNetworkSceneName)
			{
				return;
			}

			if (_networkManager != null && _networkManager.IsServer)
			{
				HandleServerNetworkSceneEvent(sceneEvent);
				return;
			}

			HandleClientNetworkSceneEvent(sceneEvent);
		}

		private void HandleServerNetworkSceneEvent(SceneEvent sceneEvent)
		{
			if (sceneEvent.SceneEventType != SceneEventType.LoadEventCompleted)
			{
				return;
			}

			_networkSceneCompletion?.SetResult(true);
			CompleteLoading(_pendingNetworkStateAfterLoading);
		}

		private void HandleClientNetworkSceneEvent(SceneEvent sceneEvent)
		{
			if (sceneEvent.SceneEventType != SceneEventType.LoadComplete &&
			    sceneEvent.SceneEventType != SceneEventType.SynchronizeComplete)
			{
				return;
			}

			CompleteLoading(_pendingNetworkStateAfterLoading);
		}

		private async Awaitable ShutdownNetworkIfNeeded()
		{
			if (_multiplayerSessionService != null && _multiplayerSessionService.IsMultiplayer)
			{
				await _multiplayerSessionService.ShutdownAsync();
				return;
			}

			if (_networkManager == null)
			{
				return;
			}

			if (!_networkManager.IsListening)
			{
				return;
			}

			_networkManager.Shutdown();
			await WaitForNetworkShutdown();
		}

		private async Awaitable WaitForNetworkShutdown()
		{
			if (_networkManager == null)
			{
				return;
			}

			for (int i = 0; i < 120 && _networkManager.IsListening; i++)
			{
				await Awaitable.NextFrameAsync();
			}
		}
	}
}
