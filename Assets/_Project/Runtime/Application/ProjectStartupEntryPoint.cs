using TLN.Application.GameStates;
using TLN.Application.Scenes;
using TLN.Core.GameStates;
using UnityEngine;
using VContainer.Unity;

namespace TLN.Application.App
{
	public sealed class ProjectStartupEntryPoint : IStartable
	{
		private const int DefaultTargetFrameRate = 60;

		private readonly IGameStateMachine _gameStateMachine;
		private readonly ISceneLoader _sceneLoader;

		public ProjectStartupEntryPoint(IGameStateMachine gameStateMachine, ISceneLoader sceneLoader)
		{
			_gameStateMachine = gameStateMachine;
			_sceneLoader = sceneLoader;
		}

		public void Start()
		{
			ApplyDefaultFrameSync();

			_gameStateMachine.Enter(GameStateId.Boot);
			_sceneLoader.LoadMainMenu();
		}

		private static void ApplyDefaultFrameSync()
		{
			QualitySettings.vSyncCount = 0;
			UnityEngine.Application.targetFrameRate = DefaultTargetFrameRate;
		}
	}
}
