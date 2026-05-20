using System;
using TLN.Gameplay.Items;
using TLN.UI.Common;
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

		private int _itemIndex;
		private Action<int> _useClicked;

		public VisualElement Root => _root;

		public InventoryItemRowView(VisualElement root)
		{
			_root = root;

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

			_nameLabel.text = stack.Definition.DisplayName;
			_metaLabel.text = $"x{stack.Amount} / {stack.Definition.Weight * stack.Amount:0.##} kg";

			_useButton.SetEnabled(stack.Definition.UseKind != ItemUseKind.None);

			SetIcon(stack.Definition.Icon);
		}

		public void Dispose()
		{
			_useButton.clicked -= OnUseClicked;
		}

		private void OnUseClicked()
		{
			_useClicked?.Invoke(_itemIndex);
		}

		private void SetIcon(UnityEngine.Sprite sprite)
		{
			if (sprite == null)
			{
				_icon.style.backgroundImage = StyleKeyword.None;
				return;
			}

			_icon.style.backgroundImage = new StyleBackground(sprite);
		}
	}
}
