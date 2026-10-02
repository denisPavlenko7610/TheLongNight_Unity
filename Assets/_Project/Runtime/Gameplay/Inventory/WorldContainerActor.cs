using System;
using Newtonsoft.Json;
using TLN.Application.Localization;
using TLN.Core.Logging;
using TLN.Gameplay.Interaction;
using TLN.Gameplay.Items;
using TLN.Gameplay.Saves;
using UnityEngine;
using VContainer;

namespace TLN.Gameplay.Inventory
{
	[RequireComponent(typeof(PersistentWorldEntity))]
	public sealed class WorldContainerActor : MonoBehaviour, IInteractable, IContainerView, IWorldSaveable
	{
		private const string SaveType = "world_container";

		[Header("Container")]
		[SerializeField] private ContainerLootTable _lootTable;

		[Header("Interaction")]
		[SerializeField] private string _interactionText = "Open";
		[SerializeField] private float _maxInteractionDistance = 3.5f;

		private IContainerWindow _containerWindow;
		private ItemCatalog _itemCatalog;
		private readonly ContainerContents _contents = new();

		private bool _hasRolledLoot;

		public string SaveTypeId => SaveType;

		public string InteractionText => string.IsNullOrWhiteSpace(_interactionText)
			? Loc.ContainerInteract
			: _interactionText;

		public ContainerLootTable LootTable => _lootTable;
		public bool HasRolledLoot => _hasRolledLoot;

		public System.Collections.Generic.IReadOnlyList<ItemStack> Contents => _contents.Items;

		public float CurrentWeight => _contents.CurrentWeight;

		public event Action Changed
		{
			add => _contents.Changed += value;
			remove => _contents.Changed -= value;
		}

		private float MaxInteractionDistance => Mathf.Max(0f, _maxInteractionDistance);

		[Inject]
		public void Construct(IContainerWindow containerWindow, ItemCatalog itemCatalog)
		{
			_containerWindow = containerWindow;
			_itemCatalog = itemCatalog;
		}

		private void Awake()
		{
			RollLootIfNeeded();
		}

		public bool CanInteract(InteractionContext context)
		{
			if (context.Player == null)
			{
				return false;
			}

			float maxSqrDistance =
				MaxInteractionDistance * MaxInteractionDistance;

			return (context.Player.transform.position - transform.position).sqrMagnitude
				<= maxSqrDistance;
		}

		public void Interact(InteractionContext context)
		{
			if (!CanInteract(context))
			{
				return;
			}

			if (_containerWindow == null)
			{
				TLNLogger.LogWarning(
					"Cannot open container window because WorldContainerActor was not constructed.",
					this
				);

				return;
			}

			_containerWindow.Show(this);
		}

		/// <summary>Moves items from the container into the player's inventory.</summary>
		public bool TryTakeItem(
			IInventoryService inventory,
			int containerIndex,
			int amount,
			out string failureReason
		)
		{
			failureReason = null;

			if (inventory == null)
			{
				failureReason = Loc.BedrollInventoryMissing;
				return false;
			}

			if (containerIndex < 0 || containerIndex >= _contents.Items.Count)
			{
				failureReason = Loc.InventoryInvalidSlot;
				return false;
			}

			if (amount <= 0)
			{
				failureReason = Loc.InvalidAmount;
				return false;
			}

			ItemStack stack = _contents.Items[containerIndex];

			if (amount > stack.Amount)
			{
				failureReason = Loc.NotEnough;
				return false;
			}

			if (!inventory.CanAddItem(stack.Definition, amount, out failureReason))
			{
				return false;
			}

			if (!_contents.TryRemoveItemAt(containerIndex, amount, out failureReason))
			{
				return false;
			}

			InventoryAddResult addResult = inventory.AddItem(stack.Definition, amount);

			if (!addResult.IsSuccess)
			{
				// Roll back the container removal if the add unexpectedly failed.
				_contents.AddItem(stack.Definition, amount);
				failureReason = addResult.FailureReason;
				return false;
			}

			failureReason = string.Empty;
			return true;
		}

		/// <summary>Moves every stack the player can carry from the container into the inventory.</summary>
		public int TryTakeAll(
			IInventoryService inventory,
			out int takenStacks,
			out string failureReason
		)
		{
			takenStacks = 0;
			failureReason = string.Empty;

			if (inventory == null)
			{
				failureReason = Loc.BedrollInventoryMissing;
				return 0;
			}

			int movedAmount = 0;

			// Walk backwards so indices stay valid while stacks shrink or vanish.
			for (int i = _contents.Items.Count - 1; i >= 0; i--)
			{
				ItemStack stack = _contents.Items[i];

				if (!inventory.CanAddItem(stack.Definition, stack.Amount, out _))
				{
					continue;
				}

				if (!_contents.TryRemoveItemAt(i, stack.Amount, out _))
				{
					continue;
				}

				InventoryAddResult addResult = inventory.AddItem(stack.Definition, stack.Amount);

				if (!addResult.IsSuccess)
				{
					_contents.AddItem(stack.Definition, stack.Amount);
					continue;
				}

				movedAmount += stack.Amount;
				takenStacks++;
			}

			if (movedAmount == 0)
			{
				failureReason = Loc.ContainerNothingTaken;
			}

			return movedAmount;
		}

		/// <summary>Moves items from the player's inventory into the container.</summary>
		public bool TryStoreItem(
			IInventoryService inventory,
			int inventoryIndex,
			int amount,
			out string failureReason
		)
		{
			failureReason = null;

			if (inventory == null)
			{
				failureReason = Loc.BedrollInventoryMissing;
				return false;
			}

			if (inventoryIndex < 0 || inventoryIndex >= inventory.Items.Count)
			{
				failureReason = Loc.InventoryInvalidSlot;
				return false;
			}

			if (amount <= 0)
			{
				failureReason = Loc.InvalidAmount;
				return false;
			}

			ItemStack stack = inventory.Items[inventoryIndex];

			if (amount > stack.Amount)
			{
				failureReason = Loc.NotEnough;
				return false;
			}

			if (!_contents.CanAddItem(stack.Definition, amount))
			{
				failureReason = Loc.InventoryInvalidItem;
				return false;
			}

			if (!inventory.TryRemoveItemAt(inventoryIndex, amount, out failureReason))
			{
				return false;
			}

			InventoryAddResult addResult = _contents.AddItem(stack.Definition, amount);

			if (!addResult.IsSuccess)
			{
				// Roll back the inventory removal if the add unexpectedly failed.
				inventory.AddItem(stack.Definition, amount);
				failureReason = addResult.FailureReason;
				return false;
			}

			failureReason = string.Empty;
			return true;
		}

		private void RollLootIfNeeded()
		{
			if (_hasRolledLoot || _lootTable == null)
			{
				return;
			}

			System.Collections.Generic.List<ItemStack> loot = _lootTable.Roll();
			_contents.ReplaceItems(loot);
			_hasRolledLoot = true;
		}

#if UNITY_EDITOR
		/// <summary>Editor-only hook for EditMode tests to seed contents and mark loot as rolled.</summary>
		public void EditorSeedContents(System.Collections.Generic.List<ItemStack> items)
		{
			_contents.ReplaceItems(items);
			_hasRolledLoot = true;
		}
#endif

		public string CaptureStateJson()
		{
			ContainerSaveData data = new ContainerSaveData
			{
				hasRolledLoot = _hasRolledLoot,
				items = new ContainerItemData[_contents.Items.Count]
			};

			for (int i = 0; i < _contents.Items.Count; i++)
			{
				ItemStack stack = _contents.Items[i];

				data.items[i] = new ContainerItemData
				{
					itemId = stack.Definition.Id,
					amount = stack.Amount
				};
			}

			return JsonConvert.SerializeObject(data);
		}

		public void RestoreStateJson(string json)
		{
			if (string.IsNullOrWhiteSpace(json))
			{
				return;
			}

			ContainerSaveData data = JsonConvert.DeserializeObject<ContainerSaveData>(json);

			if (data == null)
			{
				return;
			}

			_hasRolledLoot = data.hasRolledLoot;

			System.Collections.Generic.List<ItemStack> restored =
				new System.Collections.Generic.List<ItemStack>();

			if (data.items != null)
			{
				for (int i = 0; i < data.items.Length; i++)
				{
					ContainerItemData itemData = data.items[i];

					if (itemData == null ||
					    string.IsNullOrWhiteSpace(itemData.itemId) ||
					    itemData.amount <= 0 ||
					    _itemCatalog == null ||
					    !_itemCatalog.TryGetItem(itemData.itemId, out ItemDefinition definition))
					{
						continue;
					}

					restored.Add(new ItemStack(definition, itemData.amount));
				}
			}

			_contents.ReplaceItems(restored);
		}

		private sealed class ContainerSaveData
		{
			public bool hasRolledLoot;
			public ContainerItemData[] items;
		}

		private sealed class ContainerItemData
		{
			public string itemId;
			public int amount;
		}
	}
}
