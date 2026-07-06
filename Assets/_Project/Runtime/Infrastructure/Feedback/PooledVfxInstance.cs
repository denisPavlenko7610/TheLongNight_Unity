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
		private ParticleSystem[] _particleSystems;

		public uint Version { get; private set; }
		public ParticleSystem[] ParticleSystems
		{
			get
			{
				if (_particleSystems == null)
				{
					_particleSystems = GetComponentsInChildren<ParticleSystem>(true);
				}

				return _particleSystems;
			}
		}

		public void Initialize(IObjectPool<PooledVfxInstance> pool)
		{
			_pool = pool;
			_isReleased = false;
			Version++;
		}

		public void CacheParticleSystems()
		{
			_particleSystems = GetComponentsInChildren<ParticleSystem>(true);
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
