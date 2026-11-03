using System.Collections.Generic;
using TLN.Application.Notifications;
using TLN.Gameplay.Building;
using TLN.Gameplay.Placement;
using TLN.Gameplay.Player;
using TLN.Gameplay.World;
using UnityEngine;
using NUnit.Framework;
using TLN.Gameplay.Inventory;
using TLN.Gameplay.Items;

namespace TLN.Tests.EditMode
{
	public sealed class InventoryServiceTests
	{
		[TestCase(false, 2)]
		[TestCase(false, 5)]
		[TestCase(true, 2)]
		[TestCase(true, 5)]
		public void RemoveItem_UpdatesStackAndWeightBeforeSingleChangedEvent(bool byDefinition, int amount)
		{
			InventoryConfig config = TestAssetFactory.CreateInventoryConfig(30f);
			ItemDefinition item = TestAssetFactory.CreateItem<ItemDefinition>("stick", weight: 0.5f);
			try
			{
				InventoryService inventory = new InventoryService(config);
				inventory.AddItem(item, 5);
				int events = 0;
				inventory.Changed += () =>
				{
					events++;
					Assert.AreEqual((5 - amount) * 0.5f, inventory.CurrentWeight);
					Assert.AreEqual(amount == 5 ? 0 : 1, inventory.Items.Count);
				};
				string reason;
				bool removed = byDefinition
					? inventory.TryRemoveItem(item, amount, out reason)
					: inventory.TryRemoveItemAt(0, amount, out reason);
				Assert.IsTrue(removed, reason);
				Assert.IsEmpty(reason);
				Assert.AreEqual(1, events);
				if (amount < 5)
				{
					Assert.AreEqual(5 - amount, inventory.Items[0].Amount);
				}
			}
			finally
			{
				UnityEngine.Object.DestroyImmediate(item);
				UnityEngine.Object.DestroyImmediate(config);
			}
		}

		[Test]
		public void TryRemoveItem_MatchesIdAndRemovesFromLastStackFirst()
		{
			InventoryConfig config = TestAssetFactory.CreateInventoryConfig(30f);
			ItemDefinition item = TestAssetFactory.CreateItem<ItemDefinition>("stick", weight: 0.5f);
			ItemDefinition sameId = TestAssetFactory.CreateItem<ItemDefinition>("stick", weight: 0.5f);
			ItemDefinition other = TestAssetFactory.CreateItem<ItemDefinition>("rope", weight: 1f);
			try
			{
				InventoryService inventory = new InventoryService(config);
				inventory.ReplaceItems(
					new[] { new ItemStack(item, 4), new ItemStack(other, 1), new ItemStack(item, 3) }
				);
				int events = 0;
				inventory.Changed += () => events++;
				Assert.IsTrue(inventory.TryRemoveItem(sameId, 5, out string reason), reason);
				Assert.AreEqual(2, inventory.Items.Count);
				Assert.AreSame(item, inventory.Items[0].Definition);
				Assert.AreEqual(2, inventory.Items[0].Amount);
				Assert.AreSame(other, inventory.Items[1].Definition);
				Assert.AreEqual(2f, inventory.CurrentWeight);
				Assert.AreEqual(1, events);
			}
			finally
			{
				UnityEngine.Object.DestroyImmediate(item);
				UnityEngine.Object.DestroyImmediate(sameId);
				UnityEngine.Object.DestroyImmediate(other);
				UnityEngine.Object.DestroyImmediate(config);
			}
		}

		[TestCase(false, 0)]
		[TestCase(false, 6)]
		[TestCase(true, 0)]
		[TestCase(true, 6)]
		public void RemoveItem_InvalidAmount_PreservesStateWithoutChangedEvent(bool byDefinition, int amount)
		{
			InventoryConfig config = TestAssetFactory.CreateInventoryConfig(30f);
			ItemDefinition item = TestAssetFactory.CreateItem<ItemDefinition>("stick", weight: 0.5f);
			try
			{
				InventoryService inventory = new InventoryService(config);
				inventory.AddItem(item, 5);
				int events = 0;
				inventory.Changed += () => events++;
				string reason;
				bool removed = byDefinition
					? inventory.TryRemoveItem(item, amount, out reason)
					: inventory.TryRemoveItemAt(0, amount, out reason);
				Assert.IsFalse(removed);
				Assert.IsNotEmpty(reason);
				Assert.AreEqual(1, inventory.Items.Count);
				Assert.AreEqual(5, inventory.Items[0].Amount);
				Assert.AreEqual(2.5f, inventory.CurrentWeight);
				Assert.AreEqual(0, events);
			}
			finally
			{
				UnityEngine.Object.DestroyImmediate(item);
				UnityEngine.Object.DestroyImmediate(config);
			}
		}

		[Test]
		public void AddItem_StackableItem_CombinesIntoSingleStack()
		{
			InventoryConfig config = TestAssetFactory.CreateInventoryConfig(30f);

			ItemDefinition stick = TestAssetFactory.CreateItem<ItemDefinition>(
					"stick",
					weight: 0.5f,
					isStackable: true,
					maxStackSize: 10
				);

			InventoryService inventory = new InventoryService(config);

			InventoryAddResult firstResult = inventory.AddItem(stick, 2);
			InventoryAddResult secondResult = inventory.AddItem(stick, 3);

			Assert.IsTrue(firstResult.IsSuccess);
			Assert.IsTrue(secondResult.IsSuccess);

			Assert.AreEqual(1, inventory.Items.Count);
			Assert.AreEqual(5, inventory.Items[0].Amount);
			Assert.AreEqual(2.5f, inventory.CurrentWeight, 0.001f);
		}

		[Test]
		public void CanAddItem_WhenWeightWouldExceedLimit_ReturnsFalse()
		{
			InventoryConfig config = TestAssetFactory.CreateInventoryConfig(30f);

			ItemDefinition log = TestAssetFactory.CreateItem<ItemDefinition>(
					"log",
					weight: 20f,
					isStackable: true,
					maxStackSize: 10
				);

			InventoryService inventory = new InventoryService(config);

			bool canAdd = inventory.CanAddItem(
				log,
				2,
				out string reason
			);

			Assert.IsFalse(canAdd);
			Assert.IsFalse(string.IsNullOrWhiteSpace(reason));
			Assert.AreEqual(0, inventory.Items.Count);
		}

		[Test]
		public void TryRemoveItemAt_WhenRemovingPartialAmount_LeavesRemainingStack()
		{
			InventoryConfig config = TestAssetFactory.CreateInventoryConfig(30f);

			ItemDefinition stick = TestAssetFactory.CreateItem<ItemDefinition>(
					"stick",
					weight: 0.5f,
					isStackable: true,
					maxStackSize: 10
				);

			InventoryService inventory = new InventoryService(config);
			inventory.AddItem(stick, 5);

			bool removed = inventory.TryRemoveItemAt(
				0,
				2,
				out string reason
			);

			Assert.IsTrue(removed, reason);
			Assert.AreEqual(1, inventory.Items.Count);
			Assert.AreEqual(3, inventory.Items[0].Amount);
			Assert.AreEqual(1.5f, inventory.CurrentWeight, 0.001f);
		}
	}
}

namespace TLN.Tests.EditMode
{
	public sealed class RefactorRegressionTests
	{
		private readonly List<Object> _assets = new();
		private T Track<T>(T asset) where T : Object { _assets.Add(asset); return asset; }
		[TearDown]
		public void TearDown()
		{
			for (int i = _assets.Count - 1; i >= 0; i--)
				if (_assets[i] != null) Object.DestroyImmediate(_assets[i]);
			_assets.Clear();
		}

		[TestCase(false)]
		[TestCase(true)]
		public void ReplaceOwnItems_PreservesContentsAndNotifiesOnce(bool container)
		{
			var item = Track(TestAssetFactory.CreateItem<ItemDefinition>("stick", weight: 0.5f));
			int events = 0;
			if (container)
			{
				var contents = new ContainerContents();
				contents.AddItem(item, 4);
				contents.Changed += () => events++;
				contents.ReplaceItems(contents.Items);
				Assert.AreEqual(4, contents.Items[0].Amount);
				Assert.AreEqual(2f, contents.CurrentWeight);
			}
			else
			{
				var inventory = new InventoryService(Track(TestAssetFactory.CreateInventoryConfig(30)));
				inventory.AddItem(item, 4);
				inventory.Changed += () => events++;
				inventory.ReplaceItems(inventory.Items);
				Assert.AreEqual(4, inventory.Items[0].Amount);
				Assert.AreEqual(2f, inventory.CurrentWeight);
			}
			Assert.AreEqual(1, events);
		}

		[TestCase(false)]
		[TestCase(true)]
		public void CanBuild_PreservesFirstFailureWhenLaterIngredientIsInvalid(bool invalidFirst)
		{
			var item = Track(TestAssetFactory.CreateItem<ItemDefinition>("stick"));
			var inventory = new InventoryService(Track(TestAssetFactory.CreateInventoryConfig(30)));
			var recipe = Track(ScriptableObject.CreateInstance<BuildRecipeDefinition>());
			TestAssetFactory.SetPrivateField(recipe, "_placedPrefab", Track(new GameObject("recipe prefab")));
			var ingredient = new BuildRecipeIngredient();
			TestAssetFactory.SetPrivateField(ingredient, "_item", item);
			TestAssetFactory.SetPrivateField(recipe, "_ingredients", invalidFirst
				? new[] { null, ingredient }
				: new[] { ingredient, null });
			var service = new BuildService(inventory, null, new Notifications());

			Assert.IsFalse(service.CanBuild(recipe, out string reason));
			Assert.AreEqual(invalidFirst
				? TLN.Application.Localization.Loc.InvalidIngredient
				: TLN.Application.Localization.Loc.NotEnoughItems(item.DisplayName, 1, 0), reason);
		}

		[Test]
		public void CanBuild_CombinedAmountExceedsIntMaxValue_RejectsAndClampsMessage()
		{
			var item = Track(TestAssetFactory.CreateItem<ItemDefinition>("stick", weight: 0f));
			var sameId = Track(TestAssetFactory.CreateItem<ItemDefinition>("stick", weight: 0f));
			var inventory = new InventoryService(Track(TestAssetFactory.CreateInventoryConfig(30)));
			inventory.ReplaceItems(new[] { new ItemStack(item, int.MaxValue) });
			var recipe = Track(ScriptableObject.CreateInstance<BuildRecipeDefinition>());
			TestAssetFactory.SetPrivateField(recipe, "_placedPrefab", Track(new GameObject("recipe prefab")));
			var first = new BuildRecipeIngredient();
			var second = new BuildRecipeIngredient();
			TestAssetFactory.SetPrivateField(first, "_item", item);
			TestAssetFactory.SetPrivateField(first, "_amount", int.MaxValue);
			TestAssetFactory.SetPrivateField(second, "_item", sameId);
			TestAssetFactory.SetPrivateField(recipe, "_ingredients", new[] { first, second });
			var service = new BuildService(inventory, null, new Notifications());
			int events = 0;
			inventory.Changed += () => events++;

			Assert.IsFalse(service.CanBuild(recipe, out string reason));
			Assert.AreEqual(TLN.Application.Localization.Loc.NotEnoughItems(item.DisplayName, int.MaxValue, int.MaxValue), reason);
			Assert.AreEqual(int.MaxValue, inventory.CountItems(item));
			Assert.AreEqual(0, events);
		}

		[TestCase(4, false)]
		[TestCase(7, true)]
		public void Build_RepeatedIngredientIds_RequiresCombinedAmountAndConsumesAtomically(int available, bool success)
		{
			var item = Track(TestAssetFactory.CreateItem<ItemDefinition>("stick", weight: 0.5f));
			var sameId = Track(TestAssetFactory.CreateItem<ItemDefinition>("stick", weight: 0.5f));
			var inventory = new InventoryService(Track(TestAssetFactory.CreateInventoryConfig(30)));
			inventory.AddItem(item, available);
			var recipe = Track(ScriptableObject.CreateInstance<BuildRecipeDefinition>());
			var prefab = Track(new GameObject("recipe prefab"));
			TestAssetFactory.SetPrivateField(recipe, "_placedPrefab", prefab);
			var first = new BuildRecipeIngredient();
			var second = new BuildRecipeIngredient();
			TestAssetFactory.SetPrivateField(first, "_item", item);
			TestAssetFactory.SetPrivateField(second, "_item", sameId);
			TestAssetFactory.SetPrivateField(first, "_amount", 3);
			TestAssetFactory.SetPrivateField(second, "_amount", 3);
			TestAssetFactory.SetPrivateField(recipe, "_ingredients", new[] { first, second });

			var ground = Track(GameObject.CreatePrimitive(PrimitiveType.Cube));
			ground.transform.position = new Vector3(0, -1, 0);
			ground.transform.localScale = new Vector3(20, 1, 20);
			var player = Track(new GameObject("player")).AddComponent<PlayerRoot>();
			var camera = player.gameObject.AddComponent<Camera>();
			TestAssetFactory.SetPrivateField(player, "<Camera>k__BackingField", camera);
			Physics.SyncTransforms();
			var factory = new Factory(this);
			var placement = new PlacementService(factory);
			placement.SetPlayerRoot(player);
			var service = new BuildService(inventory, placement, new Notifications());
			int events = 0;
			inventory.Changed += () =>
			{
				events++;
				Assert.AreEqual(available - 6, inventory.CountItems(item));
			};
			Assert.AreEqual(success, service.CanBuild(recipe, out _));
			Assert.AreEqual(success, service.Build(recipe).IsSuccess);
			Assert.AreEqual(success ? available - 6 : available, inventory.CountItems(item));
			Assert.AreEqual(success ? 1 : 0, events);
			Assert.AreEqual(success ? 1 : 0, factory.Created);
		}

		[Test]
		public void Drop_InvalidPickupPrefab_PreservesInventoryBeforePlacement()
		{
			var item = Track(TestAssetFactory.CreateItem<ItemDefinition>("stick"));
			var inventory = new InventoryService(Track(TestAssetFactory.CreateInventoryConfig(30)));
			inventory.AddItem(item, 2);
			var catalog = Track(ScriptableObject.CreateInstance<WorldItemPrefabCatalog>());
			catalog.EditorSetEntries(new[] { new WorldItemPrefabCatalog.EntryDraft(item.Id, Track(new GameObject("invalid pickup"))) });
			var service = new ItemDropService(inventory, null, catalog);
			int events = 0;
			inventory.Changed += () => events++;
			Assert.IsFalse(service.DropItemAt(0, 1, out string reason));
			Assert.IsNotEmpty(reason);
			Assert.AreEqual(2, inventory.CountItems(item));
			Assert.AreEqual(0, events);
		}

		private sealed class Factory : IWorldObjectFactory
		{
			private readonly RefactorRegressionTests _owner;
			public int Created { get; private set; }
			public Factory(RefactorRegressionTests owner) { _owner = owner; }
			public GameObject Create(GameObject prefab, Vector3 position, Quaternion rotation)
			{
				Created++;
				return _owner.Track(Object.Instantiate(prefab, position, rotation));
			}
		}
		private sealed class Notifications : INotificationService
		{
			public void SetView(INotificationView view) { }
			public void Show(string message) { }
		}
	}
}
