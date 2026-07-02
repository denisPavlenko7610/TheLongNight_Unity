using System;
using System.Collections.Generic;
using TLN.Gameplay.Feedback;
using UnityEngine;
using UnityEngine.Pool;
using Object = UnityEngine.Object;

namespace TLN.Infrastructure.Feedback
{
	public sealed class PooledEffectPlayer : IDisposable
	{
		private const int DefaultCapacity = 4;
		private const int MaxInstancesPerPrefab = 32;
		private const float MinLifetimeSeconds = 0.1f;

		private readonly Transform _root;
		private readonly Dictionary<GameObject, ObjectPool<PooledEffectInstance>> _poolsByPrefab = new();

		private bool _isDisposed;

		public PooledEffectPlayer(Transform root)
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

			ObjectPool<PooledEffectInstance> pool = GetPool(prefab);
			PooledEffectInstance instance = pool.Get();

			instance.Initialize(pool);

			Transform instanceTransform = instance.transform;
			instanceTransform.SetParent(_root, true);
			instanceTransform.SetPositionAndRotation(position, Quaternion.identity);

			instance.gameObject.SetActive(true);
			instance.MarkSpawned();

			RestartParticleSystems(instance.gameObject);

			float lifetime = Mathf.Max(
				MinLifetimeSeconds,
				definition.EffectLifetimeSeconds
			);

			_ = ReleaseAfterDelay(instance, instance.Version, lifetime);
		}

		public void Dispose()
		{
			_isDisposed = true;

			foreach (ObjectPool<PooledEffectInstance> pool in _poolsByPrefab.Values)
			{
				pool.Clear();
			}

			_poolsByPrefab.Clear();
		}

		private ObjectPool<PooledEffectInstance> GetPool(GameObject prefab)
		{
			if (_poolsByPrefab.TryGetValue(
				    prefab,
				    out ObjectPool<PooledEffectInstance> pool))
			{
				return pool;
			}

			pool = new ObjectPool<PooledEffectInstance>(
				createFunc: () => CreateInstance(prefab),
				actionOnGet: OnGetInstance,
				actionOnRelease: OnReleaseInstance,
				actionOnDestroy: OnDestroyInstance,
				collectionCheck: false,
				defaultCapacity: DefaultCapacity,
				maxSize: MaxInstancesPerPrefab
			);

			_poolsByPrefab[prefab] = pool;

			return pool;
		}

		private PooledEffectInstance CreateInstance(GameObject prefab)
		{
			GameObject instanceObject = Object.Instantiate(prefab, _root);
			instanceObject.name = $"{prefab.name} (Pooled)";
			instanceObject.SetActive(false);

			PooledEffectInstance instance =
				instanceObject.GetComponent<PooledEffectInstance>();

			if (instance == null)
			{
				instance = instanceObject.AddComponent<PooledEffectInstance>();
			}

			return instance;
		}

		private static void OnGetInstance(PooledEffectInstance instance)
		{
			if (instance != null)
			{
				instance.gameObject.SetActive(true);
			}
		}

		private static void OnReleaseInstance(PooledEffectInstance instance)
		{
			if (instance == null)
			{
				return;
			}

			StopParticleSystems(instance.gameObject);
			instance.gameObject.SetActive(false);
		}

		private static void OnDestroyInstance(PooledEffectInstance instance)
		{
			if (instance != null)
			{
				Object.Destroy(instance.gameObject);
			}
		}

		private async Awaitable ReleaseAfterDelay(
			PooledEffectInstance instance,
			int expectedVersion,
			float delaySeconds
		)
		{
			float endTime = UnityEngine.Time.realtimeSinceStartup + delaySeconds;

			while (UnityEngine.Time.realtimeSinceStartup < endTime)
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

		private static void RestartParticleSystems(GameObject root)
		{
			ParticleSystem[] particleSystems =
				root.GetComponentsInChildren<ParticleSystem>(true);

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

				particleSystem.Play(true);
			}
		}

		private static void StopParticleSystems(GameObject root)
		{
			ParticleSystem[] particleSystems =
				root.GetComponentsInChildren<ParticleSystem>(true);

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
