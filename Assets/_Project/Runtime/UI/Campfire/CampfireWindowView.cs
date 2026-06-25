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

		private VisualElement _root;
		private Label _stateLabel;
		private Label _fuelLabel;
		private VisualElement _fuelMeterFill;
		private Button _addFuelButton;
		private Button _igniteButton;
		private Button _extinguishButton;
		private Button _closeButton;

		private CampfireActor _currentCampfire;
		private IInventoryService _inventoryService;
		private ILocalizationService _localizationService;
		private IInputModeService _inputModeService;
		private INotificationService _notificationService;
		private IGameSaveService _gameSaveService;

		[Inject]
		public void Construct(
			IInventoryService inventoryService,
			ILocalizationService localizationService,
			IInputModeService inputModeService,
			INotificationService notificationService,
			IGameSaveService gameSaveService
		)
		{
			_inventoryService = inventoryService;
			_localizationService = localizationService;
			_inputModeService = inputModeService;
			_notificationService = notificationService;
			_gameSaveService = gameSaveService;

			Hide();
		}

		private void Awake()
		{
			UIDocument document = GetComponent<UIDocument>();
			VisualElement documentRoot = document.rootVisualElement;

			_root = documentRoot.RequiredQ<VisualElement>("campfire-window-root");
			_stateLabel = documentRoot.RequiredQ<Label>("campfire-state-label");
			_fuelLabel = documentRoot.RequiredQ<Label>("campfire-fuel-label");
			_fuelMeterFill = documentRoot.RequiredQ<VisualElement>("campfire-fuel-meter-fill");
			_addFuelButton = documentRoot.RequiredQ<Button>("campfire-add-fuel-button");
			_igniteButton = documentRoot.RequiredQ<Button>("campfire-ignite-button");
			_extinguishButton = documentRoot.RequiredQ<Button>("campfire-extinguish-button");
			_closeButton = documentRoot.RequiredQ<Button>("campfire-close-button");

			_root.RemoveFromClassList(VisibleClassName);
			_root.SetVisible(false);

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
			_root.SetVisible(true);
			_root.AddToClassList(VisibleClassName);
			_inputModeService?.SetUIMode();
		}

		public void Hide()
		{
			UnsubscribeFromCurrentCampfire();

			_root?.RemoveFromClassList(VisibleClassName);
			_root?.SetVisible(false);

			_inputModeService?.SetGameplayMode();
		}

		private void Refresh()
		{
			if (_currentCampfire == null)
			{
				return;
			}

			_stateLabel.text = _localizationService.Get(LocalizationKeys.Campfire.StateLabel, _currentCampfire.State);
			_fuelLabel.text = _localizationService.Get(LocalizationKeys.Campfire.FuelLabel, _currentCampfire.RemainingBurnMinutes, _currentCampfire.MaxBurnMinutes);
			float fuelPercent = _currentCampfire.MaxBurnMinutes > 0
				? Mathf.Clamp01((float)_currentCampfire.RemainingBurnMinutes / _currentCampfire.MaxBurnMinutes) * 100f
				: 0f;
			_fuelMeterFill.style.width = Length.Percent(fuelPercent);

			_igniteButton.SetEnabled(!_currentCampfire.IsBurning);
			_extinguishButton.SetEnabled(_currentCampfire.IsBurning);
		}

		private void OnAddFuelClicked()
		{
			if (_currentCampfire == null || _inventoryService == null)
			{
				return;
			}

			if (!TryFindFuelItem(out int itemIndex, out FuelItemDefinition fuel))
			{
				_notificationService?.Show(_localizationService.Get(LocalizationKeys.Campfire.NoFuelInInventory));
				return;
			}

			if (!_currentCampfire.CanAddFuel(fuel, 1, out string campfireFailureReason))
			{
				_notificationService?.Show(campfireFailureReason);
				return;
			}

			bool wasRemoved = _inventoryService.TryRemoveItemAt(
				itemIndex,
				1,
				out string removeFailureReason
			);

			if (!wasRemoved)
			{
				_notificationService?.Show(removeFailureReason);
				return;
			}

			bool wasAdded = _currentCampfire.AddFuel(
				fuel,
				1,
				out string addFailureReason
			);

			if (!wasAdded)
			{
				_inventoryService.AddItem(fuel, 1);
				_notificationService?.Show(addFailureReason);
				return;
			}

			_notificationService?.Show(_localizationService.Get(LocalizationKeys.Campfire.FuelAdded, fuel.DisplayName));
			Refresh();
		}

		private void OnIgniteClicked()
		{
			if (_currentCampfire == null)
			{
				return;
			}

			bool wasIgnited = _currentCampfire.Ignite(out string failureReason);

			if (!wasIgnited)
			{
				_notificationService?.Show(failureReason);
				return;
			}

			_notificationService?.Show(_localizationService.Get(LocalizationKeys.Campfire.FireStarted));

			if (_gameSaveService != null)
			{
				_ = _gameSaveService.SaveCheckpoint(SaveTrigger.CampfireIgnited);
			}

			Refresh();
		}

		private void OnExtinguishClicked()
		{
			if (_currentCampfire == null)
			{
				return;
			}

			bool wasExtinguished = _currentCampfire.Extinguish(out string failureReason);

			if (!wasExtinguished)
			{
				_notificationService?.Show(failureReason);
				return;
			}

			_notificationService?.Show(_localizationService.Get(LocalizationKeys.Campfire.FireExtinguished));
			Refresh();
		}

		private bool TryFindFuelItem(out int itemIndex, out FuelItemDefinition fuel)
		{
			itemIndex = -1;
			fuel = null;

			if (_inventoryService == null)
			{
				return false;
			}

			for (int i = 0; i < _inventoryService.Items.Count; i++)
			{
				ItemStack stack = _inventoryService.Items[i];

				if (stack.Definition is FuelItemDefinition fuelDefinition)
				{
					itemIndex = i;
					fuel = fuelDefinition;
					return true;
				}
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
	}
}
