using System;
using System.Collections.Generic;
using TLN.Application.Input;
using TLN.Application.Localization;
using TLN.Application.Multiplayer;
using TLN.Application.Notifications;
using TLN.Application.Saves;
using TLN.Gameplay.Campfire;
using TLN.Gameplay.Inventory;
using TLN.Gameplay.Inventory.Networking;
using TLN.Gameplay.Items;
using TLN.Gameplay.Player;
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

		private VisualElement _root;
		private Label _stateLabel;
		private Label _fuelLabel;
		private Button _addFuelButton;
		private Button _igniteButton;
		private Button _extinguishButton;
		private Button _closeButton;

		private CampfireActor _currentCampfire;
		private IDisposable _inputModeScope;

		private IInventoryService _inventoryService;
		private IInputModeService _inputModeService;
		private INotificationService _notificationService;
		private IGameSaveService _gameSaveService;
		private IMultiplayerSessionService _multiplayerSessionService;
		private LocalPlayerService _localPlayerService;

		[Inject]
		public void Construct(
			IInventoryService inventoryService,
			IInputModeService inputModeService,
			INotificationService notificationService,
			IGameSaveService gameSaveService,
			IMultiplayerSessionService multiplayerSessionService,
			LocalPlayerService localPlayerService
		)
		{
			_inventoryService = inventoryService;
			_inputModeService = inputModeService;
			_notificationService = notificationService;
			_gameSaveService = gameSaveService;
			_multiplayerSessionService = multiplayerSessionService;
			_localPlayerService = localPlayerService;

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
			_igniteButton = documentRoot.RequiredQ<Button>("campfire-ignite-button");
			_extinguishButton = documentRoot.RequiredQ<Button>("campfire-extinguish-button");
			_closeButton = documentRoot.RequiredQ<Button>("campfire-close-button");

			_addFuelButton.clicked += OnAddFuelClicked;
			_igniteButton.clicked += OnIgniteClicked;
			_extinguishButton.clicked += OnExtinguishClicked;
			_closeButton.clicked += Hide;
		}

		private void OnDestroy()
		{
			_addFuelButton.clicked -= OnAddFuelClicked;
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
		}

		private void OnAddFuelClicked()
		{
			if (_currentCampfire == null)
			{
				return;
			}

			IInventoryService inventoryService = GetActiveInventoryService();

			if (inventoryService == null)
			{
				_notificationService?.Show(Loc.BedrollInventoryMissing);
				return;
			}

			if (!TryFindFuelItem(
				    inventoryService,
				    out int itemIndex,
				    out FuelItemDefinition fuel))
			{
				_notificationService?.Show(Loc.NoFuelInInventory);
				return;
			}

			if (IsMultiplayer())
			{
				AddFuelMultiplayer(inventoryService, itemIndex);
				return;
			}

			AddFuelOffline(inventoryService, itemIndex, fuel);
		}

		private void OnIgniteClicked()
		{
			if (_currentCampfire == null)
			{
				return;
			}

			if (IsMultiplayer())
			{
				RequestIgniteMultiplayer();
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

			if (IsMultiplayer())
			{
				RequestExtinguishMultiplayer();
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

		private void AddFuelOffline(
			IInventoryService inventoryService,
			int itemIndex,
			FuelItemDefinition fuel
		)
		{
			if (!_currentCampfire.CanAddFuel(
				    fuel,
				    1,
				    out string fuelFailureReason))
			{
				_notificationService?.Show(fuelFailureReason);
				return;
			}

			if (!inventoryService.TryRemoveItemAt(
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

		private void AddFuelMultiplayer(
			IInventoryService inventoryService,
			int itemIndex
		)
		{
			if (!TryGetNetworkInventory(inventoryService, out NetworkPlayerInventory networkInventory))
			{
				return;
			}

			networkInventory.RequestAddFuelToCampfire(
				_currentCampfire,
				itemIndex
			);
		}

		private void RequestIgniteMultiplayer()
		{
			if (!TryGetNetworkInventory(GetActiveInventoryService(), out NetworkPlayerInventory networkInventory))
			{
				return;
			}

			networkInventory.RequestIgniteCampfire(_currentCampfire);
		}

		private void RequestExtinguishMultiplayer()
		{
			if (!TryGetNetworkInventory(GetActiveInventoryService(), out NetworkPlayerInventory networkInventory))
			{
				return;
			}

			networkInventory.RequestExtinguishCampfire(_currentCampfire);
		}

		private bool TryGetNetworkInventory(
			IInventoryService inventoryService,
			out NetworkPlayerInventory networkInventory
		)
		{
			if (inventoryService is NetworkPlayerInventory activeNetworkInventory)
			{
				networkInventory = activeNetworkInventory;
				return true;
			}

			networkInventory = null;
			_notificationService?.Show(Loc.CannotUse);
			return false;
		}

		private IInventoryService GetActiveInventoryService()
		{
			if (IsMultiplayer())
			{
				return _localPlayerService?.InventoryService;
			}

			return _inventoryService;
		}

		private bool IsMultiplayer()
		{
			return _multiplayerSessionService is { IsMultiplayer: true };
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

			IReadOnlyList<ItemStack> items = inventoryService.Items;

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
