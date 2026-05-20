using UnityEngine;

namespace TLN.Gameplay.Items
{
	[CreateAssetMenu(fileName = "ConsumableItemDefinition", menuName = "TLN/Items/Consumable Item Definition")]
	public sealed class ConsumableItemDefinition : ItemDefinition
	{
		[Header("Need Restoration")]
		[SerializeField] private float _hungerRestore;
		[SerializeField] private float _thirstRestore;
		[SerializeField] private float _fatigueRestore;
		[SerializeField] private float _coldRestore;

		[Header("Condition")]
		[SerializeField] private float _conditionRestore;
		[SerializeField] private float _conditionDamage;

		public float HungerRestore => _hungerRestore;
		public float ThirstRestore => _thirstRestore;
		public float FatigueRestore => _fatigueRestore;
		public float ColdRestore => _coldRestore;
		public float ConditionRestore => _conditionRestore;
		public float ConditionDamage => _conditionDamage;
	}
}
