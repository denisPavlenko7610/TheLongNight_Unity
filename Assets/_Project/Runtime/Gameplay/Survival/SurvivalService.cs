using System;
using TLN.Gameplay.Items;

namespace TLN.Gameplay.Survival
{
    public sealed class SurvivalService : ISurvivalService
    {
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

            Hunger = new SurvivalStat(SurvivalStatId.Hunger, _config.InitialHunger, 0f, 100f);
            Thirst = new SurvivalStat(SurvivalStatId.Thirst, _config.InitialThirst, 0f, 100f);
            Fatigue = new SurvivalStat(SurvivalStatId.Fatigue, _config.InitialFatigue, 0f, 100f);
            Cold = new SurvivalStat(SurvivalStatId.Cold, _config.InitialCold, 0f, 100f);
            Condition = new SurvivalStat(SurvivalStatId.Condition, _config.InitialCondition, 0f, 100f);
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
            Cold.Add(_config.ColdPerHour * gameHours);

            ApplyConditionDamage(gameHours);

            Changed?.Invoke();
        }

        private float ConvertRealDeltaTimeToGameHours(float deltaTime)
        {
            float realMinutes = deltaTime / 60f;
            return realMinutes * _config.GameHoursPerRealMinute;
        }

        private void ApplyConditionDamage(float gameHours)
        {
            float damage = 0f;

            if (Hunger.Value >= 100f)
            {
                damage += 5f * gameHours;
            }

            if (Thirst.Value >= 100f)
            {
                damage += 10f * gameHours;
            }

            if (Fatigue.Value >= 100f)
            {
                damage += 3f * gameHours;
            }

            if (Cold.Value >= 100f)
            {
                damage += 15f * gameHours;
            }

            if (damage > 0f)
            {
                Condition.Subtract(damage);
            }
        }

        private static SurvivalStat ApplyStatChange(SurvivalStat stat, float change)
        {
            if (change > 0f)
            {
                stat.Add(change);
                return stat;
            }

            if (change < 0f)
            {
                stat.Subtract(-change);
                return stat;
            }

            return stat;
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
