using System;
using System.Globalization;
using TLN.Application.Localization;
using TLN.Application.Saves;
using TLN.UI.Common;
using UnityEngine.UIElements;

namespace TLN.UI.Saves
{
	public sealed class SaveSlotsPanel : IDisposable
	{
		private const string EmptySlotClassName = "save-slot-button-empty";
		private const string OccupiedSlotClassName = "save-slot-button-occupied";
		private const int MinutesPerHour = 60;
		private const int HoursPerDay = 24;
		private const int MinutesPerDay = MinutesPerHour * HoursPerDay;

		private readonly VisualElement _panelRoot;
		private readonly Label _titleLabel;
		private readonly Label _descriptionLabel;

		private readonly Button _slot1Button;
		private readonly Button _slot2Button;
		private readonly Button _slot3Button;
		private readonly Button _backButton;

		private readonly VisualElement _overwriteRoot;
		private readonly Label _overwriteLabel;
		private readonly Button _overwriteConfirmButton;
		private readonly Button _overwriteCancelButton;

		private readonly ISaveRepository _saveRepository;
		private readonly Action<int> _newGameSelected;
		private readonly Action<int> _loadGameSelected;
		private readonly Action _backClicked;

		private SaveSlotsPanelMode _mode;
		private int _pendingOverwriteSlotId;

		public SaveSlotsPanel(
			VisualElement root,
			ISaveRepository saveRepository,
			Action<int> newGameSelected,
			Action<int> loadGameSelected,
			Action backClicked
		)
		{
			_saveRepository = saveRepository;
			_newGameSelected = newGameSelected;
			_loadGameSelected = loadGameSelected;
			_backClicked = backClicked;

			_panelRoot = root.RequiredQ<VisualElement>("save-slots-panel");
			_titleLabel = root.RequiredQ<Label>("save-slots-title-label");
			_descriptionLabel = root.RequiredQ<Label>("save-slots-description-label");

			_slot1Button = root.RequiredQ<Button>("save-slot-1-button");
			_slot2Button = root.RequiredQ<Button>("save-slot-2-button");
			_slot3Button = root.RequiredQ<Button>("save-slot-3-button");
			_backButton = root.RequiredQ<Button>("save-slots-back-button");

			_overwriteRoot = root.RequiredQ<VisualElement>("save-overwrite-panel");
			_overwriteLabel = root.RequiredQ<Label>("save-overwrite-label");
			_overwriteConfirmButton = root.RequiredQ<Button>("save-overwrite-confirm-button");
			_overwriteCancelButton = root.RequiredQ<Button>("save-overwrite-cancel-button");

			_slot1Button.clicked += OnSlot1Clicked;
			_slot2Button.clicked += OnSlot2Clicked;
			_slot3Button.clicked += OnSlot3Clicked;
			_backButton.clicked += OnBackClicked;

			_overwriteConfirmButton.clicked += OnOverwriteConfirmed;
			_overwriteCancelButton.clicked += HideOverwriteConfirm;

			Hide();
		}

		public void Dispose()
		{
			_slot1Button.clicked -= OnSlot1Clicked;
			_slot2Button.clicked -= OnSlot2Clicked;
			_slot3Button.clicked -= OnSlot3Clicked;
			_backButton.clicked -= OnBackClicked;

			_overwriteConfirmButton.clicked -= OnOverwriteConfirmed;
			_overwriteCancelButton.clicked -= HideOverwriteConfirm;
		}

		public void ShowNewGame()
		{
			_mode = SaveSlotsPanelMode.NewGame;

			_titleLabel.text = Loc.NewGame;
			_descriptionLabel.text = Loc.NewGameDescription;

			Refresh();
			_panelRoot.SetVisible(true);
			HideOverwriteConfirm();
		}

		public void ShowLoadGame()
		{
			_mode = SaveSlotsPanelMode.LoadGame;

			_titleLabel.text = Loc.LoadGame;
			_descriptionLabel.text = Loc.LoadGameDescription;

			Refresh();
			_panelRoot.SetVisible(true);
			HideOverwriteConfirm();
		}

		public void Hide()
		{
			_panelRoot.SetVisible(false);
			HideOverwriteConfirm();
		}

		private void Refresh()
		{
			RefreshSlotButton(_slot1Button, 1);
			RefreshSlotButton(_slot2Button, 2);
			RefreshSlotButton(_slot3Button, 3);
		}

		private void RefreshSlotButton(Button button, int slotId)
		{
			GameSaveData saveData = _saveRepository?.Load(slotId);

			bool hasSave = saveData != null;

			button.text = hasSave
				? CreateOccupiedSlotText(slotId, saveData)
				: CreateEmptySlotText(slotId);

			button.EnableInClassList(EmptySlotClassName, !hasSave);
			button.EnableInClassList(OccupiedSlotClassName, hasSave);

			bool isEnabled = _mode == SaveSlotsPanelMode.NewGame || hasSave;

			button.SetEnabled(isEnabled);
		}

		private string CreateEmptySlotText(int slotId)
		{
			return Loc.SlotFormat(slotId, Loc.Empty);
		}

		private string CreateOccupiedSlotText(int slotId, GameSaveData saveData)
		{
			string gameTime = CreateGameTimeText(saveData.time?.totalMinutes ?? 0);

			string savedAt = CreateSavedAtText(saveData.savedAtUtc);

			string reason = string.IsNullOrWhiteSpace(saveData.saveReason)
				? Loc.DefaultReason
				: saveData.saveReason;

			string details = string.IsNullOrEmpty(savedAt)
				? Loc.DetailsFormat(gameTime, reason)
				: Loc.DetailsFormatWithDate(gameTime, reason, savedAt);

			return Loc.SlotFormat(slotId, details);
		}

		private string CreateGameTimeText(int totalMinutes)
		{
			int safeTotalMinutes = Math.Max(0, totalMinutes);

			int day = safeTotalMinutes / MinutesPerDay + 1;
			int minutesInDay = safeTotalMinutes % MinutesPerDay;
			int hour = minutesInDay / MinutesPerHour;
			int minute = minutesInDay % MinutesPerHour;

			return Loc.DayTimeFormat(day, hour.ToString("00"), minute.ToString("00"));
		}

		private static string CreateSavedAtText(string savedAtUtc)
		{
			if (string.IsNullOrWhiteSpace(savedAtUtc))
			{
				return string.Empty;
			}

			if (!DateTime.TryParse(
					savedAtUtc,
					CultureInfo.InvariantCulture,
					DateTimeStyles.RoundtripKind,
					out DateTime utcTime
				))
			{
				return savedAtUtc;
			}

			DateTime localTime = utcTime.ToLocalTime();
			return localTime.ToString("yyyy-MM-dd HH:mm");
		}

		private void OnSlot1Clicked()
		{
			OnSlotClicked(1);
		}

		private void OnSlot2Clicked()
		{
			OnSlotClicked(2);
		}

		private void OnSlot3Clicked()
		{
			OnSlotClicked(3);
		}

		private void OnSlotClicked(int slotId)
		{
			switch (_mode)
			{
				case SaveSlotsPanelMode.NewGame:
					HandleNewGameSlot(slotId);
					break;

				case SaveSlotsPanelMode.LoadGame:
					HandleLoadGameSlot(slotId);
					break;
			}
		}

		private void HandleNewGameSlot(int slotId)
		{
			bool hasSave = _saveRepository != null && _saveRepository.SaveExists(slotId);

			if (hasSave)
			{
				ShowOverwriteConfirm(slotId);
				return;
			}

			_newGameSelected?.Invoke(slotId);
		}

		private void HandleLoadGameSlot(int slotId)
		{
			bool hasSave = _saveRepository != null && _saveRepository.SaveExists(slotId);

			if (!hasSave)
			{
				Refresh();
				return;
			}

			_loadGameSelected?.Invoke(slotId);
		}

		private void ShowOverwriteConfirm(int slotId)
		{
			_pendingOverwriteSlotId = slotId;

			_overwriteLabel.text = Loc.OverwriteLabel(slotId);

			_overwriteRoot.SetVisible(true);
		}

		private void HideOverwriteConfirm()
		{
			_pendingOverwriteSlotId = 0;
			_overwriteRoot.SetVisible(false);
		}

		private void OnOverwriteConfirmed()
		{
			if (_pendingOverwriteSlotId <= 0)
			{
				HideOverwriteConfirm();
				return;
			}

			int slotId = _pendingOverwriteSlotId;
			HideOverwriteConfirm();

			_newGameSelected?.Invoke(slotId);
		}

		private void OnBackClicked()
		{
			Hide();
			_backClicked?.Invoke();
		}
	}
}
