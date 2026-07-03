using System;
using System.Collections.Generic;
using TLN.Core.Logging;
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

		private const int CurveSampleCount = 16;

		private readonly Transform _root;
		private readonly Dictionary<GameObject, ObjectPool<PooledVfxInstance>> _poolsByPrefab = new();
		private readonly Dictionary<GameObject, float?> _lifetimesByPrefab = new();

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

			float? lifetime = GetLifetimeSeconds(prefab);
			ObjectPool<PooledVfxInstance> pool = GetPool(prefab);
			PooledVfxInstance instance = pool.Get();

			instance.Initialize(pool);

			Transform instanceTransform = instance.transform;
			instanceTransform.SetParent(_root, false);
			instanceTransform.SetPositionAndRotation(position, Quaternion.identity);

			instance.MarkSpawned();

			RestartParticleSystems(instance.gameObject);

			if (lifetime.HasValue)
			{
				_ = ReleaseAfterDelay(instance, instance.Version, lifetime.Value);
			}
		}

		public void Dispose()
		{
			_isDisposed = true;

			foreach (ObjectPool<PooledVfxInstance> pool in _poolsByPrefab.Values)
			{
				pool.Clear();
			}

			_poolsByPrefab.Clear();
			_lifetimesByPrefab.Clear();
		}

		private float? GetLifetimeSeconds(GameObject prefab)
		{
			if (_lifetimesByPrefab.TryGetValue(prefab, out float? lifetime))
			{
				return lifetime;
			}

			lifetime = CalculateLifetimeSeconds(prefab);
			_lifetimesByPrefab[prefab] = lifetime;

			return lifetime;
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

			StopParticleSystems(instance.gameObject);
			instance.gameObject.SetActive(false);
		}

		private static void OnDestroyInstance(PooledVfxInstance instance)
		{
			if (instance != null)
			{
				Object.Destroy(instance.gameObject);
			}
		}

		private async Awaitable ReleaseAfterDelay(PooledVfxInstance instance, uint expectedVersion, float delaySeconds)
		{
			float endTime = UnityEngine.Time.time + delaySeconds;
			while (UnityEngine.Time.time < endTime)
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

		private static float? CalculateLifetimeSeconds(GameObject root)
		{
			ParticleSystem[] particleSystems = root.GetComponentsInChildren<ParticleSystem>(true);

			float lifetime = 0f;

			for (int i = 0; i < particleSystems.Length; i++)
			{
				ParticleSystem particleSystem = particleSystems[i];
				if (particleSystem == null)
				{
					continue;
				}

				ParticleSystem.MainModule main = particleSystem.main;
				if (main.loop)
				{
					TLNLogger.LogWarning(
						$"Looping ParticleSystem '{particleSystem.name}' is used in pooled one-shot VFX '{root.name}'. " +
						"Looping VFX will not be automatically returned to pool."
					);

					return null;
				}

				float startDelay = GetMaxCurveValue(main.startDelay);
				float startLifetime = GetMaxCurveValue(main.startLifetime);

				float particleLifetime = startDelay + main.duration + startLifetime;
				lifetime = Mathf.Max(lifetime, particleLifetime);
			}

			return Mathf.Max(Mathf.Epsilon, lifetime);
		}

		private static float GetMaxCurveValue(ParticleSystem.MinMaxCurve curve)
		{
			switch (curve.mode)
			{
				case ParticleSystemCurveMode.Constant:
					return curve.constant;

				case ParticleSystemCurveMode.TwoConstants:
					return curve.constantMax;

				case ParticleSystemCurveMode.Curve:
					return GetMaxAnimationCurveValue(curve.curve) * curve.curveMultiplier;

				case ParticleSystemCurveMode.TwoCurves:
					float minCurveMax = GetMaxAnimationCurveValue(curve.curveMin);
					float maxCurveMax = GetMaxAnimationCurveValue(curve.curveMax);

					return Mathf.Max(minCurveMax, maxCurveMax) * curve.curveMultiplier;

				default:
					return 0f;
			}
		}

		private static float GetMaxAnimationCurveValue(AnimationCurve curve)
		{
			if (curve == null || curve.length == 0)
			{
				return 0f;
			}

			float maxValue = 0f;

			for (int i = 0; i <= CurveSampleCount; i++)
			{
				float time = (float)i / CurveSampleCount;
				float value = curve.Evaluate(time);

				maxValue = Mathf.Max(maxValue, value);
			}

			return maxValue;
		}

		private static void RestartParticleSystems(GameObject root)
		{
			ParticleSystem[] particleSystems = root.GetComponentsInChildren<ParticleSystem>(true);

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
