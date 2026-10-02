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
			_gameStateMachine = gameStateMachine ?? throw new ArgumentNullException(nameof(gameStateMachine));
		}

		public async Awaitable LoadMainMenu()
		{
			await LoadRegularScene(SceneNames.MainMenu, GameStateId.MainMenu);
		}

		public async Awaitable LoadWorld()
		{
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

			_gameStateMachine.Enter(stateAfterLoading);
		}

		private void FailLoading(string sceneName, Exception exception)
		{
			TLNLogger.LogError($"Failed to load {sceneName}: {exception}");

			_isLoading = false;
		}
	}
}
