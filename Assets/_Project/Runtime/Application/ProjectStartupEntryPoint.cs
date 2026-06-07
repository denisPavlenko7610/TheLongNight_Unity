using TLN.Application.GameStates;
using TLN.Core.GameStates;
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
			_gameStateMachine.Enter(GameStateId.Boot);
			_bootStartupService.Start();
		}
	}
}
