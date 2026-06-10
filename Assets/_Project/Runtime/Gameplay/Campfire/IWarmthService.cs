using TLN.Core.Lifetime;
using UnityEngine;

namespace TLN.Gameplay.Campfire
{
	public interface IWarmthService : IGameService
	{
		void Register(IWarmthProvider provider);
		void Unregister(IWarmthProvider provider);
		float GetWarmthAt(Vector3 position);
	}
}
