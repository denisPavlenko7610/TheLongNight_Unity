using Newtonsoft.Json;
using TLN.Application.Localization;
using TLN.Application.Notifications;
using TLN.Core.Logging;
using TLN.Gameplay.Interaction;
using TLN.Gameplay.Inventory;
using TLN.Gameplay.Items;
using TLN.Gameplay.Saves;
using UnityEngine;
using VContainer;

namespace TLN.Gameplay.Sleep
{
	[RequireComponent(typeof(PersistentWorldEntity))]
	public sealed class BedrollActor : MonoBehaviour, IInteractable, IWorldSaveable
	{
		private const string SaveType = "bedroll";

		[Header("Interaction")]
		[SerializeField] private string _interactionText = "Sleep";

		[Header("Pickup")]
		[SerializeField] private bool _canPickUp = true;
		[SerializeField] private ItemDefinition _packedItemDefinition;
		[SerializeField] private int _packedAmount = 1;

		private ISleepWindow _sleepWindow;
		private IInventoryService _inventoryService;
		private INotificationService _notificationService;

		private WorldSaveRegistry _worldSaveRegistry;
		private ItemCatalog _itemCatalog;
		private PersistentWorldEntity _persistentEntity;

		public string InteractionText => _interactionText;
		public bool CanPickUp => _canPickUp;
		public ItemDefinition PackedItemDefinition => _packedItemDefinition;
		public int PackedAmount => Mathf.Max(1, _packedAmount);

		public string SaveTypeId => SaveType;

		[Inject]
		public void Construct(
			ISleepWindow sleepWindow,
			IInventoryService inventoryService,
			INotificationService notificationService,

			WorldSaveRegistry worldSaveRegistry,
			ItemCatalog itemCatalog
		)
		{
			_sleepWindow = sleepWindow;
			_inventoryService = inventoryService;
			_notificationService = notificationService;

			_worldSaveRegistry = worldSaveRegistry;
			_itemCatalog = itemCatalog;
		}

		private void Awake()
		{
			_persistentEntity = GetComponent<PersistentWorldEntity>();
		}

		public bool CanInteract(InteractionContext context)
		{
			return true;
		}

		public void Interact(InteractionContext context)
		{
			if (_sleepWindow == null)
			{
				TLNLogger.LogWarning(
					"Cannot open sleep window because BedrollActor was not constructed.",
					this
				);

				return;
			}

			_sleepWindow.Show(this);
		}

		public bool TryPickUp()
		{
			if (!_canPickUp)
			{
				return false;
			}

			if (_packedItemDefinition == null)
			{
				_notificationService?.Show(LocalizationKeys.BedrollPickupFailed);
				return false;
			}

			if (_inventoryService == null)
			{
				_notificationService?.Show(LocalizationKeys.BedrollInventoryMissing);
				return false;
			}

			InventoryAddResult result =
				_inventoryService.AddItem(
					_packedItemDefinition,
					PackedAmount
				);

			if (!result.IsSuccess)
			{
				_notificationService?.Show(result.FailureReason);
				return false;
			}

			_notificationService?.Show(LocalizationKeys.BedrollPickedUp(_packedItemDefinition.DisplayName));

			if (_persistentEntity != null &&
				_persistentEntity.IsSceneObject)
			{
				_worldSaveRegistry?.MarkSceneEntityDestroyed(_persistentEntity.Id);
			}

			Destroy(gameObject);
			return true;
		}

		public string CaptureStateJson()
		{
			BedrollSaveData data = new BedrollSaveData
			{
				canPickUp = _canPickUp,

				packedItemId = _packedItemDefinition == null
					? string.Empty
					: _packedItemDefinition.Id,

				packedAmount = PackedAmount
			};

			return JsonConvert.SerializeObject(data);
		}

		public void RestoreStateJson(string json)
		{
			if (string.IsNullOrWhiteSpace(json))
			{
				return;
			}

			BedrollSaveData data =
				JsonConvert.DeserializeObject<BedrollSaveData>(json);

			if (data == null)
			{
				return;
			}

			_canPickUp = data.canPickUp;
			_packedAmount = Mathf.Max(1, data.packedAmount);

			if (_itemCatalog != null &&
				_itemCatalog.TryGetItem(
					data.packedItemId,
					out ItemDefinition packedItem
				))
			{
				_packedItemDefinition = packedItem;
			}
		}

		private sealed class BedrollSaveData
		{
			public bool canPickUp;
			public string packedItemId;
			public int packedAmount;
		}
	}
}
