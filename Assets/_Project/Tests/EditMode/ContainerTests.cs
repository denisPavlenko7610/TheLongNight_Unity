using System.Collections.Generic;
using NUnit.Framework;
using TLN.Gameplay.Inventory;
using TLN.Gameplay.Items;
using UnityEditor;
using UnityEngine;

namespace TLN.Tests.EditMode
{
	public sealed class ContainerTests
	{
		[Test]
		public void ContainerContents_AddStackable_CombinesStacks()
		{
			ContainerContents contents = new ContainerContents();
			ItemDefinition stick = TestAssetFactory.CreateItem<ItemDefinition>("stick", weight: 0.5f, maxStackSize: 10);

			contents.AddItem(stick, 2);
			contents.AddItem(stick, 3);

			Assert.AreEqual(1, contents.Items.Count);
			Assert.AreEqual(5, contents.Items[0].Amount);
			Assert.AreEqual(2.5f, contents.CurrentWeight, 0.001f);
		}

		[Test]
		public void TryTakeItem_MovesItemsIntoInventory()
		{
			ItemDefinition stick = TestAssetFactory.CreateItem<ItemDefinition>("stick", weight: 0.5f, maxStackSize: 10);

			WorldContainerActor container = CreateContainer(stick, 4);
			InventoryService inventory = CreateInventory(30f);

			bool success = container.TryTakeItem(inventory, 0, 3, out string failureReason);

			Assert.IsTrue(success, failureReason);
			Assert.AreEqual(1, container.Contents.Count);
			Assert.AreEqual(1, container.Contents[0].Amount);
			Assert.AreEqual(1, inventory.Items.Count);
			Assert.AreEqual(3, inventory.Items[0].Amount);
			Assert.AreEqual(1.5f, inventory.CurrentWeight, 0.001f);
		}

		[Test]
		public void TryTakeItem_WhenBackpackWouldOverflow_Fails()
		{
			ItemDefinition log = TestAssetFactory.CreateItem<ItemDefinition>("log", weight: 20f, maxStackSize: 10);

			WorldContainerActor container = CreateContainer(log, 2);
			InventoryService inventory = CreateInventory(30f);

			bool success = container.TryTakeItem(inventory, 0, 2, out string failureReason);

			Assert.IsFalse(success);
			Assert.IsFalse(string.IsNullOrWhiteSpace(failureReason));
			Assert.AreEqual(1, container.Contents.Count);
			Assert.AreEqual(2, container.Contents[0].Amount, "Container must keep items when inventory rejects them.");
			Assert.AreEqual(0, inventory.Items.Count);
		}

		[Test]
		public void TryStoreItem_MovesItemsIntoContainer()
		{
			ItemDefinition apple = TestAssetFactory.CreateItem<ItemDefinition>("apple", weight: 0.2f, maxStackSize: 10);

			WorldContainerActor container = CreateContainer(null, 0);
			InventoryService inventory = CreateInventory(30f);
			inventory.AddItem(apple, 5);

			bool success = container.TryStoreItem(inventory, 0, 2, out string failureReason);

			Assert.IsTrue(success, failureReason);
			Assert.AreEqual(1, container.Contents.Count);
			Assert.AreEqual(2, container.Contents[0].Amount);
			Assert.AreEqual(1, inventory.Items.Count);
			Assert.AreEqual(3, inventory.Items[0].Amount);
		}

		[Test]
		public void TryTakeAll_TakesEverythingWhenBackpackFits()
		{
			ItemDefinition stick = TestAssetFactory.CreateItem<ItemDefinition>("stick", weight: 0.5f, maxStackSize: 10);
			ItemDefinition apple = TestAssetFactory.CreateItem<ItemDefinition>("apple", weight: 0.2f, maxStackSize: 10);

			WorldContainerActor container = CreateContainer(null, 0);
			container.EditorSeedContents(new List<ItemStack>
			{
				new ItemStack(stick, 4),
				new ItemStack(apple, 2),
			});

			InventoryService inventory = CreateInventory(30f);

			int moved = container.TryTakeAll(inventory, out int takenStacks, out string failureReason);

			Assert.Greater(moved, 0);
			Assert.AreEqual(2, takenStacks);
			Assert.IsEmpty(failureReason);
			Assert.AreEqual(0, container.Contents.Count);
			Assert.AreEqual(2, inventory.Items.Count);
		}

		[Test]
		public void TryTakeAll_WhenBackpackTooSmall_TakesOnlyWhatFits()
		{
			ItemDefinition log = TestAssetFactory.CreateItem<ItemDefinition>("log", weight: 20f, maxStackSize: 10);
			ItemDefinition apple = TestAssetFactory.CreateItem<ItemDefinition>("apple", weight: 0.2f, maxStackSize: 10);

			WorldContainerActor container = CreateContainer(null, 0);
			container.EditorSeedContents(new List<ItemStack>
			{
				new ItemStack(log, 2),
				new ItemStack(apple, 1),
			});

			InventoryService inventory = CreateInventory(25f);

			int moved = container.TryTakeAll(inventory, out int takenStacks, out string failureReason);

			Assert.AreEqual(1, moved, "Only the apple should fit into a 25kg backpack next to two 20kg logs.");
			Assert.AreEqual(1, takenStacks);
			Assert.AreEqual(1, container.Contents.Count);
			Assert.AreEqual(2, container.Contents[0].Amount);
		}

		[Test]
		public void LootTable_Roll_WithGuaranteedEntries_AlwaysProducesAmountInRange()
		{
			ItemDefinition stick = TestAssetFactory.CreateItem<ItemDefinition>("stick", weight: 0.5f, maxStackSize: 10);
			ItemDefinition rope = TestAssetFactory.CreateItem<ItemDefinition>("rope", weight: 1f, maxStackSize: 10);

			ContainerLootTable table = ScriptableObject.CreateInstance<ContainerLootTable>();
			WriteLootTable(table, new List<LootEntryData>
			{
				new LootEntryData(stick, 2, 4, 1f),
				new LootEntryData(rope, 1, 1, 1f),
			});

			for (int roll = 0; roll < 20; roll++)
			{
				List<ItemStack> result = table.Roll();

				Assert.AreEqual(2, result.Count, "Two guaranteed entries must always drop.");

				Assert.AreEqual("stick", result[0].Definition.Id);
				Assert.That(result[0].Amount, Is.InRange(2, 4));

				Assert.AreEqual("rope", result[1].Definition.Id);
				Assert.AreEqual(1, result[1].Amount);
			}
		}

		[Test]
		public void SaveRestore_Roundtrip_PreservesContentsAndRollFlag()
		{
			ItemDefinition stick = TestAssetFactory.CreateItem<ItemDefinition>("stick", weight: 0.5f, maxStackSize: 10);
			ItemCatalog catalog = ScriptableObject.CreateInstance<ItemCatalog>();
			catalog.EditorSetItems(new ItemDefinition[] { stick });

			WorldContainerActor container = CreateContainer(stick, 7);

			string json = container.CaptureStateJson();
			Assert.IsFalse(string.IsNullOrWhiteSpace(json));

			WorldContainerActor restored = CreateContainer(null, 0, catalog);
			restored.RestoreStateJson(json);

			Assert.AreEqual(1, restored.Contents.Count);
			Assert.AreEqual("stick", restored.Contents[0].Definition.Id);
			Assert.AreEqual(7, restored.Contents[0].Amount);
			Assert.IsTrue(restored.HasRolledLoot, "Restored container must not re-roll loot.");
		}

		[Test]
		public void SaveRestore_EmptyContainerWithRolledFlag_StaysEmptyOnLoad()
		{
			ItemCatalog catalog = ScriptableObject.CreateInstance<ItemCatalog>();
			catalog.EditorSetItems(new ItemDefinition[0]);

			WorldContainerActor container = CreateContainer(null, 0, catalog);

			string json = container.CaptureStateJson();

			WorldContainerActor restored = CreateContainer(null, 0, catalog);
			restored.RestoreStateJson(json);

			Assert.AreEqual(0, restored.Contents.Count);
			Assert.IsTrue(restored.HasRolledLoot, "Looted-empty container must not re-roll loot on load.");
		}

		private static InventoryService CreateInventory(float maxWeight)
		{
			return new InventoryService(TestAssetFactory.CreateInventoryConfig(maxWeight));
		}

		private static WorldContainerActor CreateContainer(ItemDefinition seedItem, int seedAmount)
		{
			return CreateContainer(seedItem, seedAmount, ScriptableObject.CreateInstance<ItemCatalog>());
		}

		private static WorldContainerActor CreateContainer(
			ItemDefinition seedItem,
			int seedAmount,
			ItemCatalog catalog)
		{
			var go = new GameObject("TestContainer");
			WorldContainerActor actor = go.AddComponent<WorldContainerActor>();
			actor.Construct(null, catalog);

			var items = new List<ItemStack>();

			if (seedItem != null && seedAmount > 0)
			{
				items.Add(new ItemStack(seedItem, seedAmount));
			}

			actor.EditorSeedContents(items);

			return actor;
		}

		private readonly struct LootEntryData
		{
			public readonly ItemDefinition Item;
			public readonly int Min;
			public readonly int Max;
			public readonly float Chance;

			public LootEntryData(ItemDefinition item, int min, int max, float chance)
			{
				Item = item;
				Min = min;
				Max = max;
				Chance = chance;
			}
		}

		private static void WriteLootTable(ContainerLootTable table, List<LootEntryData> entries)
		{
			SerializedObject so = new SerializedObject(table);
			SerializedProperty array = so.FindProperty("_entries");
			array.ClearArray();

			for (int i = 0; i < entries.Count; i++)
			{
				array.InsertArrayElementAtIndex(i);
				SerializedProperty element = array.GetArrayElementAtIndex(i);
				element.FindPropertyRelative("_item").objectReferenceValue = entries[i].Item;
				element.FindPropertyRelative("_minAmount").intValue = entries[i].Min;
				element.FindPropertyRelative("_maxAmount").intValue = entries[i].Max;
				element.FindPropertyRelative("_dropChance").floatValue = entries[i].Chance;
			}

			so.ApplyModifiedPropertiesWithoutUndo();
		}
	}
}
