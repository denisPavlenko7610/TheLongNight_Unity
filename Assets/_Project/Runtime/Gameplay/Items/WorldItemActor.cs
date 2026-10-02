using Newtonsoft.Json;
using TLN.Application.Feedback;
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

		[Header("Item")]
		[SerializeField] [Required] private ItemDefinition _definition;
		[SerializeField] private int _amount = 1;

		private INotificationService _notificationService;
		private IInventoryService _inventoryService;
		private ItemCatalog _itemCatalog;
		private WorldSaveRegistry _worldSaveRegistry;
		private PersistentWorldEntity _persistentEntity;
		private IFeedbackService _feedbackService;

		public string SaveTypeId => SaveType;

		public ItemDefinition Definition => _definition;
		public int Amount => Mathf.Max(1, _amount);

		public bool CanBePickedUp =>
			_definition != null &&
			_amount > 0;

		public string InteractionText => _definition == null
			? Loc.InteractionPickup
			: Loc.InteractionPickupFormat(_definition.DisplayName);

		[Inject]
		public void Construct(
			IInventoryService inventoryService,
			INotificationService notificationService,
			ItemCatalog itemCatalog,
			WorldSaveRegistry worldSaveRegistry,
			IFeedbackService feedbackService
		)
		{
			_inventoryService = inventoryService;
			_notificationService = notificationService;
			_itemCatalog = itemCatalog;
			_worldSaveRegistry = worldSaveRegistry;
			_feedbackService = feedbackService;
		}

		private void Awake()
		{
			_persistentEntity = GetComponent<PersistentWorldEntity>();
		}

		/// <summary>
		/// Called right after the item was dropped by the player to set exact content,
		/// since the prefab's serialized definition/amount only act as defaults.
		/// </summary>
		public void SetDropContent(ItemDefinition definition, int amount)
		{
			if (definition != null)
			{
				_definition = definition;
			}

			_amount = Mathf.Max(1, amount);
		}

		public bool CanInteract(InteractionContext context)
		{
			return CanBePickedUp &&
			       _inventoryService != null &&
			       _inventoryService.CanAddItem(_definition, Amount, out _);
		}

		public void Interact(InteractionContext context)
		{
			if (!CanInteract(context))
			{
				return;
			}

			InventoryAddResult result = _inventoryService.AddItem(_definition, Amount);

			if (!result.IsSuccess)
			{
				_notificationService?.Show(result.FailureReason);

				TLNLogger.LogWarning(
					$"Cannot pick up {_definition.DisplayName}. Reason: {result.FailureReason}",
					this
				);

				return;
			}

			_notificationService?.Show(Loc.ItemsPickedUp(_definition.DisplayName));
			_feedbackService?.PlayAt(FeedbackEventId.ItemPickedUp, transform.position);

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
				amount = Amount
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

			if (_itemCatalog != null &&
			    _itemCatalog.TryGetItem(data.itemId, out ItemDefinition definition))
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
