using System;
using System.Collections.Generic;
using TLN.Application.Localization;
using TLN.Application.Notifications;
using TLN.Gameplay.Items;
using TLN.Gameplay.Survival.Networking;
using Unity.Netcode;
using UnityEngine;
using VContainer;

namespace TLN.Gameplay.Inventory.Networking
{
	[RequireComponent(typeof(NetworkObject))]
	[RequireComponent(typeof(NetworkPlayerSurvival))]
	public sealed class NetworkPlayerInventory : NetworkBehaviour, IInventoryService, IItemUseService
	{
		private const int SingleItemAmount = 1;

		private readonly List<ItemStack> _items = new();

		private NetworkList<NetworkInventoryItem> _networkItems;

		private InventoryConfig _config;
		private ItemCatalog _itemCatalog;
		private INotificationService _notificationService;
		private NetworkPlayerSurvival _survivalService;

		private bool _isConstructed;
		private bool _isCacheDirty = true;

		public IReadOnlyList<ItemStack> Items
		{
			get
			{
				RebuildCacheIfNeeded();
				return _items;
			}
		}

		public float CurrentWeight { get; private set; }

		public float MaxCarryWeight => _config.MaxCarryWeight;

		public event Action Changed;

		private void Awake()
		{
			EnsureNetworkItemsInitialized();
			_survivalService = GetComponent<NetworkPlayerSurvival>();
		}

		[Inject]
		public void Construct(
			InventoryConfig config,
			ItemCatalog itemCatalog,
			INotificationService notificationService
		)
		{
			_config = config ?? throw new ArgumentNullException(nameof(config));
			_itemCatalog = itemCatalog ?? throw new ArgumentNullException(nameof(itemCatalog));
			_notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));

			EnsureNetworkItemsInitialized();

			_isConstructed = true;

			MarkCacheDirtyAndNotify();
		}

		public override void OnNetworkSpawn()
		{
			EnsureNetworkItemsInitialized();
			_networkItems.OnListChanged += OnNetworkItemsChanged;
			MarkCacheDirtyAndNotify();
		}

		public override void OnNetworkDespawn()
		{
			_networkItems.OnListChanged -= OnNetworkItemsChanged;
		}

		private void OnDestroy()
		{
			_networkItems?.Dispose();
		}

		public InventoryAddResult AddItem(ItemDefinition definition, int amount)
		{
			if (!CanMutateInventory())
			{
				return InventoryAddResult.Failure(Loc.CannotUse);
			}

			if (!CanAddItem(definition, amount, out string reason))
			{
				return InventoryAddResult.Failure(reason);
			}

			AddItemServer(definition, amount);
			return InventoryAddResult.Success();
		}

		public bool CanAddItem(ItemDefinition definition, int amount, out string reason)
		{
			EnsureConstructed();
			RebuildCacheIfNeeded();

			if (definition == null)
			{
				reason = Loc.InventoryInvalidItem;
				return false;
			}

			if (amount <= 0)
			{
				reason = Loc.InvalidAmount;
				return false;
			}

			float addedWeight = definition.Weight * amount;
			float weightAfterAdd = CurrentWeight + addedWeight;

			if (weightAfterAdd > MaxCarryWeight)
			{
				reason = Loc.TooHeavy;
				return false;
			}

			reason = string.Empty;
			return true;
		}

		public bool TryRemoveItemAt(int index, int amount, out string reason)
		{
			if (!CanMutateInventory())
			{
				reason = Loc.CannotUse;
				return false;
			}

			RebuildCacheIfNeeded();

			if (index < 0 || index >= _networkItems.Count)
			{
				reason = Loc.InventoryInvalidSlot;
				return false;
			}

			if (amount <= 0)
			{
				reason = Loc.InvalidAmount;
				return false;
			}

			NetworkInventoryItem networkItem = _networkItems[index];

			if (amount > networkItem.Amount)
			{
				reason = Loc.NotEnough;
				return false;
			}

			networkItem.Amount -= amount;

			if (networkItem.Amount <= 0)
			{
				_networkItems.RemoveAt(index);
			}
			else
			{
				_networkItems[index] = networkItem;
			}

			MarkCacheDirtyAndNotify();

			reason = string.Empty;
			return true;
		}

		public bool TryRemoveItem(ItemDefinition definition, int amount, out string reason)
		{
			if (!CanMutateInventory())
			{
				reason = Loc.CannotUse;
				return false;
			}

			if (definition == null)
			{
				reason = Loc.InventoryInvalidItem;
				return false;
			}

			if (amount <= 0)
			{
				reason = Loc.InvalidAmount;
				return false;
			}

			int availableAmount = CountItems(definition);

			if (availableAmount < amount)
			{
				reason = Loc.InventoryNotEnoughItem(definition.DisplayName);
				return false;
			}

			int remainingAmount = amount;

			for (int i = _networkItems.Count - 1; i >= 0; i--)
			{
				if (remainingAmount <= 0)
				{
					break;
				}

				NetworkInventoryItem networkItem = _networkItems[i];

				if (networkItem.ItemId.ToString() != definition.Id)
				{
					continue;
				}

				int amountToRemove = Mathf.Min(remainingAmount, networkItem.Amount);
				networkItem.Amount -= amountToRemove;
				remainingAmount -= amountToRemove;

				if (networkItem.Amount <= 0)
				{
					_networkItems.RemoveAt(i);
				}
				else
				{
					_networkItems[i] = networkItem;
				}
			}

			MarkCacheDirtyAndNotify();

			reason = string.Empty;
			return true;
		}

		public void ReplaceItems(IReadOnlyList<ItemStack> items)
		{
			if (!CanMutateInventory())
			{
				return;
			}

			_networkItems.Clear();

			if (items == null)
			{
				MarkCacheDirtyAndNotify();
				return;
			}

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

				AddItemServer(stack.Definition, stack.Amount);
			}

			MarkCacheDirtyAndNotify();
		}

		public ItemUseResult UseItemAt(int index)
		{
			RebuildCacheIfNeeded();

			if (index < 0 || index >= _items.Count)
			{
				return ItemUseResult.Failure(Loc.ItemsInvalidSlot);
			}

			if (!IsSpawned || IsServer)
			{
				return UseItemAtServer(index);
			}

			if (!IsOwner)
			{
				return ItemUseResult.Failure(Loc.CannotUse);
			}

			RequestUseItemAtServerRpc(index);

			return ItemUseResult.Success(string.Empty);
		}

		[Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
		private void RequestUseItemAtServerRpc(int index)
		{
			if (!_isConstructed)
			{
				return;
			}

			if (!IsServer)
			{
				return;
			}

			UseItemAtServer(index);
		}

		private ItemUseResult UseItemAtServer(int index)
		{
			if (!CanMutateInventory())
			{
				return FailUse(Loc.CannotUse);
			}

			RebuildCacheIfNeeded();

			if (index < 0 || index >= _items.Count)
			{
				return FailUse(Loc.ItemsInvalidSlot);
			}

			ItemStack stack = _items[index];

			return stack.Definition.UseKind switch
			{
				ItemUseKind.Consumable => UseConsumableAtServer(index, stack),
				ItemUseKind.Clothing => FailUse(Loc.CannotEquip),
				ItemUseKind.Placeable => FailUse(Loc.CannotPlace),
				_ => FailUse(Loc.CannotUse)
			};
		}

		private ItemUseResult UseConsumableAtServer(int index, ItemStack stack)
		{
			if (stack.Definition is not ConsumableItemDefinition consumable)
			{
				return FailUse(Loc.CannotConsume);
			}

			if (_survivalService == null)
			{
				return FailUse(Loc.ServiceMissingItemUse);
			}

			bool wasRemoved = TryRemoveItemAt(index, SingleItemAmount, out string removeFailureReason);

			if (!wasRemoved)
			{
				return FailUse(removeFailureReason);
			}

			_survivalService.ApplyConsumable(consumable);

			string message = Loc.Used(consumable.DisplayName);
			NotifyOwner(message);

			return ItemUseResult.Success(message);
		}

		private ItemUseResult FailUse(string message)
		{
			NotifyOwner(message);
			return ItemUseResult.Failure(message);
		}

		private void AddItemServer(ItemDefinition definition, int amount)
		{
			if (definition.IsStackable)
			{
				AddStackableItemServer(definition, amount);
			}
			else
			{
				AddNonStackableItemServer(definition, amount);
			}

			MarkCacheDirtyAndNotify();
		}

		private void AddStackableItemServer(ItemDefinition definition, int amount)
		{
			int remainingAmount = amount;

			for (int i = 0; i < _networkItems.Count; i++)
			{
				NetworkInventoryItem networkItem = _networkItems[i];

				if (networkItem.ItemId.ToString() != definition.Id)
				{
					continue;
				}

				int freeSpace = definition.MaxStackSize - networkItem.Amount;

				if (freeSpace <= 0)
				{
					continue;
				}

				int amountToAdd = Mathf.Min(remainingAmount, freeSpace);

				networkItem.Amount += amountToAdd;
				_networkItems[i] = networkItem;

				remainingAmount -= amountToAdd;

				if (remainingAmount <= 0)
				{
					return;
				}
			}

			while (remainingAmount > 0)
			{
				int amountToAdd = Mathf.Min(remainingAmount, definition.MaxStackSize);
				_networkItems.Add(new NetworkInventoryItem(definition.Id, amountToAdd));
				remainingAmount -= amountToAdd;
			}
		}

		private void AddNonStackableItemServer(ItemDefinition definition, int amount)
		{
			for (int i = 0; i < amount; i++)
			{
				_networkItems.Add(new NetworkInventoryItem(definition.Id, SingleItemAmount));
			}
		}

		private int CountItems(ItemDefinition definition)
		{
			if (definition == null)
			{
				return 0;
			}

			int amount = 0;

			for (int i = 0; i < _networkItems.Count; i++)
			{
				NetworkInventoryItem networkItem = _networkItems[i];

				if (networkItem.ItemId.ToString() == definition.Id)
				{
					amount += networkItem.Amount;
				}
			}

			return amount;
		}

		private void OnNetworkItemsChanged(NetworkListEvent<NetworkInventoryItem> changeEvent)
		{
			MarkCacheDirtyAndNotify();
		}

		private void MarkCacheDirtyAndNotify()
		{
			_isCacheDirty = true;
			RebuildCacheIfNeeded();
			Changed?.Invoke();
		}

		private void RebuildCacheIfNeeded()
		{
			if (!_isCacheDirty)
			{
				return;
			}

			_items.Clear();
			CurrentWeight = 0f;

			if (!_isConstructed)
			{
				return;
			}

			EnsureNetworkItemsInitialized();

			for (int i = 0; i < _networkItems.Count; i++)
			{
				NetworkInventoryItem networkItem = _networkItems[i];

				if (networkItem.Amount <= 0)
				{
					continue;
				}

				string itemId = networkItem.ItemId.ToString();

				if (!_itemCatalog.TryGetItem(itemId, out ItemDefinition definition))
				{
					continue;
				}

				_items.Add(new ItemStack(definition, networkItem.Amount));
				CurrentWeight += definition.Weight * networkItem.Amount;
			}

			_isCacheDirty = false;
		}

		private bool CanMutateInventory()
		{
			return _isConstructed && (!IsSpawned || IsServer);
		}

		private void EnsureConstructed()
		{
			if (!_isConstructed)
			{
				throw new InvalidOperationException(
					"NetworkPlayerInventory must be constructed before inventory is accessed."
				);
			}
		}

		private void EnsureNetworkItemsInitialized()
		{
			_networkItems ??= new NetworkList<NetworkInventoryItem>();
		}

		private void NotifyOwner(string message)
		{
			if (string.IsNullOrWhiteSpace(message))
			{
				return;
			}

			if (IsSpawned && IsServer)
			{
				ShowOwnerNotificationRpc(message);
				return;
			}

			_notificationService.Show(message);
		}

		[Rpc(SendTo.Owner)]
		private void ShowOwnerNotificationRpc(string message)
		{
			_notificationService?.Show(message);
		}
	}
}
