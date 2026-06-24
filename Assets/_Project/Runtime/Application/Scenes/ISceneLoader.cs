using TLN.Core.Lifetime;
using UnityEngine;

namespace TLN.Application.Scenes
{
	public interface ISceneLoader : IGameService
	{
		bool IsLoading { get; }

		Awaitable LoadMainMenu();
		Awaitable LoadWorld();
	}
}
