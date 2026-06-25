using System;
using TLN.Application.Assets;
using TLN.Application.Localization;
using TLN.Gameplay.Equipment;
using TLN.Gameplay.Items;
using TLN.UI.Common;
using UnityEngine;
using UnityEngine.UIElements;

namespace TLN.UI.Inventory
{
	public sealed class InventoryItemRowView
	{
		private readonly VisualElement _root;
		private readonly VisualElement _icon;
		private readonly Label _nameLabel;
		private readonly Label _metaLabel;
		private readonly Button _useButton;
		private readonly IAddressableAssetService _addressableAssetService;

		private int _itemIndex;
		private int _iconRequestVersion;
		private bool _isDisposed;
		private Action<int> _useClicked;

		public VisualElement Root => _root;

		public InventoryItemRowView(VisualElement root, IAddressableAssetService addressableAssetService)
		{
			_root = root;
			_addressableAssetService = addressableAssetService;

			_icon = root.RequiredQ<VisualElement>("item-icon");
			_nameLabel = root.RequiredQ<Label>("item-name-label");
			_metaLabel = root.RequiredQ<Label>("item-meta-label");
			_useButton = root.RequiredQ<Button>("item-use-button");

			_useButton.clicked += OnUseClicked;
		}

		public void Bind(int itemIndex, ItemStack stack, Action<int> useClicked)
		{
			_itemIndex = itemIndex;
			_useClicked = useClicked;
			_isDisposed = false;

			_nameLabel.text = stack.Definition.DisplayName;
			_metaLabel.text = CreateMetaText(stack);
			_useButton.SetEnabled(stack.Definition.UseKind != ItemUseKind.None);

			SetIcon(stack.Definition);
		}

		public void Dispose()
		{
			_isDisposed = true;
			_iconRequestVersion++;

			_useButton.clicked -= OnUseClicked;
		}

		private string CreateMetaText(ItemStack stack)
		{
			string weightText = string.Format(LocalizationKeys.WeightKg, stack.Definition.Weight * stack.Amount, 0f);

			if (stack.Definition is ClothingItemDefinition clothing)
			{
				return string.Format(LocalizationKeys.MetaClothing, stack.Amount, weightText, clothing.Slot, clothing.WarmthBonus);
			}

			return string.Format(LocalizationKeys.MetaDefault, stack.Amount, weightText);
		}

		private void SetIcon(ItemDefinition definition)
		{
			_iconRequestVersion++;
			int requestVersion = _iconRequestVersion;

			_icon.style.backgroundImage = StyleKeyword.None;

			if (definition == null)
			{
				return;
			}

			if (definition.IconReference == null || !definition.IconReference.RuntimeKeyIsValid())
			{
				return;
			}

			_addressableAssetService?.LoadSprite(
				definition.IconReference,
				sprite =>
				{
					if (_isDisposed)
					{
						return;
					}

					if (requestVersion != _iconRequestVersion)
					{
						return;
					}

					SetDirectIcon(sprite);
				}
			);
		}

		private void SetDirectIcon(Sprite sprite)
		{
			if (sprite == null)
			{
				_icon.style.backgroundImage = StyleKeyword.None;
				return;
			}

			_icon.style.backgroundImage = new StyleBackground(sprite);
		}

		private void OnUseClicked()
		{
			_useClicked?.Invoke(_itemIndex);
		}
	}
}
