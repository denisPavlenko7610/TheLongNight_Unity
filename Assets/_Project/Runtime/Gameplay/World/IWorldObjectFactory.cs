using UnityEngine;

namespace TLN.Gameplay.World
{
	public interface IWorldObjectFactory
	{
		GameObject Create(GameObject prefab, Vector3 position, Quaternion rotation);
	}
}
