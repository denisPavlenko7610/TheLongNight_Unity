using System;
using TLN.Gameplay.Equipment;
using TLN.Gameplay.Items;
using TLN.UI.Common;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
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

        private AsyncOperationHandle<Sprite> _iconLoadHandle;
        private bool _hasIconLoadHandle;
        private bool _isDisposed;

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
            ReleaseIconHandle();

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
            _useButton.clicked -= OnUseClicked;
            ReleaseIconHandle();
        }

        private static string CreateMetaText(ItemStack stack)
        {
            string weightText = $"{stack.Definition.Weight * stack.Amount:0.##} kg";

            if (stack.Definition is ClothingItemDefinition clothing)
            {
                return $"x{stack.Amount} / {weightText} / {clothing.Slot} / +{clothing.WarmthBonus:0.#} warmth";
            }

            return $"x{stack.Amount} / {weightText}";
        }

        private void SetIcon(ItemDefinition definition)
        {
            _icon.style.backgroundImage = StyleKeyword.None;

            if (definition == null)
            {
                return;
            }

            AssetReferenceT<Sprite> iconReference = definition.IconReference;

            if (iconReference != null && iconReference.RuntimeKeyIsValid())
            {
                LoadAddressableIcon(iconReference);
                return;
            }

            SetDirectIcon(definition.Icon);
        }

        private void LoadAddressableIcon(AssetReferenceT<Sprite> iconReference)
        {
            _iconLoadHandle = iconReference.LoadAssetAsync<Sprite>();
            _hasIconLoadHandle = true;
            _iconLoadHandle.Completed += OnIconLoaded;
        }

        private void OnIconLoaded(AsyncOperationHandle<Sprite> handle)
        {
            if (_isDisposed)
            {
                return;
            }

            if (handle.Status != AsyncOperationStatus.Succeeded)
            {
                return;
            }

            SetDirectIcon(handle.Result);
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

        private void ReleaseIconHandle()
        {
            if (!_hasIconLoadHandle)
            {
                return;
            }

            if (_iconLoadHandle.IsValid())
            {
                _iconLoadHandle.Completed -= OnIconLoaded;
                Addressables.Release(_iconLoadHandle);
            }

            _hasIconLoadHandle = false;
            _iconLoadHandle = default;
        }

        private void OnUseClicked()
        {
            _useClicked?.Invoke(_itemIndex);
        }
    }
}
