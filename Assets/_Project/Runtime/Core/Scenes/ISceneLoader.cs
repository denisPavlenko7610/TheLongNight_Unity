using TLN.Core.Lifetime;

namespace TLN.Application.Scenes
{
	public interface ISceneLoader : IGameService
	{
		bool IsLoading { get; }

		void LoadMainMenu();
		void LoadWorld();
	}
}
