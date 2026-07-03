using UnityEngine;
using UnityEngine.Pool;

namespace TLN.Infrastructure.Feedback
{
	[DisallowMultipleComponent]
	[AddComponentMenu("")]
	public sealed class PooledVfxInstance : MonoBehaviour
	{
		private IObjectPool<PooledVfxInstance> _pool;
		private bool _isReleased = true;

		public uint Version { get; private set; }

		public void Initialize(IObjectPool<PooledVfxInstance> pool)
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
