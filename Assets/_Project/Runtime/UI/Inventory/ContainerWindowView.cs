using System;
using System.Collections.Generic;
using TLN.Application.Assets;
using TLN.Application.Input;
using TLN.Application.Localization;
using TLN.Application.Notifications;
using TLN.Gameplay.Inventory;
using TLN.Gameplay.Items;
using TLN.UI.Common;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;

namespace TLN.UI.Inventory
{
	[RequireComponent(typeof(UIDocument))]
	public sealed class ContainerWindowView : MonoBehaviour, IContainerWindow
	{
		private const string VisibleClassName = "container-window-root-visible";
		private const float ListItemHeight = 52f;
		private const int SingleItemAmount = 1;

		private readonly List<ItemStack> _containerItems = new();
		private readonly List<ItemStack> _backpackItems = new();

		private VisualElement _root;
		private Label _titleLabel;
		private Button _takeAllButton;
		private Button _closeButton;
		private ListView _containerListView;
		private ListView _backpackListView;
		private Label _containerEmptyLabel;
		private Label _weightLabel;
		private VisualElement _weightBarFill;

		private WorldContainerActor _currentContainer;
		private IDisposable _inputModeScope;

		private IInventoryService _inventoryService;
		private IInputModeService _inputModeService;
		private INotificationService _notificationService;
		private IAddressableAssetService _addressableAssetService;

		private bool _isInitialized;

		[Inject]
		public void Construct(
			IInventoryService inventoryService,
			IInputModeService inputModeService,
			INotificationService notificationService,
			IAddressableAssetService addressableAssetService
		)
		{
			_inventoryService = inventoryService;
			_inputModeService = inputModeService;
			_notificationService = notificationService;
			_addressableAssetService = addressableAssetService;

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

			UnsubscribeFromCurrentContainer();
			ReleaseInputMode();
		}

		public void Show(WorldContainerActor container)
		{
			EnsureInitialized();

			UnsubscribeFromCurrentContainer();

			_currentContainer = container;

			if (_currentContainer != null)
			{
				_currentContainer.Changed += OnContainerChanged;
			}

			Refresh();

			_root.AddToClassList(VisibleClassName);
			AcquireInputMode();
		}

		public void Hide()
		{
			UnsubscribeFromCurrentContainer();

			_root?.RemoveFromClassList(VisibleClassName);
			ReleaseInputMode();
		}

		private void EnsureInitialized()
		{
			if (_isInitialized)
			{
				return;
			}

			VisualElement documentRoot = GetComponent<UIDocument>().rootVisualElement;

			_root = documentRoot.RequiredQ<VisualElement>("container-window-root");
			_titleLabel = documentRoot.RequiredQ<Label>("container-title-label");
			_takeAllButton = documentRoot.RequiredQ<Button>("container-take-all-button");
			_closeButton = documentRoot.RequiredQ<Button>("container-close-button");
			_containerListView = documentRoot.RequiredQ<ListView>("container-list-view");
			_backpackListView = documentRoot.RequiredQ<ListView>("container-backpack-list-view");
			_containerEmptyLabel = documentRoot.RequiredQ<Label>("container-empty-label");
			_weightLabel = documentRoot.RequiredQ<Label>("container-weight-label");
			_weightBarFill = documentRoot.RequiredQ<VisualElement>("container-weight-bar-fill");

			_titleLabel.text = Loc.ContainerTitle;
			_takeAllButton.text = Loc.ContainerTakeAll;
			_closeButton.text = Loc.SettingsBack;

			_takeAllButton.clicked += OnTakeAllClicked;
			_closeButton.clicked += Hide;

			_containerItems.Clear();
			_backpackItems.Clear();

			_containerListView.itemsSource = _containerItems;
			_containerListView.fixedItemHeight = ListItemHeight;
			_containerListView.selectionType = SelectionType.None;
			_containerListView.makeItem = MakeListItem;
			_containerListView.bindItem = (element, index) =>
				BindListItem(element, isContainerSide: true, index);

			_backpackListView.itemsSource = _backpackItems;
			_backpackListView.fixedItemHeight = ListItemHeight;
			_backpackListView.selectionType = SelectionType.None;
			_backpackListView.makeItem = MakeListItem;
			_backpackListView.bindItem = (element, index) =>
				BindListItem(element, isContainerSide: false, index);

			_isInitialized = true;
		}

		private VisualElement MakeListItem()
		{
			Button row = new Button();
			row.AddToClassList("container-row");

			VisualElement iconFrame = new VisualElement();
			iconFrame.AddToClassList("container-row-icon-frame");

			VisualElement icon = new VisualElement();
			icon.name = "container-row-icon";
			icon.AddToClassList("container-row-icon");

			iconFrame.Add(icon);

			VisualElement textColumn = new VisualElement();
			textColumn.AddToClassList("container-row-text-column");

			Label titleLabel = new Label();
			titleLabel.name = "container-row-title-label";
			titleLabel.AddToClassList("container-row-title");

			Label metaLabel = new Label();
			metaLabel.name = "container-row-meta-label";
			metaLabel.AddToClassList("container-row-meta");

			textColumn.Add(titleLabel);
			textColumn.Add(metaLabel);

			row.Add(iconFrame);
			row.Add(textColumn);

			row.RegisterCallback<PointerDownEvent>(OnRowPointerDown);

			return row;
		}

		private void OnRowPointerDown(PointerDownEvent evt)
		{
			if (evt.button != 0 || !(evt.currentTarget is VisualElement row))
			{
				return;
			}

			if (row.userData is not RowTarget target)
			{
				return;
			}

			int amount = evt.shiftKey ? int.MaxValue : SingleItemAmount;

			if (target.IsContainerSide)
			{
				TakeItem(target.Index, amount);
			}
			else
			{
				StoreItem(target.Index, amount);
			}
		}

		private void BindListItem(VisualElement element, bool isContainerSide, int index)
		{
			Label titleLabel = element.Q<Label>("container-row-title-label");
			Label metaLabel = element.Q<Label>("container-row-meta-label");
			VisualElement icon = element.Q<VisualElement>("container-row-icon");

			List<ItemStack> source = isContainerSide ? _containerItems : _backpackItems;

			element.userData = new RowTarget(isContainerSide, index);

			if (titleLabel == null || metaLabel == null || icon == null)
			{
				return;
			}

			icon.style.backgroundImage = StyleKeyword.None;
			icon.userData = null;

			if (index < 0 || index >= source.Count)
			{
				titleLabel.text = string.Empty;
				metaLabel.text = string.Empty;
				return;
			}

			ItemStack stack = source[index];
			ItemDefinition definition = stack.Definition;

			if (definition == null)
			{
				titleLabel.text = "Missing item";
				metaLabel.text = string.Empty;
				return;
			}

			titleLabel.text = stack.Amount > 1
				? $"{definition.DisplayName} x{stack.Amount}"
				: definition.DisplayName;

			float totalWeight = definition.Weight * stack.Amount;
			metaLabel.text = $"{totalWeight:0.##} kg";

			LoadRowIcon(icon, definition);
		}

		private sealed class RowTarget
		{
			public readonly bool IsContainerSide;
			public readonly int Index;

			public RowTarget(bool isContainerSide, int index)
			{
				IsContainerSide = isContainerSide;
				Index = index;
			}
		}

		private void LoadRowIcon(VisualElement icon, ItemDefinition definition)
		{
			if (icon == null || definition == null)
			{
				return;
			}

			if (definition.IconReference == null ||
			    !definition.IconReference.RuntimeKeyIsValid())
			{
				return;
			}

			icon.userData = definition;

			_addressableAssetService?.LoadSprite(definition.IconReference, sprite =>
			{
				if (!ReferenceEquals(icon.userData, definition))
				{
					return;
				}

				icon.style.backgroundImage = sprite == null
					? StyleKeyword.None
					: new StyleBackground(sprite);
			});
		}

		private void OnTakeAllClicked()
		{
			if (_currentContainer == null)
			{
				return;
			}

			int movedAmount = _currentContainer.TryTakeAll(
				_inventoryService,
				out int takenStacks,
				out string failureReason
			);

			if (movedAmount == 0)
			{
				_notificationService?.Show(failureReason);
				return;
			}

			_notificationService?.Show(Loc.ContainerTaken(movedAmount, takenStacks));
			Refresh();
		}

		private void TakeItem(int containerIndex, int amount)
		{
			if (_currentContainer == null || amount <= 0)
			{
				return;
			}

			if (containerIndex < 0 || containerIndex >= _currentContainer.Contents.Count)
			{
				return;
			}

			int requestedAmount = Mathf.Min(
				amount,
				_currentContainer.Contents[containerIndex].Amount
			);

			if (!_currentContainer.TryTakeItem(
				    _inventoryService,
				    containerIndex,
				    requestedAmount,
				    out string failureReason))
			{
				_notificationService?.Show(failureReason);
				return;
			}

			_notificationService?.Show(Loc.ContainerTaken(requestedAmount, SingleItemAmount));
		}

		private void StoreItem(int inventoryIndex, int amount)
		{
			if (_currentContainer == null || amount <= 0)
			{
				return;
			}

			if (_inventoryService == null ||
			    inventoryIndex < 0 ||
			    inventoryIndex >= _inventoryService.Items.Count)
			{
				return;
			}

			int requestedAmount = Mathf.Min(
				amount,
				_inventoryService.Items[inventoryIndex].Amount
			);

			if (!_currentContainer.TryStoreItem(
				    _inventoryService,
				    inventoryIndex,
				    requestedAmount,
				    out string failureReason))
			{
				_notificationService?.Show(failureReason);
				return;
			}

			_notificationService?.Show(Loc.ContainerStored(requestedAmount));
		}

		private void OnContainerChanged()
		{
			Refresh();
		}

		private void OnInventoryChanged()
		{
			Refresh();
		}

		private void Refresh()
		{
			_containerItems.Clear();
			_backpackItems.Clear();

			if (_currentContainer != null)
			{
				IReadOnlyList<ItemStack> contents = _currentContainer.Contents;

				for (int i = 0; i < contents.Count; i++)
				{
					_containerItems.Add(contents[i]);
				}
			}

			if (_inventoryService != null)
			{
				IReadOnlyList<ItemStack> items = _inventoryService.Items;

				for (int i = 0; i < items.Count; i++)
				{
					_backpackItems.Add(items[i]);
				}

				_weightLabel.text = Loc.WeightFormat(
					_inventoryService.CurrentWeight,
					_inventoryService.MaxCarryWeight
				);

				RefreshWeightBar();
			}

			_containerListView.RefreshItems();
			_backpackListView.RefreshItems();

			_containerEmptyLabel.SetVisible(_containerItems.Count == 0);
			_takeAllButton.SetEnabled(_containerItems.Count > 0);
		}

		private void RefreshWeightBar()
		{
			if (_weightBarFill == null || _inventoryService == null)
			{
				return;
			}

			float maxWeight = Mathf.Max(0.01f, _inventoryService.MaxCarryWeight);
			float ratio = Mathf.Clamp01(_inventoryService.CurrentWeight / maxWeight);

			_weightBarFill.style.width = Length.Percent(ratio * 100f);

			Color fillColor = ratio >= OverweightRatio
				? OverweightFillColor
				: ratio >= HeavyRatio
					? HeavyFillColor
					: NormalFillColor;

			_weightBarFill.style.backgroundColor = new StyleColor(fillColor);
		}

		private const float HeavyRatio = 0.75f;
		private const float OverweightRatio = 0.95f;
		private static readonly Color NormalFillColor = new Color(0.92f, 0.95f, 0.96f, 0.85f);
		private static readonly Color HeavyFillColor = new Color(0.9f, 0.76f, 0.47f, 0.95f);
		private static readonly Color OverweightFillColor = new Color(0.83f, 0.37f, 0.33f, 0.95f);

		private void UnsubscribeFromCurrentContainer()
		{
			if (_currentContainer != null)
			{
				_currentContainer.Changed -= OnContainerChanged;
			}

			_currentContainer = null;
		}

		private void AcquireInputMode()
		{
			_inputModeScope ??= _inputModeService?.AcquireUIMode();
		}

		private void ReleaseInputMode()
		{
			_inputModeScope?.Dispose();
			_inputModeScope = null;
		}
	}
}
