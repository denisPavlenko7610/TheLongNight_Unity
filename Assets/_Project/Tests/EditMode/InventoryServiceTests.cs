using NUnit.Framework;
using TLN.Gameplay.Inventory;
using TLN.Gameplay.Items;

namespace TLN.Tests.EditMode
{
	public sealed class InventoryServiceTests
	{
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
