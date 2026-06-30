using System;
using TLN.Gameplay.Items;

namespace TLN.Gameplay.Survival
{
	public sealed class SurvivalService : ISurvivalService
	{
		public const float MinStat = 0f;
		public const float MaxStat = 100f;
		private const float SecondsPerMinute = 60f;

		private readonly SurvivalConfig _config;

		public SurvivalStat Hunger { get; private set; }
		public SurvivalStat Thirst { get; private set; }
		public SurvivalStat Fatigue { get; private set; }
		public SurvivalStat Cold { get; private set; }
		public SurvivalStat Condition { get; private set; }

		public event Action Changed;

		public SurvivalService(SurvivalConfig config)
		{
			_config = config ?? throw new ArgumentNullException(nameof(config));

			Hunger = new SurvivalStat(SurvivalStatId.Hunger, _config.InitialHunger, MinStat, MaxStat);
			Thirst = new SurvivalStat(SurvivalStatId.Thirst, _config.InitialThirst, MinStat, MaxStat);
			Fatigue = new SurvivalStat(SurvivalStatId.Fatigue, _config.InitialFatigue, MinStat, MaxStat);
			Cold = new SurvivalStat(SurvivalStatId.Cold, _config.InitialCold, MinStat, MaxStat);
			Condition = new SurvivalStat(SurvivalStatId.Condition, _config.InitialCondition, MinStat, MaxStat);
		}

		public void Tick(float deltaTime)
		{
			if (deltaTime <= 0f)
			{
				return;
			}

			float gameHours = ConvertRealDeltaTimeToGameHours(deltaTime);

			Hunger.Add(_config.HungerPerHour * gameHours);
			Thirst.Add(_config.ThirstPerHour * gameHours);
			Fatigue.Add(_config.FatiguePerHour * gameHours);

			ApplyConditionDamage(gameHours);

			Changed?.Invoke();
		}

		public void SetValues(float hunger, float thirst, float fatigue, float cold, float condition)
		{
			Hunger = SetStatValue(Hunger, hunger);
			Thirst = SetStatValue(Thirst, thirst);
			Fatigue = SetStatValue(Fatigue, fatigue);
			Cold = SetStatValue(Cold, cold);
			Condition = SetStatValue(Condition, condition);

			Changed?.Invoke();
		}

		private static SurvivalStat SetStatValue(SurvivalStat stat, float value)
		{
			stat.Set(value);
			return stat;
		}

		private float ConvertRealDeltaTimeToGameHours(float deltaTime)
		{
			float realMinutes = deltaTime / SecondsPerMinute;
			return realMinutes * _config.GameHoursPerRealMinute;
		}

		private void ApplyConditionDamage(float gameHours)
		{
			float damage = 0f;

			if (Hunger.Value >= MaxStat)
			{
				damage += _config.HungerConditionDamagePerHour * gameHours;
			}

			if (Thirst.Value >= MaxStat)
			{
				damage += _config.ThirstConditionDamagePerHour * gameHours;
			}

			if (Fatigue.Value >= MaxStat)
			{
				damage += _config.FatigueConditionDamagePerHour * gameHours;
			}

			if (Cold.Value >= MaxStat)
			{
				damage += _config.ColdConditionDamagePerHour * gameHours;
			}

			if (damage > 0f)
			{
				Condition.Subtract(damage);
			}
		}

		public void ApplyConsumable(ConsumableItemDefinition consumable)
		{
			if (consumable == null)
			{
				return;
			}

			Hunger = SubtractFromStat(Hunger, consumable.HungerRestore);
			Thirst = SubtractFromStat(Thirst, consumable.ThirstRestore);
			Fatigue = SubtractFromStat(Fatigue, consumable.FatigueRestore);
			Cold = SubtractFromStat(Cold, consumable.ColdRestore);

			Condition = AddToStat(Condition, consumable.ConditionRestore);
			Condition = SubtractFromStat(Condition, consumable.ConditionDamage);

			Changed?.Invoke();
		}

		public void AddHunger(float amount)
		{
			Hunger = AddToStat(Hunger, amount);
			Changed?.Invoke();
		}

		public void AddThirst(float amount)
		{
			Thirst = AddToStat(Thirst, amount);
			Changed?.Invoke();
		}

		public void AddFatigue(float amount)
		{
			Fatigue = AddToStat(Fatigue, amount);
			Changed?.Invoke();
		}

		public void ReduceFatigue(float amount)
		{
			Fatigue = SubtractFromStat(Fatigue, amount);
			Changed?.Invoke();
		}

		public void AddCold(float amount)
		{
			Cold = AddToStat(Cold, amount);
			Changed?.Invoke();
		}

		public void ReduceCold(float amount)
		{
			Cold = SubtractFromStat(Cold, amount);
			Changed?.Invoke();
		}

		public void RestoreCondition(float amount)
		{
			Condition = AddToStat(Condition, amount);
			Changed?.Invoke();
		}

		public void DamageCondition(float amount)
		{
			Condition = SubtractFromStat(Condition, amount);
			Changed?.Invoke();
		}

		private static SurvivalStat AddToStat(SurvivalStat stat, float amount)
		{
			if (amount <= 0f)
			{
				return stat;
			}

			stat.Add(amount);
			return stat;
		}

		private static SurvivalStat SubtractFromStat(SurvivalStat stat, float amount)
		{
			if (amount <= 0f)
			{
				return stat;
			}

			stat.Subtract(amount);
			return stat;
		}
	}
}
