using TLN.Application.Scenes;
using TLN.Core.Lifetime;

namespace TLN.Application.App
{
	public sealed class BootStartupService : IGameService
	{
		private readonly ISceneLoader _sceneLoader;

		public BootStartupService(ISceneLoader sceneLoader)
		{
			_sceneLoader = sceneLoader;
		}

		public void Start()
		{
			_sceneLoader.LoadMainMenu();
		}
	}
}
