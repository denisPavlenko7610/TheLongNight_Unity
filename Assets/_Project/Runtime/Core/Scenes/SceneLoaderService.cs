using System.Collections;
using TLN.Application.GameStates;
using TLN.Core.GameStates;
using TLN.Core.Scenes;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TLN.Application.Scenes
{
	public sealed class SceneLoaderService : ISceneLoader
	{
		private readonly IGameStateMachine _gameStateMachine;
		private readonly MonoBehaviour _coroutineRunner;

		private Coroutine _loadingCoroutine;

		public bool IsLoading => _loadingCoroutine != null;

		public SceneLoaderService(IGameStateMachine gameStateMachine, MonoBehaviour coroutineRunner)
		{
			_gameStateMachine = gameStateMachine;
			_coroutineRunner = coroutineRunner;
		}

		public void LoadMainMenu()
		{
			if (IsLoading)
			{
				Debug.LogWarning("Cannot load MainMenu because another scene is already loading.");
				return;
			}

			_loadingCoroutine = _coroutineRunner.StartCoroutine(LoadSceneRoutine(SceneNames.MainMenu, GameStateId.MainMenu));
		}

		public void LoadWorld()
		{
			if (IsLoading)
			{
				Debug.LogWarning("Cannot load World because another scene is already loading.");
				return;
			}

			_gameStateMachine.Enter(GameStateId.Loading);

			_loadingCoroutine = _coroutineRunner.StartCoroutine(LoadSceneRoutine(
				SceneNames.World,
				GameStateId.Playing));
		}

		private IEnumerator LoadSceneRoutine(string sceneName, GameStateId stateAfterLoading)
		{
			AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);

			while (!operation.isDone)
			{
				yield return null;
			}

			_loadingCoroutine = null;

			_gameStateMachine.Enter(stateAfterLoading);
		}
	}
}
