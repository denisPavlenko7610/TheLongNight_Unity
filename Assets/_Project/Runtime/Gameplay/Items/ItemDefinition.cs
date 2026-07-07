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
		public string DisplayName => GetLocalizedStringOrFallback(_displayName, Id);
		public string Description => GetLocalizedStringOrFallback(_description, string.Empty);
		public ItemCategory Category => _category;
		public float Weight => Mathf.Max(0f, _weight);
		public bool IsStackable => _isStackable;
		public int MaxStackSize => _isStackable ? Mathf.Max(1, _maxStackSize) : 1;
		public AssetReferenceSprite IconReference => _iconReference;
		public ItemUseKind UseKind => _useKind;

		private static string GetLocalizedStringOrFallback(LocalizedString localizedString, string fallback)
		{
			if (localizedString == null || localizedString.IsEmpty)
			{
				return fallback;
			}

			string value = localizedString.GetLocalizedString();
			return string.IsNullOrWhiteSpace(value) ? fallback : value;
		}
	}
}
