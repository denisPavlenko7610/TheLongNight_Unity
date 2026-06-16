using TLN.Application.Input;
using TLN.Application.Notifications;
using TLN.Gameplay.Inventory;
using TLN.Gameplay.Items;
using TLN.Gameplay.Sleep;
using TLN.UI.Common;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;

namespace TLN.UI.Sleep
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class SleepWindowView : MonoBehaviour, ISleepWindow
    {
        private const string VisibleClassName = "sleep-window-root-visible";

        private UIDocument _document;

        private VisualElement _root;

        private Button _sleep1Button;
        private Button _sleep2Button;
        private Button _sleep4Button;
        private Button _sleep8Button;
        private Button _cancelButton;

        private Button _pickUpButton;

        private IInventoryService _inventoryService;
        private INotificationService _notificationService;
        private BedrollActor _currentBedroll;

        private SleepService _sleepService;
        private IInputModeService _inputModeService;

        private bool _isVisible;

        [Inject]
        public void Construct(SleepService sleepService, IInputModeService inputModeService, IInventoryService inventoryService,
            INotificationService notificationService)
        {
            _sleepService = sleepService;
            _inputModeService = inputModeService;
            _inventoryService = inventoryService;
            _notificationService = notificationService;
        }

        private void Awake()
        {
            _document = GetComponent<UIDocument>();
            VisualElement documentRoot = _document.rootVisualElement;

            _root = documentRoot.RequiredQ<VisualElement>("sleep-window-root");
            _root.SetVisible(false);

            _pickUpButton = documentRoot.RequiredQ<Button>("sleep-pick-up-button");
            _pickUpButton.clicked += OnPickUpClicked;

            _sleep1Button = documentRoot.RequiredQ<Button>("sleep-1h-button");
            _sleep2Button = documentRoot.RequiredQ<Button>("sleep-2h-button");
            _sleep4Button = documentRoot.RequiredQ<Button>("sleep-4h-button");
            _sleep8Button = documentRoot.RequiredQ<Button>("sleep-8h-button");
            _cancelButton = documentRoot.RequiredQ<Button>("sleep-cancel-button");

            _sleep1Button.clicked += OnSleep1Clicked;
            _sleep2Button.clicked += OnSleep2Clicked;
            _sleep4Button.clicked += OnSleep4Clicked;
            _sleep8Button.clicked += OnSleep8Clicked;
            _cancelButton.clicked += Hide;
        }

        private void OnDestroy()
        {
            _sleep1Button.clicked -= OnSleep1Clicked;
            _sleep2Button.clicked -= OnSleep2Clicked;
            _sleep4Button.clicked -= OnSleep4Clicked;
            _sleep8Button.clicked -= OnSleep8Clicked;
            _cancelButton.clicked -= Hide;

            _pickUpButton.clicked -= OnPickUpClicked;
        }

        public void Show(BedrollActor bedrollActor)
        {
            _currentBedroll = bedrollActor;

            _isVisible = true;
            _root.SetVisible(true);
            _root.AddToClassList(VisibleClassName);

            RefreshPickUpButton();

            _inputModeService?.SetUIMode();
        }

        public void Hide()
        {
            _isVisible = false;
            _currentBedroll = null;

            if (_root != null)
            {
                _root.SetVisible(false);
                _root.RemoveFromClassList(VisibleClassName);
            }

            _inputModeService?.SetGameplayMode();
        }

        private void RefreshPickUpButton()
        {
            if (_pickUpButton == null)
            {
                return;
            }

            bool canPickUp = _currentBedroll != null && _currentBedroll.CanPickUp;

            _pickUpButton.SetVisible(canPickUp);
        }

        private void OnPickUpClicked()
        {
            if (_currentBedroll == null)
            {
                return;
            }

            if (!_currentBedroll.CanPickUp)
            {
                return;
            }

            ItemDefinition packedItemDefinition = _currentBedroll.PackedItemDefinition;

            if (packedItemDefinition == null)
            {
                _notificationService?.Show("Cannot pick up this bedroll.");
                return;
            }

            InventoryAddResult result = _inventoryService.AddItem(packedItemDefinition, _currentBedroll.PackedAmount);

            if (!result.IsSuccess)
            {
                _notificationService?.Show(result.FailureReason);
                return;
            }

            _notificationService?.Show($"Picked up {packedItemDefinition.DisplayName}");

            GameObject bedrollObject = _currentBedroll.gameObject;

            Hide();

            Destroy(bedrollObject);
        }

        private void SleepAndClose(int hours)
        {
            if (_sleepService == null)
            {
                return;
            }

            _sleepService.Sleep(hours);
            Hide();
        }

        private void OnSleep1Clicked()
        {
            SleepAndClose(1);
        }

        private void OnSleep2Clicked()
        {
            SleepAndClose(2);
        }

        private void OnSleep4Clicked()
        {
            SleepAndClose(4);
        }

        private void OnSleep8Clicked()
        {
            SleepAndClose(8);
        }
    }
}
