using System;
using TLN.Core.Lifetime;
using TLN.Gameplay.Items;

namespace TLN.Gameplay.Survival
{
	public interface ISurvivalService : IGameService
	{
		SurvivalStat Hunger { get; }
		SurvivalStat Thirst { get; }
		SurvivalStat Fatigue { get; }
		SurvivalStat Cold { get; }
		SurvivalStat Condition { get; }

		event Action Changed;

		void Tick(float deltaTime);
		void SetValues(float hunger, float thirst, float fatigue, float cold, float condition);

		void ApplyConsumable(ConsumableItemDefinition consumable);
		void ReduceCold(float amount);

		void AddFatigue(float amount);
		void ReduceFatigue(float amount);
		void AddHunger(float amount);
		void AddThirst(float amount);
		void AddCold(float amount);
		void RestoreCondition(float amount);
		void DamageCondition(float amount);
	}
}
