using UnityEngine;

namespace TLN.Bootstrap
{
	[DisallowMultipleComponent]
	public sealed class SceneInjectable : MonoBehaviour
	{
		[SerializeField] private bool _injectChildren = true;

		public bool InjectChildren => _injectChildren;
	}
}
