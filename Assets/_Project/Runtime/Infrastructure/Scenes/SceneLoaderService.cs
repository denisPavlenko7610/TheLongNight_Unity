using System;
using TLN.Application.GameStates;
using TLN.Application.Scenes;
using TLN.Core.Logging;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TLN.Infrastructure.Scenes
{
	public sealed class SceneLoaderService : ISceneLoader
	{
		private readonly IGameStateMachine _gameStateMachine;
		private bool _isLoading;

		public bool IsLoading => _isLoading;

		public SceneLoaderService(IGameStateMachine gameStateMachine)
		{
			_gameStateMachine = gameStateMachine;
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
	}
}
