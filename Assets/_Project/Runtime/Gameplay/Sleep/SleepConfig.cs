using UnityEngine;

namespace TLN.Gameplay.Sleep
{
	[CreateAssetMenu(fileName = "SleepConfig", menuName = "TLN/Sleep/Sleep Config")]
	public sealed class SleepConfig : ScriptableObject
	{
		[Header("Per Sleep Hour")]
		[SerializeField] private float _fatigueRecoveryPerHour = 25f;
		[SerializeField] private float _hungerIncreasePerHour = 8f;
		[SerializeField] private float _thirstIncreasePerHour = 10f;
		[SerializeField] private float _coldIncreasePerHour = 5f;
		[SerializeField] private float _conditionRecoveryPerHour = 3f;

		[Header("Limits")]
		[SerializeField] private int _minSleepHours = 1;
		[SerializeField] private int _maxSleepHours = 12;

		public float FatigueRecoveryPerHour => _fatigueRecoveryPerHour;
		public float HungerIncreasePerHour => _hungerIncreasePerHour;
		public float ThirstIncreasePerHour => _thirstIncreasePerHour;
		public float ColdIncreasePerHour => _coldIncreasePerHour;
		public float ConditionRecoveryPerHour => _conditionRecoveryPerHour;

		public int MinSleepHours => _minSleepHours;
		public int MaxSleepHours => _maxSleepHours;
	}
}
