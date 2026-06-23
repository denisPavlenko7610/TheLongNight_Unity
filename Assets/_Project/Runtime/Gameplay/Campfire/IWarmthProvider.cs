using UnityEngine;

namespace TLN.Gameplay.Campfire
{
	public interface IWarmthProvider
	{
		bool IsWarmthActive { get; }
		float WarmthBonus { get; }
		float WarmthRadius { get; }
		Vector3 Position { get; }
	}
}