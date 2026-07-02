using UnityEngine;

namespace TLN.Infrastructure.Feedback
{
	[DisallowMultipleComponent]
	[AddComponentMenu("")]
	public sealed class PooledEffectInstance : MonoBehaviour
	{
		public int Version { get; private set; }

		public void MarkSpawned()
		{
			Version++;
		}
	}
}
