using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace TLN.Gameplay.World
{
	public sealed class VContainerWorldObjectFactory : IWorldObjectFactory
	{
		private readonly IObjectResolver _resolver;

		public VContainerWorldObjectFactory(IObjectResolver resolver)
		{
			_resolver = resolver;
		}

		public GameObject Create(GameObject prefab, Vector3 position, Quaternion rotation)
		{
			return _resolver.Instantiate(prefab, position, rotation);
		}
	}
}