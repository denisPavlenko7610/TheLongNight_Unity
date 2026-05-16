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

		[Header("Simulation")]
		[SerializeField] private float _gameHoursPerRealMinute = 1f;

		public float InitialHunger => _initialHunger;
		public float InitialThirst => _initialThirst;
		public float InitialFatigue => _initialFatigue;
		public float InitialCold => _initialCold;
		public float InitialCondition => _initialCondition;

		public float HungerPerHour => _hungerPerHour;
		public float ThirstPerHour => _thirstPerHour;
		public float FatiguePerHour => _fatiguePerHour;
		public float ColdPerHour => _coldPerHour;

		public float GameHoursPerRealMinute => _gameHoursPerRealMinute;
	}
}
