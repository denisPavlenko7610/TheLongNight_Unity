using System;
using System.Collections.Generic;
using TLN.Application.Audio;
using TLN.Gameplay.Feedback;
using UnityEngine;
using Random = UnityEngine.Random;

namespace TLN.Infrastructure.Feedback
{
	public sealed class PooledAudioPlayer : IDisposable
	{
		private const int InitialAudioSourceCount = 8;
		private const int MaxAudioSourceCount = 32;

		private readonly Transform _root;
		private readonly IAudioMixerService _audioMixerService;
		private readonly List<AudioSource> _sources = new();

		public PooledAudioPlayer(Transform root, IAudioMixerService audioMixerService = null)
		{
			_root = root != null ? root : throw new ArgumentNullException(nameof(root));
			_audioMixerService = audioMixerService;

			for (int i = 0; i < InitialAudioSourceCount; i++)
			{
				CreateSource();
			}
		}

		public void Play(FeedbackDefinition definition, Vector3 position, bool spatial)
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

			AudioSource source = GetSourceForPlayback(definition.Priority);
			if (source == null)
			{
				return;
			}

			source.Stop();
			source.transform.position = position;
			source.clip = clip;
			source.volume = definition.Volume;
			source.pitch = Random.Range(definition.MinPitch, definition.MaxPitch);
			source.spatialBlend = spatial ? definition.SpatialBlend : 0f;
			source.minDistance = definition.MinDistance;
			source.maxDistance = definition.MaxDistance;
			source.priority = (int)definition.Priority;
			_audioMixerService?.AssignMixerGroup(source, definition.AudioBusId);

			source.Play();
		}

		public void Dispose()
		{
			_sources.Clear();
		}

		private AudioSource GetSourceForPlayback(FeedbackDefinition.UnityAudioPriority priority)
		{
			for (int i = 0; i < _sources.Count; i++)
			{
				AudioSource source = _sources[i];

				if (!source.isPlaying)
				{
					return source;
				}
			}

			if (_sources.Count < MaxAudioSourceCount)
			{
				return CreateSource();
			}

			return FindReplacementSource(priority);
		}

		private AudioSource FindReplacementSource(FeedbackDefinition.UnityAudioPriority incomingPriority)
		{
			AudioSource bestSource = null;
			FeedbackDefinition.UnityAudioPriority bestPriority = FeedbackDefinition.UnityAudioPriority.Critical;
			float bestProgress = -1f;

			for (int i = 0; i < _sources.Count; i++)
			{
				AudioSource source = _sources[i];
				if (source == null)
				{
					continue;
				}

				FeedbackDefinition.UnityAudioPriority sourcePriority = (FeedbackDefinition.UnityAudioPriority)source.priority;
				if (sourcePriority < incomingPriority)
				{
					continue;
				}

				float progress = GetPlaybackProgress(source);
				if (
					bestSource == null
					|| sourcePriority > bestPriority
					|| sourcePriority == bestPriority && progress > bestProgress
				)
				{
					bestProgress = progress;
					bestPriority = sourcePriority;
					bestSource = source;
				}
			}

			return bestSource;
		}

		private static float GetPlaybackProgress(AudioSource source)
		{
			AudioClip clip = source.clip;
			if (clip == null)
			{
				return 1f;
			}

			if (clip.length <= 0f)
			{
				return 1f;
			}

			return Mathf.Clamp01(source.time / clip.length);
		}

		private AudioSource CreateSource()
		{
			GameObject sourceObject = new GameObject("Feedback Audio Source");
			sourceObject.transform.SetParent(_root, false);

			AudioSource source = sourceObject.AddComponent<AudioSource>();
			source.playOnAwake = false;
			source.loop = false;

			_sources.Add(source);
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
