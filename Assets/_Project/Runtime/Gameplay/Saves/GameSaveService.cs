using System;
using System.Collections.Generic;
using TLN.Application.Localization;
using TLN.Application.Notifications;
using TLN.Application.Saves;
using TLN.Core.Logging;
using TLN.Gameplay.Inventory;
using TLN.Gameplay.Items;
using TLN.Gameplay.Placement;
using TLN.Gameplay.Survival;
using TLN.Gameplay.Time;
using UnityEngine;

namespace TLN.Gameplay.Saves
{
	public sealed class GameSaveService : IGameSaveService
	{
		private const string WorldSceneName = "World";

		private readonly ISaveRepository _saveRepository;
		private readonly SaveSessionService _saveSessionService;
		private readonly IInventoryService _inventoryService;
		private readonly ISurvivalService _survivalService;
		private readonly IGameTimeService _gameTimeService;
		private readonly PlacementService _placementService;
		private readonly ItemCatalog _itemCatalog;
		private readonly INotificationService _notificationService;
		private readonly ILocalizationService _localizationService;
		private readonly WorldSaveRegistry _worldSaveRegistry;

		public bool CanSaveManually => false;

		public GameSaveService(
			ISaveRepository saveRepository,
			SaveSessionService saveSessionService,
			IInventoryService inventoryService,
			ISurvivalService survivalService,
			IGameTimeService gameTimeService,
			PlacementService placementService,
			ItemCatalog itemCatalog,
			INotificationService notificationService,
			ILocalizationService localizationService,
			WorldSaveRegistry worldSaveRegistry
		)
		{
			_saveRepository = saveRepository;
			_saveSessionService = saveSessionService;
			_inventoryService = inventoryService;
			_survivalService = survivalService;
			_gameTimeService = gameTimeService;
			_placementService = placementService;
			_itemCatalog = itemCatalog;
			_notificationService = notificationService;
			_localizationService = localizationService;
			_worldSaveRegistry = worldSaveRegistry;
		}

		public async Awaitable<bool> SaveManual()
		{
			if (!CanSaveManually)
			{
				_notificationService?.Show(_localizationService.Get(LocalizationTableNames.UI, LocalizationKeys.Saves.ManualUnavailable));

				return false;
			}

			return await SaveCheckpoint(SaveTrigger.Manual);
		}

		public async Awaitable<bool> SaveCheckpoint(SaveTrigger trigger)
		{
			GameSaveData data = CreateSaveData(trigger);
			_saveRepository.PrepareForBackgroundAccess();

			try
			{
				await Awaitable.BackgroundThreadAsync();
				_saveRepository.Save(data);
				await Awaitable.MainThreadAsync();
			}
			catch (Exception exception)
			{
				await Awaitable.MainThreadAsync();
				TLNLogger.LogError($"Failed to save checkpoint. {exception}");
				_notificationService?.Show(_localizationService.Get(LocalizationTableNames.UI, LocalizationKeys.Saves.Failed));
				return false;
			}

			_notificationService?.Show(_localizationService.Get(LocalizationTableNames.UI, LocalizationKeys.Saves.Saved));

			return true;
		}

		public bool LoadActiveSlotIfRequested()
		{
			if (!_saveSessionService.ShouldLoadActiveSlot)
			{
				return false;
			}

			int slotId = _saveSessionService.ActiveSlotId;
			GameSaveData data = _saveRepository.Load(slotId);

			_saveSessionService.ConsumeLoadRequest();

			if (data == null)
			{
				_notificationService?.Show(_localizationService.Get(LocalizationTableNames.UI, LocalizationKeys.Saves.SlotEmpty, slotId));

				return false;
			}

			ApplySaveData(data);

			_notificationService?.Show(_localizationService.Get(LocalizationTableNames.UI, LocalizationKeys.Saves.Loaded));

			return true;
		}

		private GameSaveData CreateSaveData(SaveTrigger trigger)
		{
			GameSaveData data = new GameSaveData
			{
				version = 1,
				slotId = _saveSessionService.ActiveSlotId,
				savedAtUtc = DateTime.UtcNow.ToString("O"),
				sceneName = WorldSceneName,
				saveReason = trigger.ToString()
			};

			CapturePlayer(data.player);
			CaptureTime(data.time);
			CaptureSurvival(data.survival);
			CaptureInventory(data.inventory);
			CaptureWorld(data.world);

			return data;
		}

		private void CaptureWorld(WorldSaveData data)
		{
			_worldSaveRegistry?.Capture(data);
		}

		private void CapturePlayer(PlayerSaveData data)
		{
			if (data == null)
			{
				return;
			}

			Transform playerTransform = _placementService.PlayerRoot == null
					? null
					: _placementService.PlayerRoot.transform;

			if (playerTransform == null)
			{
				return;
			}

			data.position = Vector3SaveData.FromVector3(playerTransform.position);
			data.rotation = QuaternionSaveData.FromQuaternion(playerTransform.rotation);
		}

		private void CaptureTime(GameTimeSaveData data)
		{
			if (data == null || _gameTimeService == null)
			{
				return;
			}

			data.totalMinutes = _gameTimeService.TotalMinutes;
		}

		private void CaptureSurvival(SurvivalSaveData data)
		{
			if (data == null || _survivalService == null)
			{
				return;
			}

			data.hunger = _survivalService.Hunger.Value;
			data.thirst = _survivalService.Thirst.Value;
			data.fatigue = _survivalService.Fatigue.Value;
			data.cold = _survivalService.Cold.Value;
			data.condition = _survivalService.Condition.Value;
		}

		private void CaptureInventory(InventorySaveData data)
		{
			if (data == null || _inventoryService == null)
			{
				return;
			}

			data.items.Clear();

			IReadOnlyList<ItemStack> items = _inventoryService.Items;

			for (int i = 0; i < items.Count; i++)
			{
				ItemStack stack = items[i];

				if (stack.Definition == null)
				{
					continue;
				}

				if (string.IsNullOrWhiteSpace(stack.Definition.Id))
				{
					continue;
				}

				if (stack.Amount <= 0)
				{
					continue;
				}

				data.items.Add(new InventoryItemSaveData
					{
						itemId = stack.Definition.Id,
						amount = stack.Amount
					}
				);
			}
		}

		private void ApplySaveData(GameSaveData data)
		{
			if (data == null)
			{
				return;
			}

			ApplyPlayer(data.player);
			ApplyTime(data.time);
			ApplySurvival(data.survival);
			ApplyInventory(data.inventory);
			ApplyWorld(data.world);
		}

		private void ApplyWorld(WorldSaveData data)
		{
			_worldSaveRegistry?.Restore(data);
		}

		private void ApplyPlayer(PlayerSaveData data)
		{
			if (data == null)
			{
				return;
			}

			if (_placementService.PlayerRoot == null)
			{
				return;
			}

			GameObject playerObject =
				_placementService.PlayerRoot.gameObject;

			CharacterController characterController = playerObject.GetComponent<CharacterController>();

			bool hadController = characterController != null && characterController.enabled;

			if (hadController)
			{
				characterController.enabled = false;
			}

			playerObject.transform.SetPositionAndRotation(data.position.ToVector3(), data.rotation.ToQuaternion());
			if (hadController)
			{
				characterController.enabled = true;
			}
		}

		private void ApplyTime(GameTimeSaveData data)
		{
			if (data == null || _gameTimeService == null)
			{
				return;
			}

			_gameTimeService.SetTotalMinutes(data.totalMinutes);
		}

		private void ApplySurvival(SurvivalSaveData data)
		{
			if (data == null || _survivalService == null)
			{
				return;
			}

			_survivalService.SetValues(data.hunger, data.thirst, data.fatigue, data.cold, data.condition);
		}

		private void ApplyInventory(InventorySaveData data)
		{
			List<ItemStack> restoredItems = new List<ItemStack>();

			if (data != null && data.items != null)
			{
				for (int i = 0; i < data.items.Count; i++)
				{
					InventoryItemSaveData savedItem = data.items[i];

					if (savedItem == null)
					{
						continue;
					}

					if (savedItem.amount <= 0)
					{
						continue;
					}

					if (!_itemCatalog.TryGetItem(savedItem.itemId, out ItemDefinition definition))
					{
						TLNLogger.LogWarning($"Cannot restore item. Unknown id: {savedItem.itemId}");

						continue;
					}

					restoredItems.Add(new ItemStack(definition, savedItem.amount));
				}
			}

			_inventoryService.ReplaceItems(restoredItems);
		}
	}
}
