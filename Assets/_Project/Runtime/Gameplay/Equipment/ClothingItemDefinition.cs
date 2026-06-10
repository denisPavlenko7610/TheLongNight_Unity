using TLN.Gameplay.Items;
using UnityEngine;

namespace TLN.Gameplay.Equipment
{
	[CreateAssetMenu(fileName = "ClothingItemDefinition", menuName = "TLN/Items/Clothing Item Definition")]
	public sealed class ClothingItemDefinition : ItemDefinition
	{
		[Header("Clothing")]
		[SerializeField] private ClothingSlotId _slot = ClothingSlotId.None;
		[SerializeField] private float _warmthBonus = 1f;

		public ClothingSlotId Slot => _slot;
		public float WarmthBonus => Mathf.Max(0f, _warmthBonus);
	}
}
