using NUnit.Framework;
using System;
using System.IO;
using TLN.Application.Saves;
using TLN.Gameplay.Equipment;
using TLN.Gameplay.Inventory;
using TLN.Gameplay.Items;
using TLN.Gameplay.Placement;
using TLN.Gameplay.Saves;
using TLN.Infrastructure.Saves;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TLN.Tests.EditMode
{
	public sealed class PlayerEquipmentServiceTests
	{
		[Test]
		public void RemovingLastItem_UnequipsClothingAndRemovesWarmth()
		{
			InventoryConfig config = TestAssetFactory.CreateInventoryConfig(30f);
			ClothingItemDefinition clothing = CreateClothing();
			try
			{
				InventoryService inventory = new(config);
				using PlayerEquipmentService equipment = new(inventory);
				Assert.IsFalse(equipment.ToggleEquip(clothing).IsSuccess);
				inventory.AddItem(clothing, 2);
				Assert.IsTrue(equipment.ToggleEquip(clothing).IsSuccess);
				inventory.TryRemoveItem(clothing, 1, out _);
				Assert.IsTrue(equipment.IsEquipped(clothing));
				Assert.AreEqual(5f, equipment.WarmthBonus);
				inventory.TryRemoveItem(clothing, 1, out _);
				Assert.IsFalse(equipment.IsEquipped(clothing));
				Assert.AreEqual(0f, equipment.WarmthBonus);
			}
			finally
			{
				Object.DestroyImmediate(clothing);
				Object.DestroyImmediate(config);
			}
		}

		[Test]
		public void RestoreEquipment_OnlyEquipsOwnedItemsAndClearsPreviousState()
		{
			InventoryConfig config = TestAssetFactory.CreateInventoryConfig(30f);
			ClothingItemDefinition clothing = CreateClothing();
			try
			{
				InventoryService inventory = new(config);
				using PlayerEquipmentService equipment = new(inventory);
				equipment.RestoreEquipment(new[] { clothing });
				Assert.IsEmpty(equipment.EquippedItems);
				inventory.AddItem(clothing, 1);
				equipment.RestoreEquipment(new[] { clothing, clothing });
				Assert.AreEqual(1, equipment.EquippedItems.Count);
				Assert.AreEqual(5f, equipment.WarmthBonus);
				equipment.RestoreEquipment(null);
				Assert.IsEmpty(equipment.EquippedItems);
				Assert.AreEqual(0f, equipment.WarmthBonus);
			}
			finally
			{
				Object.DestroyImmediate(clothing);
				Object.DestroyImmediate(config);
			}
		}

		[Test]
		public void LoadCheckpoint_RestoresInventoryBeforeEquipmentAndAcceptsLegacySave()
		{
			string directory = Path.Combine(Path.GetTempPath(), "tln_equipment_" + Guid.NewGuid().ToString("N"));
			InventoryConfig config = TestAssetFactory.CreateInventoryConfig(30f);
			ClothingItemDefinition clothing = CreateClothing();
			ItemCatalog catalog = ScriptableObject.CreateInstance<ItemCatalog>();
			catalog.EditorSetItems(new ItemDefinition[] { clothing });
			try
			{
				JsonSaveRepository repository = new(directory);
				GameSaveData data = new() { slotId = 1 };
				data.inventory.items.Add(new() { itemId = clothing.Id, amount = 1 });
				data.equippedItemIds.Add(clothing.Id);
				repository.Save(data);
				SaveSessionService session = new(repository);
				InventoryService inventory = new(config);
				using PlayerEquipmentService equipment = new(inventory);
				GameSaveService saves = new(repository, session, inventory, null, null,
					new PlacementService(null), catalog, null, null, equipment);
				session.RequestLoadGame(1);
				Assert.IsTrue(saves.LoadActiveSlotIfRequested());
				Assert.IsTrue(equipment.IsEquipped(clothing));
				Assert.AreEqual(5f, equipment.WarmthBonus);

				data.equippedItemIds = null;
				repository.Save(data);
				session.RequestLoadGame(1);
				Assert.IsTrue(saves.LoadActiveSlotIfRequested());
				Assert.IsEmpty(equipment.EquippedItems);
			}
			finally
			{
				Object.DestroyImmediate(catalog);
				Object.DestroyImmediate(clothing);
				Object.DestroyImmediate(config);
				if (Directory.Exists(directory))
					Directory.Delete(directory, true);
			}
		}

		private static ClothingItemDefinition CreateClothing()
		{
			ClothingItemDefinition clothing = TestAssetFactory.CreateItem<ClothingItemDefinition>("coat");
			TestAssetFactory.SetPrivateField(clothing, "_slot", ClothingSlotId.OuterTorso);
			TestAssetFactory.SetPrivateField(clothing, "_warmthBonus", 5f);
			return clothing;
		}
	}
}
