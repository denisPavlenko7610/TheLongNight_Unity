using TLN.Application.Input;
using TLN.Application.Notifications;
using TLN.Gameplay.Campfire;
using TLN.Gameplay.Inventory;
using TLN.Gameplay.Items;
using TLN.UI.Common;
using UnityEngine;
using UnityEngine.UIElements;

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
        private IInventoryService _inventoryService;
        private IInputModeService _inputModeService;
        private INotificationService _notificationService;

        public void Construct(
            IInventoryService inventoryService,
            IInputModeService inputModeService,
            INotificationService notificationService)
        {
            _inventoryService = inventoryService;
            _inputModeService = inputModeService;
            _notificationService = notificationService;

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
            _inputModeService?.SetUIMode();
        }

        public void Hide()
        {
            UnsubscribeFromCurrentCampfire();

            if (_root != null)
            {
                _root.RemoveFromClassList(VisibleClassName);
            }

            _inputModeService?.SetGameplayMode();
        }

        private void Refresh()
        {
            if (_currentCampfire == null)
            {
                return;
            }

            _stateLabel.text = $"State: {_currentCampfire.State}";
            _fuelLabel.text =
                $"Fuel: {_currentCampfire.RemainingBurnMinutes} / {_currentCampfire.MaxBurnMinutes} min";

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
                _notificationService?.Show("You do not have any fuel.");
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
                out string removeFailureReason);

            if (!wasRemoved)
            {
                _notificationService?.Show(removeFailureReason);
                return;
            }

            bool wasAdded = _currentCampfire.AddFuel(
                fuel,
                1,
                out string addFailureReason);

            if (!wasAdded)
            {
                _inventoryService.AddItem(fuel, 1);
                _notificationService?.Show(addFailureReason);
                return;
            }

            _notificationService?.Show($"Added {fuel.DisplayName}.");
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

            _notificationService?.Show("Fire started.");
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

            _notificationService?.Show("Fire extinguished.");
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
