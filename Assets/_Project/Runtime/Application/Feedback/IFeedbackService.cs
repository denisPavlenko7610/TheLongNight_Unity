using TLN.Core.Lifetime;
using UnityEngine;

namespace TLN.Application.Feedback
{
	public interface IFeedbackService : IGameService
	{
		void Play(FeedbackEventId eventId);
		void PlayAt(FeedbackEventId eventId, Vector3 position);

		Awaitable PlayDelayed(FeedbackEventId eventId, float delaySeconds);
		Awaitable PlayAtDelayed(FeedbackEventId eventId, Vector3 position, float delaySeconds);
	}
}
