using TLN.Application.GameStates;
using TLN.Core.GameStates;
using UnityEngine;
using VContainer.Unity;

namespace TLN.Application.App
{
	public sealed class ProjectStartupEntryPoint : IStartable
	{
		private readonly IGameStateMachine _gameStateMachine;
		private readonly BootStartupService _bootStartupService;

		public ProjectStartupEntryPoint(IGameStateMachine gameStateMachine, BootStartupService bootStartupService)
		{
			_gameStateMachine = gameStateMachine;
			_bootStartupService = bootStartupService;
		}

		public void Start()
		{
			ApplyDefaultFrameSync();
			_gameStateMachine.Enter(GameStateId.Boot);
			_bootStartupService.Start();
		}

		private static void ApplyDefaultFrameSync()
		{
			QualitySettings.vSyncCount = 1;
			UnityEngine.Application.targetFrameRate = -1;
		}
	}
}
