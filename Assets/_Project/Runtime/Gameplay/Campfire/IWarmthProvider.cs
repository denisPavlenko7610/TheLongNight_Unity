using UnityEngine;

namespace TLN.Gameplay.Campfire
{
	public interface IWarmthProvider
	{
		bool IsWarmthActive { get; }
		float WarmthBonus { get; }
		Vector3 Position { get; }
	}
}
