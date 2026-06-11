using System;
using System.Collections.Generic;
using TLN.Application.Assets;
using TLN.Application.Input;
using TLN.Application.Notifications;
using TLN.Gameplay.Building;
using TLN.Gameplay.Equipment;
using TLN.Gameplay.Inventory;
using TLN.Gameplay.Items;
using TLN.UI.Common;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;

namespace TLN.UI.SurvivalMenu
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class SurvivalMenuWindowView : MonoBehaviour, IInventoryWindow, IBuildWindow
    {
        private const string VisibleClassName = "survival-menu-root-visible";
        private const string SelectedTabClassName = "survival-menu-tab-selected";

        private readonly List<object> _listItems = new();

        private VisualElement _root;
        private Button _inventoryTabButton;
        private Button _craftingTabButton;
        private Button _foodCategoryButton;
        private Button _medicineCategoryButton;
        private Button _toolsCategoryButton;
        private Button _clothesCategoryButton;
        private Button _fireCategoryButton;
        private Button _closeButton;
        private ListView _listView;
        private VisualElement _detailsIcon;
        private Label _detailsTitleLabel;
        private Label _detailsMetaLabel;
        private Label _detailsDescriptionLabel;
        private Button _actionButton;
        private Label _headerTitleLabel;
        private Label _weightLabel;

        private IInventoryService _inventoryService;
        private IItemUseService _itemUseService;
        private IAddressableAssetService _addressableAssetService;
        private BuildRecipeCatalog _recipeCatalog;
        private IBuildService _buildService;
        private IInputModeService _inputModeService;
        private INotificationService _notificationService;

        private bool _isInitialized;
        private bool _isVisible;
        private SurvivalMenuTabId _currentTab = SurvivalMenuTabId.Inventory;
        private object _selectedItem;
        private int _iconRequestVersion;
        private ItemCategory? _currentInventoryCategoryFilter;

        [Inject]
        public void Construct(
            IInventoryService inventoryService,
            IItemUseService itemUseService,
            IAddressableAssetService addressableAssetService,
            BuildRecipeCatalog recipeCatalog,
            IBuildService buildService,
            IInputModeService inputModeService,
            INotificationService notificationService)
        {
            _inventoryService = inventoryService;
            _itemUseService = itemUseService;
            _addressableAssetService = addressableAssetService;
            _recipeCatalog = recipeCatalog;
            _buildService = buildService;
            _inputModeService = inputModeService;
            _notificationService = notificationService;

            EnsureInitialized();

            if (_inventoryService != null)
            {
                _inventoryService.Changed += OnInventoryChanged;
            }

            Hide();
        }

        private void Awake()
        {
            EnsureInitialized();
        }

        private void OnDestroy()
        {
            if (_inventoryService != null)
            {
                _inventoryService.Changed -= OnInventoryChanged;
            }

            if (_inventoryTabButton != null)
            {
                _inventoryTabButton.clicked -= OnInventoryTabClicked;
            }

            if (_craftingTabButton != null)
            {
                _craftingTabButton.clicked -= OnCraftingTabClicked;
            }

            if (_closeButton != null)
            {
                _closeButton.clicked -= Hide;
            }

            if (_actionButton != null)
            {
                _actionButton.clicked -= OnActionClicked;
            }

            if (_listView != null)
            {
                _listView.selectionChanged -= OnSelectionChanged;
            }

            if (_foodCategoryButton != null)
            {
                _foodCategoryButton.clicked -= OnFoodCategoryClicked;
            }

            if (_medicineCategoryButton != null)
            {
                _medicineCategoryButton.clicked -= OnMedicineCategoryClicked;
            }

            if (_fireCategoryButton != null)
            {
                _fireCategoryButton.clicked -= OnFireCategoryClicked;
            }

            if (_toolsCategoryButton != null)
            {
                _toolsCategoryButton.clicked -= OnToolsCategoryClicked;
            }

            if (_clothesCategoryButton != null)
            {
                _clothesCategoryButton.clicked -= OnClothesCategoryClicked;
            }
        }

        void IInventoryWindow.Toggle()
        {
            Toggle(SurvivalMenuTabId.Inventory);
        }

        void IInventoryWindow.Hide()
        {
            Hide();
        }

        void IBuildWindow.Toggle()
        {
            Toggle(SurvivalMenuTabId.Crafting);
        }

        void IBuildWindow.Hide()
        {
            Hide();
        }

        public void Hide()
        {
            _isVisible = false;

            if (_root != null)
            {
                _root.RemoveFromClassList(VisibleClassName);
                _root.SetVisible(false);
            }

            _inputModeService?.SetGameplayMode();
        }

        private void Toggle(SurvivalMenuTabId tab)
        {
            if (_isVisible && _currentTab == tab)
            {
                Hide();
                return;
            }

            Show(tab);
        }

        private void Show(SurvivalMenuTabId tab)
        {
            EnsureInitialized();

            _isVisible = true;
            _currentTab = tab;

            Refresh();
            _root.AddToClassList(VisibleClassName);
            _root.SetVisible(true);
            _inputModeService?.SetUIMode();
        }

        private void EnsureInitialized()
        {
            if (_isInitialized)
            {
                return;
            }

            VisualElement documentRoot = GetComponent<UIDocument>().rootVisualElement;

            _root = documentRoot.RequiredQ<VisualElement>("survival-menu-root");
            _root.SetVisible(false);
            _headerTitleLabel = documentRoot.Q<Label>("survival-header-title-label");
            _weightLabel = documentRoot.Q<Label>("survival-weight-label");
            _inventoryTabButton = documentRoot.RequiredQ<Button>("survival-inventory-tab-button");
            _foodCategoryButton = documentRoot.RequiredQ<Button>("survival-food-category-button");
            _medicineCategoryButton = documentRoot.RequiredQ<Button>("survival-medicine-category-button");
            _toolsCategoryButton = documentRoot.RequiredQ<Button>("survival-tools-category-button");
            _clothesCategoryButton = documentRoot.RequiredQ<Button>("survival-clothes-category-button");
            _craftingTabButton = documentRoot.RequiredQ<Button>("survival-crafting-tab-button");
            _closeButton = documentRoot.RequiredQ<Button>("survival-close-button");
            _listView = documentRoot.RequiredQ<ListView>("survival-list-view");
            _detailsIcon = documentRoot.RequiredQ<VisualElement>("survival-details-icon");
            _detailsTitleLabel = documentRoot.RequiredQ<Label>("survival-details-title-label");
            _detailsMetaLabel = documentRoot.RequiredQ<Label>("survival-details-meta-label");
            _detailsDescriptionLabel = documentRoot.RequiredQ<Label>("survival-details-description-label");
            _actionButton = documentRoot.RequiredQ<Button>("survival-action-button");
            _fireCategoryButton = documentRoot.RequiredQ<Button>("survival-fire-category-button");

            _inventoryTabButton.clicked += OnInventoryTabClicked;
            _foodCategoryButton.clicked += OnFoodCategoryClicked;
            _fireCategoryButton.clicked += OnFireCategoryClicked;
            _medicineCategoryButton.clicked += OnMedicineCategoryClicked;
            _toolsCategoryButton.clicked += OnToolsCategoryClicked;
            _clothesCategoryButton.clicked += OnClothesCategoryClicked;
            _craftingTabButton.clicked += OnCraftingTabClicked;
            _closeButton.clicked += Hide;
            _actionButton.clicked += OnActionClicked;

            SetupListView();

            _isInitialized = true;
        }

        private void SetupListView()
        {
            _listView.itemsSource = _listItems;
            _listView.fixedItemHeight = 62f;
            _listView.selectionType = SelectionType.Single;
            _listView.makeItem = MakeListItem;
            _listView.bindItem = BindListItem;
            _listView.selectionChanged += OnSelectionChanged;
        }

        private VisualElement MakeListItem()
        {
            VisualElement root = new VisualElement();
            root.AddToClassList("survival-menu-list-item");

            VisualElement iconFrame = new VisualElement();
            iconFrame.AddToClassList("survival-menu-list-item-icon-frame");

            VisualElement icon = new VisualElement();
            icon.name = "survival-row-icon";
            icon.AddToClassList("survival-menu-list-item-icon");

            iconFrame.Add(icon);

            VisualElement textColumn = new VisualElement();
            textColumn.AddToClassList("survival-menu-list-item-text-column");

            Label titleLabel = new Label();
            titleLabel.name = "survival-row-title-label";
            titleLabel.AddToClassList("survival-menu-list-item-title");

            Label metaLabel = new Label();
            metaLabel.name = "survival-row-meta-label";
            metaLabel.AddToClassList("survival-menu-list-item-meta");

            textColumn.Add(titleLabel);
            textColumn.Add(metaLabel);

            root.Add(iconFrame);
            root.Add(textColumn);

            return root;
        }

        private void BindListItem(VisualElement element, int index)
        {
            Label titleLabel = element.Q<Label>("survival-row-title-label");
            Label metaLabel = element.Q<Label>("survival-row-meta-label");
            VisualElement icon = element.Q<VisualElement>("survival-row-icon");

            if (titleLabel == null || metaLabel == null || icon == null)
            {
                return;
            }

            icon.style.backgroundImage = StyleKeyword.None;
            icon.userData = null;

            if (index < 0 || index >= _listItems.Count)
            {
                titleLabel.text = string.Empty;
                metaLabel.text = string.Empty;
                return;
            }

            object item = _listItems[index];

            switch (item)
            {
                case InventoryRowData inventoryItem:
                    BindInventoryListItem(titleLabel, metaLabel, icon, inventoryItem);
                    break;

                case RecipeRowData recipeItem:
                    BindRecipeListItem(titleLabel, metaLabel, icon, recipeItem);
                    break;

                default:
                    titleLabel.text = string.Empty;
                    metaLabel.text = string.Empty;
                    break;
            }
        }

        private void BindInventoryListItem(
            Label titleLabel,
            Label metaLabel,
            VisualElement icon,
            InventoryRowData item)
        {
            titleLabel.text = item.Name;
            metaLabel.text = CreateInventoryRowMetaText(item);

            if (item.Definition == null)
            {
                return;
            }

            LoadRowIcon(icon, item.Definition);
        }

        private static void BindRecipeListItem(
            Label titleLabel,
            Label metaLabel,
            VisualElement icon,
            RecipeRowData item)
        {
            titleLabel.text = item.Name;
            metaLabel.text = item.Recipe == null
                ? "Missing recipe"
                : "Crafting recipe";

            icon.style.backgroundImage = StyleKeyword.None;
        }

        private static string CreateInventoryRowMetaText(InventoryRowData item)
        {
            if (item.Definition == null)
            {
                return "Missing item";
            }

            float totalWeight = item.Definition.Weight * item.Amount;

            return $"{item.Definition.Category} / x{item.Amount} / {totalWeight:0.##} kg";
        }

        private void LoadRowIcon(VisualElement icon, ItemDefinition definition)
        {
            if (icon == null || definition == null)
            {
                return;
            }

            icon.style.backgroundImage = StyleKeyword.None;

            if (definition.IconReference == null ||
                !definition.IconReference.RuntimeKeyIsValid())
            {
                return;
            }

            string expectedItemId = definition.Id;
            icon.userData = expectedItemId;

            _addressableAssetService?.LoadSprite(definition.IconReference, sprite =>
            {
                if (icon == null)
                {
                    return;
                }

                if (!Equals(icon.userData, expectedItemId))
                {
                    return;
                }

                icon.style.backgroundImage = sprite == null
                    ? StyleKeyword.None
                    : new StyleBackground(sprite);
            });
        }

        private void OnInventoryTabClicked()
        {
            ShowInventoryCategory(null);
        }

        private void OnFoodCategoryClicked()
        {
            ShowInventoryCategory(ItemCategory.Food);
        }

        private void OnFireCategoryClicked()
        {
            ShowInventoryCategory(ItemCategory.Fuel);
        }

        private void OnMedicineCategoryClicked()
        {
            ShowInventoryCategory(ItemCategory.Medicine);
        }

        private void OnToolsCategoryClicked()
        {
            ShowInventoryCategory(ItemCategory.Tool);
        }

        private void OnClothesCategoryClicked()
        {
            ShowInventoryCategory(ItemCategory.Clothing);
        }

        private void OnCraftingTabClicked()
        {
            _currentInventoryCategoryFilter = null;
            Show(SurvivalMenuTabId.Crafting);
        }

        private void ShowInventoryCategory(ItemCategory? categoryFilter)
        {
            _currentInventoryCategoryFilter = categoryFilter;
            Show(SurvivalMenuTabId.Inventory);
        }

        private void OnInventoryChanged()
        {
            if (!_isVisible)
            {
                return;
            }

            Refresh();
        }

        private void Refresh()
        {
            RefreshHeader();
            ApplyTabVisualState();
            RebuildListItems();
            SelectFirstItemIfNeeded();
            RefreshListView();
            RefreshDetails();
        }

        private void RefreshHeader()
        {
            if (_headerTitleLabel != null)
            {
                _headerTitleLabel.text = _currentTab == SurvivalMenuTabId.Inventory
                    ? GetInventoryHeaderTitle()
                    : "CRAFTING";
            }

            if (_weightLabel == null || _inventoryService == null)
            {
                return;
            }

            _weightLabel.text = $"{_inventoryService.CurrentWeight:0.00} / {_inventoryService.MaxCarryWeight:0.00} KG";
        }

        private string GetInventoryHeaderTitle()
        {
            if (!_currentInventoryCategoryFilter.HasValue)
            {
                return "BACKPACK";
            }

            return _currentInventoryCategoryFilter.Value switch
            {
                ItemCategory.Food => "FOOD",
                ItemCategory.Water => "WATER",
                ItemCategory.Medicine => "MEDICINE",
                ItemCategory.Tool => "TOOLS",
                ItemCategory.Clothing => "CLOTHING",
                _ => _currentInventoryCategoryFilter.Value.ToString().ToUpperInvariant()
            };
        }

        private void ApplyTabVisualState()
        {
            bool isInventoryTab = _currentTab == SurvivalMenuTabId.Inventory;

            SetTabSelected(
                _inventoryTabButton,
                isInventoryTab && !_currentInventoryCategoryFilter.HasValue);

            SetTabSelected(
                _foodCategoryButton,
                isInventoryTab && _currentInventoryCategoryFilter == ItemCategory.Food);

            SetTabSelected(
                _fireCategoryButton,
                isInventoryTab && _currentInventoryCategoryFilter == ItemCategory.Fuel);

            SetTabSelected(
                _medicineCategoryButton,
                isInventoryTab && _currentInventoryCategoryFilter == ItemCategory.Medicine);

            SetTabSelected(
                _toolsCategoryButton,
                isInventoryTab && _currentInventoryCategoryFilter == ItemCategory.Tool);

            SetTabSelected(
                _clothesCategoryButton,
                isInventoryTab && _currentInventoryCategoryFilter == ItemCategory.Clothing);

            SetTabSelected(
                _craftingTabButton,
                _currentTab == SurvivalMenuTabId.Crafting);
        }

        private static void SetTabSelected(Button button, bool isSelected)
        {
            if (button == null)
            {
                return;
            }

            if (isSelected)
            {
                button.AddToClassList(SelectedTabClassName);
                return;
            }

            button.RemoveFromClassList(SelectedTabClassName);
        }

        private void RebuildListItems()
        {
            _listItems.Clear();

            switch (_currentTab)
            {
                case SurvivalMenuTabId.Inventory:
                    AddInventoryItems();
                    break;

                case SurvivalMenuTabId.Crafting:
                    AddRecipeItems();
                    break;
            }
        }

        private void AddInventoryItems()
        {
            if (_inventoryService == null)
            {
                return;
            }

            IReadOnlyList<ItemStack> items = _inventoryService.Items;
            for (int i = 0; i < items.Count; i++)
            {
                ItemStack stack = items[i];

                if (!ShouldShowInventoryStack(stack))
                {
                    continue;
                }

                _listItems.Add(new InventoryRowData(i, stack.Definition, stack.Amount));
            }
        }

        private bool ShouldShowInventoryStack(ItemStack stack)
        {
            if (!_currentInventoryCategoryFilter.HasValue)
            {
                return true;
            }

            if (stack.Definition == null)
            {
                return false;
            }

            ItemCategory selectedCategory = _currentInventoryCategoryFilter.Value;
            ItemCategory itemCategory = stack.Definition.Category;

            return selectedCategory switch
            {
                ItemCategory.Food => itemCategory is ItemCategory.Food or ItemCategory.Water,
                ItemCategory.Fuel => itemCategory == ItemCategory.Fuel,
                _ => itemCategory == selectedCategory
            };
        }

        private void AddRecipeItems()
        {
            if (_recipeCatalog == null || _recipeCatalog.Recipes == null)
            {
                return;
            }

            foreach (BuildRecipeDefinition recipe in _recipeCatalog.Recipes)
            {
                if (recipe == null)
                {
                    continue;
                }

                _listItems.Add(new RecipeRowData(recipe));
            }
        }

        private void SelectFirstItemIfNeeded()
        {
            if (_listItems.Count == 0)
            {
                _selectedItem = null;
                return;
            }

            if (_selectedItem == null || !_listItems.Contains(_selectedItem))
            {
                _selectedItem = _listItems[0];
            }
        }

        private void RefreshListView()
        {
            _listView.RefreshItems();

            int selectedIndex = _selectedItem == null
                ? -1
                : _listItems.IndexOf(_selectedItem);

            _listView.selectedIndex = selectedIndex;
        }

        private void OnSelectionChanged(IEnumerable<object> selectedItems)
        {
            foreach (object selected in selectedItems)
            {
                _selectedItem = selected;
                RefreshDetails();
                return;
            }

            _selectedItem = null;
            RefreshDetails();
        }

        private void RefreshDetails()
        {
            _iconRequestVersion++;
            _detailsIcon.style.backgroundImage = StyleKeyword.None;

            _actionButton.SetVisible(false);
            _actionButton.SetEnabled(false);

            switch (_selectedItem)
            {
                case InventoryRowData inventoryItem:
                    ShowInventoryDetails(inventoryItem);
                    break;

                case RecipeRowData recipeItem:
                    ShowRecipeDetails(recipeItem);
                    break;

                default:
                    ShowEmptyDetails();
                    break;
            }
        }

        private void ShowInventoryDetails(InventoryRowData item)
        {
            if (item.Definition == null)
            {
                ShowEmptyDetails();
                return;
            }

            _detailsTitleLabel.text = item.Name;
            _detailsMetaLabel.text = CreateInventoryMetaText(item);
            _detailsDescriptionLabel.text = CreateItemDescription(item.Definition);

            LoadIcon(item.Definition);

            _actionButton.text = "USE";
            _actionButton.SetVisible(true);
            _actionButton.SetEnabled(item.Definition.UseKind != ItemUseKind.None);
        }

        private void ShowRecipeDetails(RecipeRowData item)
        {
            if (item.Recipe == null)
            {
                ShowEmptyDetails();
                return;
            }

            _detailsTitleLabel.text = item.Name;
            _detailsMetaLabel.text = CreateRecipeRequirementsText(item.Recipe);
            _detailsDescriptionLabel.text = GetBuildStateText(item.Recipe);

            bool canBuild = _buildService != null &&
                            _buildService.CanBuild(item.Recipe, out _);

            _actionButton.text = "BUILD";
            _actionButton.SetVisible(true);
            _actionButton.SetEnabled(canBuild);
        }

        private void ShowEmptyDetails()
        {
            if (_currentTab == SurvivalMenuTabId.Inventory)
            {
                _detailsTitleLabel.text = GetInventoryHeaderTitle();
                _detailsMetaLabel.text = "Nothing selected.";
                _detailsDescriptionLabel.text = _currentInventoryCategoryFilter.HasValue
                    ? "No items in this category."
                    : "Your backpack is empty.";
                return;
            }

            _detailsTitleLabel.text = "Crafting";
            _detailsMetaLabel.text = "Nothing selected.";
            _detailsDescriptionLabel.text = "No recipe selected.";
        }

        private static string CreateInventoryMetaText(InventoryRowData item)
        {
            ItemDefinition definition = item.Definition;

            string weightText = $"{definition.Weight * item.Amount:0.##} kg";

            if (definition is ClothingItemDefinition clothing)
            {
                return $"{definition.Category} / {weightText} / {clothing.Slot} / +{clothing.WarmthBonus:0.#} warmth";
            }

            return $"{definition.Category} / {weightText}";
        }

        private static string CreateRecipeRequirementsText(BuildRecipeDefinition recipe)
        {
            if (recipe.Ingredients == null || recipe.Ingredients.Count == 0)
            {
                return "Requires: nothing.";
            }

            List<string> parts = new List<string>();

            foreach (BuildRecipeIngredient ingredient in recipe.Ingredients)
            {
                if (ingredient == null || ingredient.Item == null)
                {
                    continue;
                }

                parts.Add($"{ingredient.Item.DisplayName} x{ingredient.Amount}");
            }

            return parts.Count == 0
                ? "Requires: nothing."
                : $"Requires: {string.Join(", ", parts)}";
        }

        private string GetBuildStateText(BuildRecipeDefinition recipe)
        {
            if (_buildService == null)
            {
                return "Build service is missing.";
            }

            bool canBuild = _buildService.CanBuild(recipe, out string failureReason);

            return canBuild
                ? "Ready to build."
                : failureReason;
        }

        private static string CreateItemDescription(ItemDefinition definition)
        {
            return definition.UseKind switch
            {
                ItemUseKind.Consumable => "Can be consumed to affect your survival stats.",
                ItemUseKind.Placeable => "Can be placed in the world.",
                ItemUseKind.Clothing => "Can be equipped for warmth.",
                _ => "No available action."
            };
        }

        private void LoadIcon(ItemDefinition definition)
        {
            int requestVersion = _iconRequestVersion;

            if (definition.IconReference == null ||
                !definition.IconReference.RuntimeKeyIsValid())
            {
                return;
            }

            _addressableAssetService?.LoadSprite(definition.IconReference, sprite =>
            {
                if (requestVersion != _iconRequestVersion)
                {
                    return;
                }

                _detailsIcon.style.backgroundImage = sprite == null
                    ? StyleKeyword.None
                    : new StyleBackground(sprite);
            });
        }

        private void OnActionClicked()
        {
            switch (_selectedItem)
            {
                case InventoryRowData inventoryItem:
                    UseInventoryItem(inventoryItem);
                    break;

                case RecipeRowData recipeItem:
                    BuildRecipe(recipeItem);
                    break;
            }
        }

        private void UseInventoryItem(InventoryRowData item)
        {
            if (_itemUseService == null)
            {
                _notificationService?.Show("Item use service is missing.");
                return;
            }

            _itemUseService.UseItemAt(item.InventoryIndex);
            Refresh();
        }

        private void BuildRecipe(RecipeRowData item)
        {
            if (_buildService == null)
            {
                _notificationService?.Show("Build service is missing.");
                return;
            }

            BuildResult result = _buildService.Build(item.Recipe);

            if (result.IsSuccess)
            {
                Hide();
                return;
            }

            Refresh();
        }

        private readonly struct InventoryRowData
        {
            public int InventoryIndex { get; }
            public ItemDefinition Definition { get; }
            public int Amount { get; }

            public string Name => Definition == null
                ? "Missing item"
                : Definition.DisplayName;

            public InventoryRowData(
                int inventoryIndex,
                ItemDefinition definition,
                int amount)
            {
                InventoryIndex = inventoryIndex;
                Definition = definition;
                Amount = amount;
            }
        }

        private readonly struct RecipeRowData
        {
            public BuildRecipeDefinition Recipe { get; }

            public string Name => Recipe == null
                ? "Missing recipe"
                : Recipe.DisplayName;

            public RecipeRowData(BuildRecipeDefinition recipe)
            {
                Recipe = recipe;
            }
        }
    }
}
