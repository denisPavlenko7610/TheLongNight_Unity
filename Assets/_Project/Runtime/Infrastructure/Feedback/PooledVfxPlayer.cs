using System;
using System.Collections.Generic;
using TLN.Gameplay.Feedback;
using UnityEngine;
using UnityEngine.Pool;
using Object = UnityEngine.Object;

namespace TLN.Infrastructure.Feedback
{
	public sealed class PooledVfxPlayer : IDisposable
	{
		private const int DefaultCapacity = 4;
		private const int MaxInstancesPerPrefab = 32;

		private readonly Transform _root;
		private readonly Dictionary<GameObject, ObjectPool<PooledVfxInstance>> _poolsByPrefab = new();

		private bool _isDisposed;

		public PooledVfxPlayer(Transform root)
		{
			_root = root != null
				? root
				: throw new ArgumentNullException(nameof(root));
		}

		public void Play(FeedbackDefinition definition, Vector3 position)
		{
			if (_isDisposed)
			{
				return;
			}

			if (definition == null || !definition.HasEffect)
			{
				return;
			}

			GameObject prefab = definition.EffectPrefab;
			if (prefab == null)
			{
				return;
			}

			ObjectPool<PooledVfxInstance> pool = GetPool(prefab);
			PooledVfxInstance instance = pool.Get();

			instance.Initialize(pool);

			Transform instanceTransform = instance.transform;
			instanceTransform.SetParent(_root, false);
			instanceTransform.SetPositionAndRotation(position, Quaternion.identity);

			instance.MarkSpawned();

			ParticleSystem[] particleSystems = instance.ParticleSystems;
			RestartParticleSystems(particleSystems);

			_ = ReleaseWhenFinished(instance, instance.Version, particleSystems);
		}

		public void Dispose()
		{
			_isDisposed = true;

			foreach (ObjectPool<PooledVfxInstance> pool in _poolsByPrefab.Values)
			{
				pool.Clear();
			}

			_poolsByPrefab.Clear();
		}

		private ObjectPool<PooledVfxInstance> GetPool(GameObject prefab)
		{
			if (_poolsByPrefab.TryGetValue(prefab, out ObjectPool<PooledVfxInstance> pool))
			{
				return pool;
			}

			pool = new ObjectPool<PooledVfxInstance>(
				createFunc: () => CreateInstance(prefab),
				actionOnGet: OnGetInstance,
				actionOnRelease: OnReleaseInstance,
				actionOnDestroy: OnDestroyInstance,
#if UNITY_EDITOR
				collectionCheck: true,
#else
				collectionCheck: false,
#endif
				defaultCapacity: DefaultCapacity,
				maxSize: MaxInstancesPerPrefab
			);

			_poolsByPrefab[prefab] = pool;

			return pool;
		}

		private PooledVfxInstance CreateInstance(GameObject prefab)
		{
			GameObject instanceObject = Object.Instantiate(prefab, _root);
			instanceObject.name = $"{prefab.name} (Pooled)";
			instanceObject.SetActive(false);

			PooledVfxInstance instance = instanceObject.GetComponent<PooledVfxInstance>();

			if (instance == null)
			{
				instance = instanceObject.AddComponent<PooledVfxInstance>();
			}

			instance.CacheParticleSystems();
			return instance;
		}

		private static void OnGetInstance(PooledVfxInstance instance)
		{
			if (instance != null)
			{
				instance.gameObject.SetActive(true);
			}
		}

		private static void OnReleaseInstance(PooledVfxInstance instance)
		{
			if (instance == null)
			{
				return;
			}

			StopParticleSystems(instance.ParticleSystems);
			instance.gameObject.SetActive(false);
		}

		private static void OnDestroyInstance(PooledVfxInstance instance)
		{
			if (instance != null)
			{
				Object.Destroy(instance.gameObject);
			}
		}

		private async Awaitable ReleaseWhenFinished(PooledVfxInstance instance, uint expectedVersion,
			ParticleSystem[] particleSystems)
		{
			while (!_isDisposed
				&& instance != null
				&& instance.Version == expectedVersion
				&& IsAnyParticleSystemAlive(particleSystems))
			{
				await Awaitable.NextFrameAsync();
			}

			if (_isDisposed)
			{
				return;
			}

			if (instance == null)
			{
				return;
			}

			if (instance.Version != expectedVersion)
			{
				return;
			}

			instance.Release();
		}

		private static bool IsAnyParticleSystemAlive(ParticleSystem[] particleSystems)
		{
			for (int i = 0; i < particleSystems.Length; i++)
			{
				ParticleSystem particleSystem = particleSystems[i];
				if (particleSystem == null)
				{
					continue;
				}

				if (particleSystem.IsAlive(true))
				{
					return true;
				}
			}

			return false;
		}

		private static void RestartParticleSystems(ParticleSystem[] particleSystems)
		{
			for (int i = 0; i < particleSystems.Length; i++)
			{
				ParticleSystem particleSystem = particleSystems[i];
				if (particleSystem == null)
				{
					continue;
				}

				particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
				particleSystem.Play(true);
			}
		}

		private static void StopParticleSystems(ParticleSystem[] particleSystems)
		{
			for (int i = 0; i < particleSystems.Length; i++)
			{
				ParticleSystem particleSystem = particleSystems[i];

				if (particleSystem == null)
				{
					continue;
				}

				particleSystem.Stop(
					true,
					ParticleSystemStopBehavior.StopEmittingAndClear
				);
			}
		}
	}
}
