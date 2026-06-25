using TLN.Application.GameStates;
using TLN.Application.Scenes;
using UnityEngine;
using VContainer.Unity;

namespace TLN.Bootstrap
{
	public sealed class ProjectStartupEntryPoint : IStartable
	{
	private const int DefaultTargetFrameRate = -1;
	private const int DisableVSync = 0;

		private readonly IGameStateMachine _gameStateMachine;
		private readonly ISceneLoader _sceneLoader;

		public ProjectStartupEntryPoint(IGameStateMachine gameStateMachine, ISceneLoader sceneLoader)
		{
			_gameStateMachine = gameStateMachine;
			_sceneLoader = sceneLoader;
		}

		public async void Start()
		{
			ApplyDefaultFrameSync();

			_gameStateMachine.Enter(GameStateId.Boot);
			await _sceneLoader.LoadMainMenu();
		}

		private static void ApplyDefaultFrameSync()
		{
			QualitySettings.vSyncCount = DisableVSync;
			UnityEngine.Application.targetFrameRate = DefaultTargetFrameRate;
		}
	}
}
