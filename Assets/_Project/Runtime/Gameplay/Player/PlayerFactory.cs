using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace TLN.Gameplay.Player
{
	public sealed class PlayerFactory : IPlayerFactory
	{
		private readonly IObjectResolver _resolver;

		public PlayerFactory(IObjectResolver resolver)
		{
			_resolver = resolver;
		}

		public PlayerRoot CreatePlayer(PlayerRoot prefab, Transform spawnPoint)
		{
			return _resolver.Instantiate(prefab, spawnPoint.position, spawnPoint.rotation);
		}
	}

	public interface IPlayerFactory
	{
		PlayerRoot CreatePlayer(PlayerRoot prefab, Transform spawnPoint);
	}
}