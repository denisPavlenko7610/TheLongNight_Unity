using Newtonsoft.Json;
using TLN.Application.Localization;
using TLN.Application.Multiplayer;
using TLN.Application.Notifications;
using TLN.Core.Logging;
using TLN.Core.Validation;
using TLN.Gameplay.Interaction;
using TLN.Gameplay.Inventory;
using TLN.Gameplay.Inventory.Networking;
using TLN.Gameplay.Saves;
using Unity.Netcode;
using UnityEngine;
using VContainer;
using TLN.Application.Feedback;

namespace TLN.Gameplay.Items
{
	[RequireComponent(typeof(PersistentWorldEntity))]
	[RequireComponent(typeof(NetworkObject))]
	public sealed class WorldItemActor : NetworkBehaviour, IInteractable, IWorldSaveable
	{
		private const string SaveType = "world_item";

		[Header("Item")]
		[SerializeField] [Required] private ItemDefinition _definition;
		[SerializeField] private int _amount = 1;

		[Header("Multiplayer Validation")]
		[SerializeField] private float _maxPickupDistance = 3.5f;

		private INotificationService _notificationService;
		private IInventoryService _inventoryService;
		private ItemCatalog _itemCatalog;
		private WorldSaveRegistry _worldSaveRegistry;
		private PersistentWorldEntity _persistentEntity;
		private IMultiplayerSessionService _multiplayerSessionService;
		private IFeedbackService _feedbackService;

		public string SaveTypeId => SaveType;

		public ItemDefinition Definition => _definition;
		public int Amount => Mathf.Max(1, _amount);
		public float MaxPickupDistance => Mathf.Max(0f, _maxPickupDistance);

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
			IMultiplayerSessionService multiplayerSessionService,
			IFeedbackService feedbackService
		)
		{
			_inventoryService = inventoryService;
			_notificationService = notificationService;
			_itemCatalog = itemCatalog;
			_worldSaveRegistry = worldSaveRegistry;
			_multiplayerSessionService = multiplayerSessionService;
			_feedbackService = feedbackService;
		}

		private void Awake()
		{
			_persistentEntity = GetComponent<PersistentWorldEntity>();
		}

		public bool CanInteract(InteractionContext context)
		{
			if (!CanBePickedUp)
			{
				return false;
			}

			if (IsMultiplayer())
			{
				return CanInteractMultiplayer(context);
			}

			return CanInteractOffline();
		}

		public void Interact(InteractionContext context)
		{
			if (!CanInteract(context))
			{
				return;
			}

			if (IsMultiplayer())
			{
				InteractMultiplayer(context);
				return;
			}

			InteractOffline();
		}

		public bool CanBePickedUpBy(Transform playerTransform)
		{
			if (playerTransform == null)
			{
				return false;
			}

			float maxDistance = MaxPickupDistance;
			float maxSqrDistance = maxDistance * maxDistance;
			float sqrDistance = (playerTransform.position - transform.position).sqrMagnitude;

			return sqrDistance <= maxSqrDistance;
		}

		public void CompletePickupServer()
		{
			if (!IsServer)
			{
				return;
			}

			if (_persistentEntity != null && _persistentEntity.IsSceneObject)
			{
				_worldSaveRegistry?.MarkSceneEntityDestroyed(_persistentEntity.Id);
			}

			if (IsSpawned)
			{
				NetworkObject.Despawn(true);
				return;
			}

			Destroy(gameObject);
		}

		private bool IsMultiplayer()
		{
			if (_multiplayerSessionService is { IsMultiplayer: true })
			{
				return true;
			}

			NetworkManager networkManager = NetworkManager.Singleton;

			return networkManager != null && networkManager.IsListening;
		}

		private bool CanInteractOffline()
		{
			if (_inventoryService == null)
			{
				return false;
			}

			return _inventoryService.CanAddItem(_definition, Amount, out _);
		}

		private bool CanInteractMultiplayer(InteractionContext context)
		{
			if (!IsSpawned)
			{
				return false;
			}

			if (context.Player == null)
			{
				return false;
			}

			return context.Player.TryGetComponent(out NetworkPlayerInventory _);
		}

		private void InteractOffline()
		{
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

		private void InteractMultiplayer(InteractionContext context)
		{
			if (context.Player == null)
			{
				return;
			}

			if (!context.Player.TryGetComponent(out NetworkPlayerInventory playerInventory))
			{
				TLNLogger.LogWarning(
					"Cannot pick up network item because local player has no NetworkPlayerInventory.",
					this
				);

				return;
			}

			playerInventory.RequestPickup(this);
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
