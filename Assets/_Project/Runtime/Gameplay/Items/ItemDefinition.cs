using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Localization;

namespace TLN.Gameplay.Items
{
	[CreateAssetMenu(fileName = "ItemDefinition", menuName = "TLN/Items/Item Definition")]
	public class ItemDefinition : ScriptableObject
	{
		[Header("Identity")]
		[SerializeField] private string _id;
		[SerializeField] private LocalizedString _displayName;
		[SerializeField] private LocalizedString _description;

		[Header("Gameplay")]
		[SerializeField] private ItemCategory _category = ItemCategory.Misc;
		[SerializeField] private float _weight = 1f;
		[SerializeField] private bool _isStackable = true;
		[SerializeField] private int _maxStackSize = 1;

		[Header("UI")]
		[SerializeField] private AssetReferenceSprite _iconReference;

		[Header("Use")]
		[SerializeField] private ItemUseKind _useKind = ItemUseKind.None;

		public string Id => _id;
		public string DisplayName => _displayName?.GetLocalizedString() ?? Id;
		public string Description => _description?.GetLocalizedString() ?? string.Empty;
		public ItemCategory Category => _category;
		public float Weight => _weight;
		public bool IsStackable => _isStackable;
		public int MaxStackSize => _maxStackSize;
		public AssetReferenceSprite IconReference => _iconReference;
		public ItemUseKind UseKind => _useKind;
	}
}