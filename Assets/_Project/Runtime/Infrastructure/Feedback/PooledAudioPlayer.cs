using System;
using System.Collections.Generic;
using TLN.Application.Audio;
using TLN.Gameplay.Feedback;
using UnityEngine;
using Object = UnityEngine.Object;
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
		private bool _isDisposed;

		public PooledAudioPlayer(Transform root, IAudioMixerService audioMixerService = null)
		{
			_root = root != null ? root : throw new ArgumentNullException(nameof(root));
			_audioMixerService = audioMixerService;

			for (int i = 0; i < InitialAudioSourceCount; i++)
			{
				CreateSource();
			}
		}

		public void Play2D(FeedbackDefinition definition, Vector3 position)
		{
			Play(definition, position, 0f);
		}

		public void PlaySpatial(FeedbackDefinition definition, Vector3 position)
		{
			Play(definition, position, definition?.SpatialBlend ?? 0f);
		}

		private void Play(FeedbackDefinition definition, Vector3 position, float spatialBlend)
		{
			if (_isDisposed)
			{
				return;
			}

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
			source.spatialBlend = spatialBlend;
			source.minDistance = definition.MinDistance;
			source.maxDistance = definition.MaxDistance;
			source.priority = (int)definition.Priority;
			_audioMixerService?.AssignMixerGroup(source, definition.AudioBusId);

			source.Play();
		}

		public void Dispose()
		{
			if (_isDisposed)
			{
				return;
			}

			_isDisposed = true;

			for (int i = 0; i < _sources.Count; i++)
			{
				AudioSource source = _sources[i];
				if (source != null)
				{
					Object.Destroy(source.gameObject);
				}
			}

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
			FeedbackDefinition.UnityAudioPriority bestReplacementPriority =
				FeedbackDefinition.UnityAudioPriority.Critical;
			float bestProgress = -1f;

			for (int i = 0; i < _sources.Count; i++)
			{
				AudioSource source = _sources[i];
				if (source == null)
				{
					continue;
				}

				FeedbackDefinition.UnityAudioPriority playingPriority =
					(FeedbackDefinition.UnityAudioPriority)source.priority;

				if (!CanInterruptPlayingSource(playingPriority, incomingPriority))
				{
					continue;
				}

				float progress = GetPlaybackProgress(source);
				if (IsBetterReplacementCandidate(
					    bestSource,
					    playingPriority,
					    bestReplacementPriority,
					    progress,
					    bestProgress
				    ))
				{
					bestProgress = progress;
					bestReplacementPriority = playingPriority;
					bestSource = source;
				}
			}

			return bestSource;
		}

		private static bool CanInterruptPlayingSource(
			FeedbackDefinition.UnityAudioPriority playingPriority,
			FeedbackDefinition.UnityAudioPriority incomingPriority
		)
		{
			return playingPriority >= incomingPriority;
		}

		private static bool IsBetterReplacementCandidate(
			AudioSource currentBestSource,
			FeedbackDefinition.UnityAudioPriority candidatePriority,
			FeedbackDefinition.UnityAudioPriority currentBestPriority,
			float candidateProgress,
			float currentBestProgress
		)
		{
			return currentBestSource == null
			       || candidatePriority > currentBestPriority
			       || (
				       candidatePriority == currentBestPriority &&
				       candidateProgress > currentBestProgress
			       );
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

			int validClipCount = 0;
			for (int i = 0; i < clips.Length; i++)
			{
				if (clips[i] != null)
				{
					validClipCount++;
				}
			}

			if (validClipCount == 0)
			{
				return null;
			}

			int selectedClipIndex = Random.Range(0, validClipCount);
			for (int i = 0; i < clips.Length; i++)
			{
				AudioClip clip = clips[i];
				if (clip == null)
				{
					continue;
				}

				if (selectedClipIndex == 0)
				{
					return clip;
				}

				selectedClipIndex--;
			}

			return null;
		}
	}
}
