using Newtonsoft.Json;
using TLN.Application.Localization;
using TLN.Application.Notifications;
using TLN.Core.Logging;
using TLN.Core.Validation;
using TLN.Gameplay.Interaction;
using TLN.Gameplay.Inventory;
using TLN.Gameplay.Saves;
using UnityEngine;
using VContainer;

namespace TLN.Gameplay.Items
{
	[RequireComponent(typeof(PersistentWorldEntity))]
	public sealed class WorldItemActor : MonoBehaviour, IInteractable, IWorldSaveable
	{
		private const string SaveType = "world_item";

		[Header("Item")] [SerializeField] [Required]
		private ItemDefinition _definition;

		[SerializeField]
		private int _amount = 1;

		private INotificationService _notificationService;
		private IInventoryService _inventoryService;
		private ItemCatalog _itemCatalog;
		private WorldSaveRegistry _worldSaveRegistry;
		private PersistentWorldEntity _persistentEntity;

		public string SaveTypeId => SaveType;

		public string InteractionText => _definition == null
			? LocalizationKeys.InteractionPickup ?? "Pick up item"
			: LocalizationKeys.InteractionPickupFormat(_definition.DisplayName) ?? $"Pick up {_definition.DisplayName}";

		[Inject]
		public void Construct(
			IInventoryService inventoryService,
			INotificationService notificationService,
			ItemCatalog itemCatalog,
			WorldSaveRegistry worldSaveRegistry
		)
		{
			_inventoryService = inventoryService;
			_notificationService = notificationService;
			_itemCatalog = itemCatalog;
			_worldSaveRegistry = worldSaveRegistry;
		}

		private void Awake()
		{
			_persistentEntity = GetComponent<PersistentWorldEntity>();
		}

		public bool CanInteract(InteractionContext context)
		{
			if (_definition == null)
			{
				return false;
			}

			if (_amount <= 0)
			{
				return false;
			}

			if (_inventoryService == null)
			{
				return false;
			}

			return _inventoryService.CanAddItem(_definition, _amount, out _);
		}

		public void Interact(InteractionContext context)
		{
			if (!CanInteract(context))
			{
				return;
			}

			InventoryAddResult result = _inventoryService.AddItem(_definition, _amount);

			if (!result.IsSuccess)
			{
				_notificationService?.Show(result.FailureReason);

				TLNLogger.LogWarning($"Cannot pick up {_definition.DisplayName}. Reason: {result.FailureReason}", this);

				return;
			}

			_notificationService?.Show(LocalizationKeys.ItemsPickedUp(_definition.DisplayName));

			if (_persistentEntity != null && _persistentEntity.IsSceneObject)
			{
				_worldSaveRegistry?.MarkSceneEntityDestroyed(_persistentEntity.Id);
			}

			Destroy(gameObject);
		}

		public string CaptureStateJson()
		{
			WorldItemSaveData data = new WorldItemSaveData
			{
				itemId = _definition == null
					? string.Empty
					: _definition.Id,

				amount = _amount
			};

			return JsonConvert.SerializeObject(data);
		}

		public void RestoreStateJson(string json)
		{
			if (string.IsNullOrWhiteSpace(json))
			{
				return;
			}

			WorldItemSaveData data = JsonConvert.DeserializeObject<WorldItemSaveData>(json);
			if (data == null)
			{
				return;
			}

			_amount = Mathf.Max(1, data.amount);

			if (_itemCatalog != null && _itemCatalog.TryGetItem(data.itemId, out ItemDefinition definition))
			{
				_definition = definition;
			}
		}

		private sealed class WorldItemSaveData
		{
			public string itemId;
			public int amount;
		}
	}
}
