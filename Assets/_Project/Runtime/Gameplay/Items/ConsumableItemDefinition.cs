using UnityEngine;

namespace TLN.Gameplay.Items
{
	[CreateAssetMenu(fileName = "ConsumableItemDefinition", menuName = "TLN/Items/Consumable Item Definition")]
	public sealed class ConsumableItemDefinition : ItemDefinition
	{
		[Header("Survival Effects")]
		[SerializeField] private float _hungerChange;
		[SerializeField] private float _thirstChange;
		[SerializeField] private float _fatigueChange;
		[SerializeField] private float _coldChange;
		[SerializeField] private float _conditionChange;

		public float HungerChange => _hungerChange;
		public float ThirstChange => _thirstChange;
		public float FatigueChange => _fatigueChange;
		public float ColdChange => _coldChange;
		public float ConditionChange => _conditionChange;
	}
}
