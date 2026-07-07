using System;
using TLN.Application.Audio;
using TLN.Application.Feedback;
using TLN.Gameplay.Feedback;
using Awaitable = UnityEngine.Awaitable;
using UnityEngine;
using Object = UnityEngine.Object;
using UnityTime = UnityEngine.Time;

namespace TLN.Infrastructure.Feedback
{
	public sealed class UnityFeedbackService : IFeedbackService, IDisposable
	{
		private const string RootName = "[TLN Feedback Service]";

		private readonly FeedbackCatalog _catalog;
		private readonly IAudioMixerService _audioMixerService;

		private GameObject _root;
		private PooledAudioPlayer _audioPlayer;
		private PooledVfxPlayer _vfxPlayer;
		private bool _isDisposed;

		public UnityFeedbackService(FeedbackCatalog catalog, IAudioMixerService audioMixerService = null)
		{
			_catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
			_audioMixerService = audioMixerService;
		}

		public void Play(FeedbackEventId eventId)
		{
			if (!TryPrepareFeedback(eventId, out FeedbackDefinition definition))
			{
				return;
			}

			Vector3 position = _root.transform.position;

			_audioPlayer.Play2D(definition, position);
			_vfxPlayer.Play(definition, position);
		}

		public void PlayAt(FeedbackEventId eventId, Vector3 position)
		{
			if (!TryPrepareFeedback(eventId, out FeedbackDefinition definition))
			{
				return;
			}

			_audioPlayer.PlaySpatial(definition, position);
			_vfxPlayer.Play(definition, position);
		}

		public async Awaitable PlayDelayed(
			FeedbackEventId eventId,
			float delaySeconds
		)
		{
			await WaitUnscaled(delaySeconds);
			Play(eventId);
		}

		public async Awaitable PlayAtDelayed(
			FeedbackEventId eventId,
			Vector3 position,
			float delaySeconds
		)
		{
			await WaitUnscaled(delaySeconds);
			PlayAt(eventId, position);
		}

		public void Dispose()
		{
			if (_isDisposed)
			{
				return;
			}

			_isDisposed = true;
			DisposePlayers();
		}

		private bool EnsurePlayers()
		{
			if (_isDisposed)
			{
				return false;
			}

			if (_root != null && _audioPlayer != null && _vfxPlayer != null)
			{
				return true;
			}

			DisposePlayers();
			CreatePlayers();
			return _root != null && _audioPlayer != null && _vfxPlayer != null;
		}

		private bool TryPrepareFeedback(
			FeedbackEventId eventId,
			out FeedbackDefinition definition
		)
		{
			definition = null;

			if (!_catalog.TryGet(eventId, out definition))
			{
				return false;
			}

			return EnsurePlayers();
		}

		private void CreatePlayers()
		{
			_root = new GameObject(RootName);
			Object.DontDestroyOnLoad(_root);

			_audioPlayer = new PooledAudioPlayer(_root.transform, _audioMixerService);
			_vfxPlayer = new PooledVfxPlayer(_root.transform);
		}

		private void DisposePlayers()
		{
			_audioPlayer?.Dispose();
			_vfxPlayer?.Dispose();

			if (_root != null)
			{
				Object.Destroy(_root);
			}

			_root = null;
			_audioPlayer = null;
			_vfxPlayer = null;
		}

		private static async Awaitable WaitUnscaled(float seconds)
		{
			if (seconds <= 0f)
			{
				return;
			}

			float endTime = UnityTime.realtimeSinceStartup + seconds;

			while (UnityTime.realtimeSinceStartup < endTime)
			{
				await Awaitable.NextFrameAsync();
			}
		}
	}
}
