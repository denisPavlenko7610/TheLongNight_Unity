using System;
using System.Collections.Generic;
using TLN.Gameplay.Feedback;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TLN.Infrastructure.Feedback
{
	public sealed class PooledEffectPlayer : IDisposable
	{
		private const int MaxInstancesPerPrefab = 32;
		private const float MinLifetimeSeconds = 0.1f;

		private readonly Transform _root;
		private readonly Dictionary<GameObject, Queue<PooledEffectInstance>> _availableByPrefab = new();
		private readonly Dictionary<GameObject, List<PooledEffectInstance>> _allByPrefab = new();

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

			PooledEffectInstance instance = GetInstance(prefab);

			if (instance == null)
			{
				return;
			}

			Transform instanceTransform = instance.transform;
			instanceTransform.SetParent(_root, true);
			instanceTransform.SetPositionAndRotation(
				position,
				Quaternion.identity
			);

			GameObject instanceObject = instance.gameObject;
			instanceObject.SetActive(true);

			instance.MarkSpawned();

			RestartParticleSystems(instanceObject);

			float lifetime = Mathf.Max(
				MinLifetimeSeconds,
				definition.EffectLifetimeSeconds
			);

			_ = ReturnToPoolAfterDelay(
				prefab,
				instance,
				instance.Version,
				lifetime
			);
		}

		public void Dispose()
		{
			_isDisposed = true;

			foreach (KeyValuePair<GameObject, List<PooledEffectInstance>> pair in _allByPrefab)
			{
				List<PooledEffectInstance> instances = pair.Value;

				if (instances == null)
				{
					continue;
				}

				for (int i = 0; i < instances.Count; i++)
				{
					PooledEffectInstance instance = instances[i];

					if (instance != null)
					{
						Object.Destroy(instance.gameObject);
					}
				}
			}

			_availableByPrefab.Clear();
			_allByPrefab.Clear();
		}

		private PooledEffectInstance GetInstance(GameObject prefab)
		{
			Queue<PooledEffectInstance> availableQueue =
				GetAvailableQueue(prefab);

			while (availableQueue.Count > 0)
			{
				PooledEffectInstance candidate = availableQueue.Dequeue();

				if (candidate != null)
				{
					return candidate;
				}
			}

			List<PooledEffectInstance> allInstances = GetAllInstances(prefab);

			if (allInstances.Count < MaxInstancesPerPrefab)
			{
				return CreateInstance(prefab, allInstances);
			}

			return GetReusableBusyInstance(allInstances);
		}

		private PooledEffectInstance CreateInstance(
			GameObject prefab,
			List<PooledEffectInstance> allInstances
		)
		{
			GameObject instanceObject = Object.Instantiate(
				prefab,
				_root
			);

			instanceObject.name = $"{prefab.name} (Pooled)";
			instanceObject.SetActive(false);

			PooledEffectInstance instance =
				instanceObject.GetComponent<PooledEffectInstance>();

			if (instance == null)
			{
				instance = instanceObject.AddComponent<PooledEffectInstance>();
			}

			allInstances.Add(instance);

			return instance;
		}

		private static PooledEffectInstance GetReusableBusyInstance(
			List<PooledEffectInstance> allInstances
		)
		{
			for (int i = 0; i < allInstances.Count; i++)
			{
				PooledEffectInstance instance = allInstances[i];

				if (instance != null)
				{
					return instance;
				}
			}

			return null;
		}

		private Queue<PooledEffectInstance> GetAvailableQueue(
			GameObject prefab
		)
		{
			if (!_availableByPrefab.TryGetValue(
				    prefab,
				    out Queue<PooledEffectInstance> queue
			    ))
			{
				queue = new Queue<PooledEffectInstance>();
				_availableByPrefab[prefab] = queue;
			}

			return queue;
		}

		private List<PooledEffectInstance> GetAllInstances(GameObject prefab)
		{
			if (!_allByPrefab.TryGetValue(
				    prefab,
				    out List<PooledEffectInstance> instances
			    ))
			{
				instances = new List<PooledEffectInstance>();
				_allByPrefab[prefab] = instances;
			}

			return instances;
		}

		private async Awaitable ReturnToPoolAfterDelay(
			GameObject prefab,
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

			GameObject instanceObject = instance.gameObject;

			StopParticleSystems(instanceObject);

			instance.transform.SetParent(_root, true);
			instanceObject.SetActive(false);

			GetAvailableQueue(prefab).Enqueue(instance);
		}

		private static void RestartParticleSystems(GameObject root)
		{
			if (root == null)
			{
				return;
			}

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
			if (root == null)
			{
				return;
			}

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
