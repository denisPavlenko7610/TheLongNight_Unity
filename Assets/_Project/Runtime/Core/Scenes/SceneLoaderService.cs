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

			_gameStateMachine.Enter(GameStateId.Loading);
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

		public void LoadSceneForDevelopment(string scenePathOrName)
		{
			if (IsLoading)
			{
				Debug.LogWarning($"Cannot load {scenePathOrName} because another scene is already loading.");
				return;
			}

			if (string.IsNullOrWhiteSpace(scenePathOrName))
			{
				Debug.LogWarning("Cannot load development scene because scene path or name is empty.");
				return;
			}

			string sceneName = GetSceneNameFromPathOrName(scenePathOrName);
			GameStateId stateAfterLoading = GetStateAfterDevelopmentSceneLoad(sceneName);

			_gameStateMachine.Enter(GameStateId.Loading);

			_loadingCoroutine = _coroutineRunner.StartCoroutine(LoadSceneRoutine(sceneName, stateAfterLoading));
		}

		private static string GetSceneNameFromPathOrName(string scenePathOrName)
		{
			if (!scenePathOrName.EndsWith(".unity"))
			{
				return scenePathOrName;
			}

			int lastSlashIndex = scenePathOrName.LastIndexOf('/');
			int startIndex = lastSlashIndex >= 0 ? lastSlashIndex + 1 : 0;

			string fileName = scenePathOrName[startIndex..];
			return fileName.Replace(".unity", string.Empty);
		}

		private static GameStateId GetStateAfterDevelopmentSceneLoad(string sceneName)
		{
			return sceneName == SceneNames.MainMenu
				? GameStateId.MainMenu
				: GameStateId.Playing;
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
