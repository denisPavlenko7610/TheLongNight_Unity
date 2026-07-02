using UnityEngine;
using UnityEngine.Pool;

namespace TLN.Infrastructure.Feedback
{
	[DisallowMultipleComponent]
	[AddComponentMenu("")]
	public sealed class PooledEffectInstance : MonoBehaviour
	{
		private IObjectPool<PooledEffectInstance> _pool;
		private bool _isReleased = true;

		public int Version { get; private set; }

		public void Initialize(IObjectPool<PooledEffectInstance> pool)
		{
			_pool = pool;
			_isReleased = false;
		}

		public void MarkSpawned()
		{
			Version++;
			_isReleased = false;
		}

		public void Release()
		{
			if (_isReleased)
			{
				return;
			}

			_isReleased = true;
			_pool?.Release(this);
		}
	}
}
