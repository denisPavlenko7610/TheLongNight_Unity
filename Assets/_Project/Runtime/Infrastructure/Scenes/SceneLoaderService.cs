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
			if (_isLoading)
			{
				TLNLogger.LogWarning("Cannot load MainMenu because another scene is already loading.");
				return;
			}

			_isLoading = true;
			_gameStateMachine.Enter(GameStateId.Loading);

			try
			{
				await LoadSceneAsync(SceneNames.MainMenu, GameStateId.MainMenu);
			}
			catch (Exception exception)
			{
				TLNLogger.LogError($"Failed to load MainMenu: {exception}");
				_isLoading = false;
			}
		}

		public async Awaitable LoadWorld()
		{
			if (_isLoading)
			{
				TLNLogger.LogWarning("Cannot load World because another scene is already loading.");
				return;
			}

			_isLoading = true;
			_gameStateMachine.Enter(GameStateId.Loading);

			try
			{
				await LoadSceneAsync(SceneNames.World, GameStateId.Playing);
			}
			catch (Exception exception)
			{
				TLNLogger.LogError($"Failed to load World: {exception}");
				_isLoading = false;
			}
		}

		private async Awaitable LoadSceneAsync(string sceneName, GameStateId stateAfterLoading)
		{
			await SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);

			_isLoading = false;

			_gameStateMachine.Enter(stateAfterLoading);
		}
	}
}
