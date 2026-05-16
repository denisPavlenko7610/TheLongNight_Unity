using System;

namespace TLN.Gameplay.Survival
{
	[Serializable]
	public struct SurvivalStat
	{
		public SurvivalStatId Id { get; }
		public float Value { get; private set; }
		public float MinValue { get; }
		public float MaxValue { get; }

		public SurvivalStat(SurvivalStatId id, float value, float minValue, float maxValue)
		{
			Id = id;
			MinValue = minValue;
			MaxValue = maxValue;
			Value = Math.Clamp(value, minValue, maxValue);
		}

		public void Add(float amount)
		{
			Value = Math.Clamp(Value + amount, MinValue, MaxValue);
		}

		public void Subtract(float amount)
		{
			Value = Math.Clamp(Value - amount, MinValue, MaxValue);
		}

		public void Set(float value)
		{
			Value = Math.Clamp(value, MinValue, MaxValue);
		}
	}
}
