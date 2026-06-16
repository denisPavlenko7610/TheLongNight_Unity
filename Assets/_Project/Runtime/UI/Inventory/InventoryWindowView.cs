using System.Collections.Generic;
using TLN.Application.Assets;
using TLN.Gameplay.Inventory;
using TLN.Gameplay.Items;
using TLN.UI.Common;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;

namespace TLN.UI.Inventory
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class InventoryWindowView : MonoBehaviour, IInventoryWindow
    {
        private const string VisibleClassName = "inventory-window-root-visible";

        [SerializeField] private VisualTreeAsset _itemRowTemplate;

        private UIDocument _document;

        private VisualElement _root;
        private Label _weightLabel;
        private ScrollView _scrollView;

        private readonly List<InventoryItemRowView> _rowViews = new();

        private bool _isVisible;
        private IAddressableAssetService _addressableAssetService;

        private IInventoryService _inventoryService;
        private IItemUseService _itemUseService;

        [Inject]
        public void Construct(IInventoryService inventoryService, IItemUseService itemUseService,
            IAddressableAssetService addressableAssetService)
        {
            _inventoryService = inventoryService;
            _itemUseService = itemUseService;
            _addressableAssetService = addressableAssetService;

            _inventoryService.Changed += Refresh;

            Refresh();
            Hide();
        }

        private void Awake()
        {
            _document = GetComponent<UIDocument>();

            VisualElement documentRoot = _document.rootVisualElement;

            _root = documentRoot.RequiredQ<VisualElement>("inventory-window-root");
            _weightLabel = documentRoot.RequiredQ<Label>("inventory-weight-label");
            _scrollView = documentRoot.RequiredQ<ScrollView>("inventory-scroll-view");
        }

        private void OnDestroy()
        {
            if (_inventoryService != null)
            {
                _inventoryService.Changed -= Refresh;
            }

            ClearRows();
        }

        public void Toggle()
        {
            if (_isVisible)
            {
                Hide();
                return;
            }

            Show();
        }

        public void Hide()
        {
            _isVisible = false;
            _root.RemoveFromClassList(VisibleClassName);
        }

        private void Show()
        {
            _isVisible = true;
            Refresh();
            _root.AddToClassList(VisibleClassName);
        }

        private void Refresh()
        {
            if (_inventoryService == null || _root == null)
            {
                return;
            }

            RefreshWeight();
            RefreshItems();
        }

        private void RefreshWeight()
        {
            _weightLabel.text = $"{_inventoryService.CurrentWeight:0.##} / {_inventoryService.MaxCarryWeight:0.##} KG";
        }

        private void RefreshItems()
        {
            ClearRows();

            IReadOnlyList<ItemStack> items = _inventoryService.Items;

            for (int i = 0; i < items.Count; i++)
            {
                CreateRow(i, items[i]);
            }
        }

        private void CreateRow(int index, ItemStack stack)
        {
            if (_itemRowTemplate == null)
            {
                Debug.LogError("Inventory item row template is not assigned.", this);
                return;
            }

            TemplateContainer instance = _itemRowTemplate.Instantiate();
            VisualElement rowRoot = instance.Q<VisualElement>("inventory-item-row");

            if (rowRoot == null)
            {
                Debug.LogError("Inventory item row root was not found in template.", this);
                return;
            }

            InventoryItemRowView rowView = new InventoryItemRowView(rowRoot, _addressableAssetService);
            rowView.Bind(index, stack, OnUseClicked);

            _rowViews.Add(rowView);
            _scrollView.Add(instance);
        }

        private void OnUseClicked(int itemIndex)
        {
            if (_itemUseService == null)
            {
                return;
            }

            _itemUseService.UseItemAt(itemIndex);
            Refresh();
        }

        private void ClearRows()
        {
            for (int i = 0; i < _rowViews.Count; i++)
            {
                _rowViews[i].Dispose();
            }

            _rowViews.Clear();

            if (_scrollView != null)
            {
                _scrollView.Clear();
            }
        }
    }
}
