using System;
using TLN.Application.Feedback;
using TLN.Gameplay.Feedback;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TLN.Infrastructure.Feedback
{
	public sealed class UnityFeedbackService : IFeedbackService, IDisposable
	{
		private const string RootName = "[TLN Feedback Service]";

		private readonly FeedbackCatalog _catalog;
		private readonly GameObject _root;
		private readonly PooledAudioPlayer _audioPlayer;
		private readonly PooledEffectPlayer _effectPlayer;

		public UnityFeedbackService(FeedbackCatalog catalog)
		{
			_catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));

			_root = new GameObject(RootName);
			Object.DontDestroyOnLoad(_root);

			_audioPlayer = new PooledAudioPlayer(_root.transform);
			_effectPlayer = new PooledEffectPlayer(_root.transform);
		}

		public void Play(FeedbackEventId eventId)
		{
			if (!TryGetDefinition(eventId, out FeedbackDefinition definition))
			{
				return;
			}

			Vector3 position = _root.transform.position;

			_audioPlayer.Play(definition, position, false);
			_effectPlayer.Play(definition, position);
		}

		public void PlayAt(FeedbackEventId eventId, Vector3 position)
		{
			if (!TryGetDefinition(eventId, out FeedbackDefinition definition))
			{
				return;
			}

			_audioPlayer.Play(definition, position, true);
			_effectPlayer.Play(definition, position);
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
			_audioPlayer.Dispose();

			if (_root != null)
			{
				Object.Destroy(_root);
			}

			_effectPlayer.Dispose();
		}

		private bool TryGetDefinition(
			FeedbackEventId eventId,
			out FeedbackDefinition definition
		)
		{
			return _catalog.TryGet(eventId, out definition);
		}

		private static async Awaitable WaitUnscaled(float seconds)
		{
			if (seconds <= 0f)
			{
				return;
			}

			float endTime = UnityEngine.Time.realtimeSinceStartup + seconds;

			while (UnityEngine.Time.realtimeSinceStartup < endTime)
			{
				await Awaitable.NextFrameAsync();
			}
		}
	}
}
