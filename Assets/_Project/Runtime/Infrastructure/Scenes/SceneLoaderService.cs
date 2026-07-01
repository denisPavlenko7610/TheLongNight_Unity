using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TLN.Application.GameStates;
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
		private readonly NetworkManager _networkManager;
		private bool _isLoading;

		public bool IsLoading => _isLoading;

		public SceneLoaderService(IGameStateMachine gameStateMachine, NetworkManager networkManager)
		{
			_gameStateMachine = gameStateMachine;
			_networkManager = networkManager;
		}

		public async Awaitable LoadMainMenu()
		{
			await LoadScene(SceneNames.MainMenu, GameStateId.MainMenu);
		}

		public async Awaitable LoadWorld()
		{
			await LoadScene(SceneNames.World, GameStateId.Playing);
		}

		private async Awaitable LoadScene(string sceneName, GameStateId stateAfterLoading)
		{
			if (_isLoading)
			{
				TLNLogger.LogWarning($"Cannot load {sceneName} because another scene is already loading.");
				return;
			}

			_isLoading = true;
			_gameStateMachine.Enter(GameStateId.Loading);

			try
			{
				if (ShouldWaitForNetworkSceneSynchronization(sceneName))
				{
					if (SceneManager.GetActiveScene().name == sceneName)
					{
						_gameStateMachine.Enter(stateAfterLoading);
					}

					_isLoading = false;
					return;
				}

				if (ShouldUseNetworkSceneLoading(sceneName))
				{
					await LoadNetworkSceneAsync(sceneName, stateAfterLoading);
					return;
				}

				await LoadSceneAsync(sceneName, stateAfterLoading);
			}
			catch (Exception exception)
			{
				TLNLogger.LogError($"Failed to load {sceneName}: {exception}");
				_isLoading = false;
			}
		}

		private async Awaitable LoadSceneAsync(string sceneName, GameStateId stateAfterLoading)
		{
			await SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);

			ShaderPreloader.PrewarmSnowShaders();

			_isLoading = false;

			_gameStateMachine.Enter(stateAfterLoading);
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
			       !_networkManager.IsServer;
		}

		private async Awaitable LoadNetworkSceneAsync(string sceneName, GameStateId stateAfterLoading)
		{
			TaskCompletionSource<bool> loadCompletion = new();

			void OnLoadEventCompleted(
				string loadedSceneName,
				LoadSceneMode loadSceneMode,
				List<ulong> clientsCompleted,
				List<ulong> clientsTimedOut
			)
			{
				if (loadedSceneName != sceneName ||
				    loadSceneMode != LoadSceneMode.Single)
				{
					return;
				}

				if (clientsTimedOut is { Count: > 0 })
				{
					TLNLogger.LogWarning(
						$"Network scene load for {sceneName} completed with {clientsTimedOut.Count} timed out clients."
					);
				}

				loadCompletion.TrySetResult(true);
			}

			_networkManager.SceneManager.OnLoadEventCompleted += OnLoadEventCompleted;

			try
			{
				SceneEventProgressStatus status = _networkManager.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
				if (status != SceneEventProgressStatus.Started)
				{
					throw new InvalidOperationException(
						$"Network scene load for {sceneName} failed to start: {status}."
					);
				}

				await loadCompletion.Task;

				ShaderPreloader.PrewarmSnowShaders();

				_isLoading = false;
				_gameStateMachine.Enter(stateAfterLoading);
			}
			finally
			{
				if (_networkManager != null && _networkManager.SceneManager != null)
				{
					_networkManager.SceneManager.OnLoadEventCompleted -= OnLoadEventCompleted;
				}
			}
		}
	}
}
