using UnityEngine;

namespace TLN.Gameplay.Inventory
{
	[CreateAssetMenu(fileName = "InventoryConfig", menuName = "TLN/Inventory/Inventory Config")]
	public sealed class InventoryConfig : ScriptableObject
	{
		[SerializeField] private float _maxCarryWeight = 30f;

		public float MaxCarryWeight => _maxCarryWeight;
	}
}
