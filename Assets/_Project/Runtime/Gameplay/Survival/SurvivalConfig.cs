using UnityEngine;

namespace TLN.Gameplay.Survival
{
	[CreateAssetMenu(fileName = "SurvivalConfig", menuName = "TLN/Survival/Survival Config")]
	public sealed class SurvivalConfig : ScriptableObject
	{
		[Header("Initial Values")]
		[SerializeField] private float _initialHunger = 0f;
		[SerializeField] private float _initialThirst = 0f;
		[SerializeField] private float _initialFatigue = 0f;
		[SerializeField] private float _initialCold = 0f;
		[SerializeField] private float _initialCondition = 100f;

		[Header("Increase Per Game Hour")]
		[SerializeField] private float _hungerPerHour = 8f;
		[SerializeField] private float _thirstPerHour = 12f;
		[SerializeField] private float _fatiguePerHour = 6f;
		[SerializeField] private float _coldPerHour = 4f;

		[Header("Condition Damage Per Game Hour")]
		[SerializeField] private float _hungerConditionDamagePerHour = 5f;
		[SerializeField] private float _thirstConditionDamagePerHour = 10f;
		[SerializeField] private float _fatigueConditionDamagePerHour = 3f;
		[SerializeField] private float _coldConditionDamagePerHour = 15f;

		[Header("Simulation")]
		[SerializeField] private float _gameHoursPerRealMinute = 1f;
		[SerializeField] private float _survivalTickIntervalSeconds = 0.25f;
		[SerializeField] private float _warningTickIntervalSeconds = 0.5f;

		[Header("Networking")]
		[SerializeField] private float _networkSyncEpsilon = 0.001f;

		public float InitialHunger => _initialHunger;
		public float InitialThirst => _initialThirst;
		public float InitialFatigue => _initialFatigue;
		public float InitialCold => _initialCold;
		public float InitialCondition => _initialCondition;

		public float HungerPerHour => _hungerPerHour;
		public float ThirstPerHour => _thirstPerHour;
		public float FatiguePerHour => _fatiguePerHour;
		public float ColdPerHour => _coldPerHour;

		public float HungerConditionDamagePerHour => _hungerConditionDamagePerHour;
		public float ThirstConditionDamagePerHour => _thirstConditionDamagePerHour;
		public float FatigueConditionDamagePerHour => _fatigueConditionDamagePerHour;
		public float ColdConditionDamagePerHour => _coldConditionDamagePerHour;

		public float GameHoursPerRealMinute => _gameHoursPerRealMinute;

		public float WarningTickIntervalSeconds => Mathf.Max(0.01f, _warningTickIntervalSeconds);

		public float SurvivalTickIntervalSeconds => Mathf.Max(0.01f, _survivalTickIntervalSeconds);

		public float NetworkSyncEpsilon => Mathf.Max(0f, _networkSyncEpsilon);
	}
}
