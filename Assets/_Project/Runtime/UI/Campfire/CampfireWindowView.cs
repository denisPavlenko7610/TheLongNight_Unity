using System;
using TLN.Application.Input;
using TLN.Application.Localization;
using TLN.Application.Notifications;
using TLN.Application.Saves;
using TLN.Gameplay.Campfire;
using TLN.Gameplay.Inventory;
using TLN.Gameplay.Items;
using TLN.UI.Common;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;

namespace TLN.UI.Campfire
{
	[RequireComponent(typeof(UIDocument))]
	public sealed class CampfireWindowView : MonoBehaviour, ICampfireWindow
	{
		private const string VisibleClassName = "campfire-window-root-visible";
		private const string WaterItemId = "water_purified";

		private VisualElement _root;
		private Label _stateLabel;
		private Label _fuelLabel;
		private Button _addFuelButton;
		private Button _meltSnowButton;
		private Button _igniteButton;
		private Button _extinguishButton;
		private Button _closeButton;

		private CampfireActor _currentCampfire;
		private IDisposable _inputModeScope;

		private IInventoryService _inventoryService;
		private IInputModeService _inputModeService;
		private INotificationService _notificationService;
		private IGameSaveService _gameSaveService;
		private ItemCatalog _itemCatalog;

		[Inject]
		public void Construct(
			IInventoryService inventoryService,
			IInputModeService inputModeService,
			INotificationService notificationService,
			IGameSaveService gameSaveService,
			ItemCatalog itemCatalog
		)
		{
			_inventoryService = inventoryService;
			_inputModeService = inputModeService;
			_notificationService = notificationService;
			_gameSaveService = gameSaveService;
			_itemCatalog = itemCatalog;

			Hide();
		}

		private void Awake()
		{
			UIDocument document = GetComponent<UIDocument>();
			VisualElement documentRoot = document.rootVisualElement;

			_root = documentRoot.RequiredQ<VisualElement>("campfire-window-root");
			_stateLabel = documentRoot.RequiredQ<Label>("campfire-state-label");
			_fuelLabel = documentRoot.RequiredQ<Label>("campfire-fuel-label");
			_addFuelButton = documentRoot.RequiredQ<Button>("campfire-add-fuel-button");
			_meltSnowButton = documentRoot.RequiredQ<Button>("campfire-melt-snow-button");
			_igniteButton = documentRoot.RequiredQ<Button>("campfire-ignite-button");
			_extinguishButton = documentRoot.RequiredQ<Button>("campfire-extinguish-button");
			_closeButton = documentRoot.RequiredQ<Button>("campfire-close-button");

			_meltSnowButton.text = Loc.CampfireMeltSnow;
			_addFuelButton.clicked += OnAddFuelClicked;
			_meltSnowButton.clicked += OnMeltSnowClicked;
			_igniteButton.clicked += OnIgniteClicked;
			_extinguishButton.clicked += OnExtinguishClicked;
			_closeButton.clicked += Hide;
		}

		private void OnDestroy()
		{
			_addFuelButton.clicked -= OnAddFuelClicked;
			_meltSnowButton.clicked -= OnMeltSnowClicked;
			_igniteButton.clicked -= OnIgniteClicked;
			_extinguishButton.clicked -= OnExtinguishClicked;
			_closeButton.clicked -= Hide;

			ReleaseInputMode();
			UnsubscribeFromCurrentCampfire();
		}

		public void Show(CampfireActor campfire)
		{
			UnsubscribeFromCurrentCampfire();

			_currentCampfire = campfire;

			if (_currentCampfire != null)
			{
				_currentCampfire.Changed += Refresh;
			}

			Refresh();

			_root.AddToClassList(VisibleClassName);
			AcquireInputMode();
		}

		public void Hide()
		{
			UnsubscribeFromCurrentCampfire();

			_root?.RemoveFromClassList(VisibleClassName);
			ReleaseInputMode();
		}

		private void Refresh()
		{
			if (_currentCampfire == null)
			{
				return;
			}

			_stateLabel.text = Loc.CampfireStateLabel(_currentCampfire.State);
			_fuelLabel.text = Loc.CampfireFuelLabel(
				_currentCampfire.RemainingBurnMinutes,
				_currentCampfire.MaxBurnMinutes
			);

			_igniteButton.SetEnabled(!_currentCampfire.IsBurning);
			_extinguishButton.SetEnabled(_currentCampfire.IsBurning);
			_meltSnowButton.SetEnabled(_currentCampfire.IsBurning);
		}

		private void OnAddFuelClicked()
		{
			if (_currentCampfire == null)
			{
				return;
			}

			if (_inventoryService == null)
			{
				_notificationService?.Show(Loc.BedrollInventoryMissing);
				return;
			}

			if (!TryFindFuelItem(
				    _inventoryService,
				    out int itemIndex,
				    out FuelItemDefinition fuel))
			{
				_notificationService?.Show(Loc.NoFuelInInventory);
				return;
			}

			if (!_currentCampfire.CanAddFuel(
				    fuel,
				    1,
				    out string fuelFailureReason))
			{
				_notificationService?.Show(fuelFailureReason);
				return;
			}

			if (!_inventoryService.TryRemoveItemAt(
				    itemIndex,
				    1,
				    out string removeFailureReason))
			{
				_notificationService?.Show(removeFailureReason);
				return;
			}

			if (!_currentCampfire.AddFuel(
				    fuel,
				    1,
				    out string addFuelFailureReason))
			{
				_notificationService?.Show(addFuelFailureReason);
				return;
			}

			_notificationService?.Show(Loc.FuelAdded(fuel.DisplayName));
			_ = _gameSaveService.SaveCheckpoint(SaveTrigger.CampfireIgnited);

			Refresh();
		}

		private void OnMeltSnowClicked()
		{
			if (_currentCampfire == null)
			{
				return;
			}

			if (!_currentCampfire.IsBurning)
			{
				_notificationService?.Show(Loc.SnowNotBurning);
				return;
			}

			if (_inventoryService == null)
			{
				_notificationService?.Show(Loc.BedrollInventoryMissing);
				return;
			}

			if (_itemCatalog == null ||
			    !_itemCatalog.TryGetItem(WaterItemId, out ItemDefinition water) ||
			    water == null)
			{
				return;
			}

			InventoryAddResult result = _inventoryService.AddItem(water, 1);

			if (!result.IsSuccess)
			{
				_notificationService?.Show(result.FailureReason);
				return;
			}

			_notificationService?.Show(Loc.SnowMelted);
			Refresh();
		}

		private void OnIgniteClicked()
		{
			if (_currentCampfire == null)
			{
				return;
			}

			if (!_currentCampfire.Ignite(out string failureReason))
			{
				_notificationService?.Show(failureReason);
				return;
			}

			_notificationService?.Show(Loc.FireStarted);
			Refresh();
		}

		private void OnExtinguishClicked()
		{
			if (_currentCampfire == null)
			{
				return;
			}

			if (!_currentCampfire.Extinguish(out string failureReason))
			{
				_notificationService?.Show(failureReason);
				return;
			}

			_notificationService?.Show(Loc.FireExtinguished);
			Refresh();
		}

		private static bool TryFindFuelItem(
			IInventoryService inventoryService,
			out int itemIndex,
			out FuelItemDefinition fuel
		)
		{
			itemIndex = -1;
			fuel = null;

			if (inventoryService == null)
			{
				return false;
			}

			System.Collections.Generic.IReadOnlyList<ItemStack> items = inventoryService.Items;

			for (int i = 0; i < items.Count; i++)
			{
				ItemStack stack = items[i];

				if (stack.Definition is not FuelItemDefinition fuelItem)
				{
					continue;
				}

				itemIndex = i;
				fuel = fuelItem;

				return true;
			}

			return false;
		}

		private void UnsubscribeFromCurrentCampfire()
		{
			if (_currentCampfire != null)
			{
				_currentCampfire.Changed -= Refresh;
			}

			_currentCampfire = null;
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
