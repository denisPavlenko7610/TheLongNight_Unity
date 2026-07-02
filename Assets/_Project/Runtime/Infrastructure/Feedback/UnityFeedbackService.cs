using System;
using System.Collections.Generic;
using TLN.Application.Feedback;
using TLN.Gameplay.Feedback;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace TLN.Infrastructure.Feedback
{
	public sealed class UnityFeedbackService : IFeedbackService, IDisposable
	{
		private const int InitialAudioSourceCount = 8;
		private const string RootName = "[TLN Feedback Service]";

		private readonly FeedbackCatalog _catalog;
		private readonly GameObject _root;
		private readonly List<AudioSource> _audioSources = new();

		public UnityFeedbackService(FeedbackCatalog catalog)
		{
			_catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));

			_root = new GameObject(RootName);
			Object.DontDestroyOnLoad(_root);

			for (int i = 0; i < InitialAudioSourceCount; i++)
			{
				CreateAudioSource();
			}
		}

		public void Play(FeedbackEventId eventId)
		{
			if (!TryGetDefinition(eventId, out FeedbackDefinition definition))
			{
				return;
			}

			PlayAudio(definition, _root.transform.position, false);
			SpawnEffect(definition, _root.transform.position);
		}

		public void PlayAt(FeedbackEventId eventId, Vector3 position)
		{
			if (!TryGetDefinition(eventId, out FeedbackDefinition definition))
			{
				return;
			}

			PlayAudio(definition, position, true);
			SpawnEffect(definition, position);
		}

		public void Dispose()
		{
			if (_root != null)
			{
				Object.Destroy(_root);
			}
		}

		private bool TryGetDefinition(
			FeedbackEventId eventId,
			out FeedbackDefinition definition
		)
		{
			return _catalog.TryGet(eventId, out definition);
		}

		private void PlayAudio(
			FeedbackDefinition definition,
			Vector3 position,
			bool spatial
		)
		{
			if (definition == null || !definition.HasAudio)
			{
				return;
			}

			AudioClip clip = GetRandomClip(definition.AudioClips);

			if (clip == null)
			{
				return;
			}

			AudioSource source = GetFreeAudioSource();

			source.transform.position = position;
			source.clip = clip;
			source.volume = definition.Volume;
			source.pitch = Random.Range(definition.MinPitch, definition.MaxPitch);
			source.spatialBlend = spatial ? definition.SpatialBlend : 0f;
			source.minDistance = definition.MinDistance;
			source.maxDistance = definition.MaxDistance;

			source.Play();
		}

		private void SpawnEffect(
			FeedbackDefinition definition,
			Vector3 position
		)
		{
			if (definition == null || !definition.HasEffect)
			{
				return;
			}

			GameObject instance = Object.Instantiate(
				definition.EffectPrefab,
				position,
				Quaternion.identity
			);

			float lifetime = Mathf.Max(
				0.1f,
				definition.EffectLifetimeSeconds
			);

			Object.Destroy(instance, lifetime);
		}

		private AudioSource GetFreeAudioSource()
		{
			for (int i = 0; i < _audioSources.Count; i++)
			{
				AudioSource source = _audioSources[i];

				if (source != null && !source.isPlaying)
				{
					return source;
				}
			}

			return CreateAudioSource();
		}

		private AudioSource CreateAudioSource()
		{
			GameObject sourceObject = new GameObject("Feedback Audio Source");
			sourceObject.transform.SetParent(_root.transform);

			AudioSource source = sourceObject.AddComponent<AudioSource>();
			source.playOnAwake = false;
			source.loop = false;

			_audioSources.Add(source);

			return source;
		}

		private static AudioClip GetRandomClip(AudioClip[] clips)
		{
			if (clips == null || clips.Length == 0)
			{
				return null;
			}

			int index = Random.Range(0, clips.Length);
			return clips[index];
		}
	}
}
