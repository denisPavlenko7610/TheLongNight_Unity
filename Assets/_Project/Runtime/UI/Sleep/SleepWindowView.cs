using System;
using TLN.Application.Input;
using TLN.Application.Localization;
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

		private VisualElement _root;

		private Label _titleLabel;
		private Label _descriptionLabel;
		private Button _sleep1Button;
		private Button _sleep2Button;
		private Button _sleep4Button;
		private Button _sleep8Button;
		private Button _cancelButton;

		private Button _pickUpButton;

		private BedrollActor _currentBedroll;
		private IDisposable _inputModeScope;

		private SleepService _sleepService;
		private IInputModeService _inputModeService;

		[Inject]
		public void Construct(
			SleepService sleepService,
			IInputModeService inputModeService)
		{
			_sleepService = sleepService;
			_inputModeService = inputModeService;
		}

		private void Awake()
		{
			UIDocument document = GetComponent<UIDocument>();
			VisualElement documentRoot = document.rootVisualElement;

			_root = documentRoot.RequiredQ<VisualElement>("sleep-window-root");
			_root.SetVisible(false);

			_pickUpButton = documentRoot.RequiredQ<Button>("sleep-pick-up-button");
			_pickUpButton.clicked += OnPickUpClicked;

			_titleLabel = documentRoot.RequiredQ<Label>("sleep-title-label");
			_descriptionLabel = documentRoot.RequiredQ<Label>("sleep-description-label");
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

			RefreshLocalizedText();
			LocaleCodes.LocaleChanged += RefreshLocalizedText;
		}

		private void OnDestroy()
		{
			LocaleCodes.LocaleChanged -= RefreshLocalizedText;

			_sleep1Button.clicked -= OnSleep1Clicked;
			_sleep2Button.clicked -= OnSleep2Clicked;
			_sleep4Button.clicked -= OnSleep4Clicked;
			_sleep8Button.clicked -= OnSleep8Clicked;
			_cancelButton.clicked -= Hide;

			_pickUpButton.clicked -= OnPickUpClicked;
			ReleaseInputMode();
		}

		private void RefreshLocalizedText()
		{
			_titleLabel.text = Loc.SleepWindowTitle;
			_descriptionLabel.text = Loc.Description;
			_sleep1Button.text = Loc.OneHour;
			_sleep2Button.text = Loc.TwoHours;
			_sleep4Button.text = Loc.FourHours;
			_sleep8Button.text = Loc.EightHours;
			_pickUpButton.text = Loc.PickUp;
			_cancelButton.text = Loc.Cancel;
		}

		public void Show(BedrollActor bedrollActor)
		{
			_currentBedroll = bedrollActor;

			_root.SetVisible(true);
			_root.AddToClassList(VisibleClassName);

			RefreshPickUpButton();

			AcquireInputMode();
		}

		public void Hide()
		{
			_currentBedroll = null;

			if (_root != null)
			{
				_root.SetVisible(false);
				_root.RemoveFromClassList(VisibleClassName);
			}

			ReleaseInputMode();
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

			if (_currentBedroll.TryPickUp())
			{
				Hide();
			}
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

		private void OnSleep1Clicked() => SleepAndClose(1);

		private void OnSleep2Clicked() => SleepAndClose(2);

		private void OnSleep4Clicked() => SleepAndClose(4);

		private void OnSleep8Clicked() => SleepAndClose(8);

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
