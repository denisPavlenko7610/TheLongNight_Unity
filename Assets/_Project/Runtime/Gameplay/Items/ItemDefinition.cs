using UnityEngine;

namespace TLN.Gameplay.Items
{
	[CreateAssetMenu(fileName = "ItemDefinition", menuName = "TLN/Items/Item Definition")]
	public class ItemDefinition : ScriptableObject
	{
		[Header("Identity")]
		[SerializeField] private string _id;
		[SerializeField] private string _displayName;

		[Header("Gameplay")]
		[SerializeField] private ItemCategory _category = ItemCategory.Misc;
		[SerializeField] private float _weight = 1f;
		[SerializeField] private bool _isStackable = true;
		[SerializeField] private int _maxStackSize = 1;

		[Header("UI")]
		[SerializeField] private Sprite _icon;

		[Header("Use")]
		[SerializeField] private ItemUseKind _useKind = ItemUseKind.None;

		public string Id => _id;
		public string DisplayName => _displayName;
		public ItemCategory Category => _category;
		public float Weight => _weight;
		public bool IsStackable => _isStackable;
		public int MaxStackSize => _maxStackSize;
		public Sprite Icon => _icon;
		public ItemUseKind UseKind => _useKind;
	}
}
