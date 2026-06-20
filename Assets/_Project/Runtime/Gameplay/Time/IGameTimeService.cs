using System;
using TLN.Core.Lifetime;

namespace TLN.Gameplay.Time
{
	public interface IGameTimeService : IGameService
	{
		int TotalMinutes { get; }
		float TotalMinutesExact { get; }
		GameTime CurrentTime { get; }

		event Action Changed;

		void Tick(float deltaTime);
		void AdvanceMinutes(int minutes);
		void AdvanceHours(int hours);
	}
}
