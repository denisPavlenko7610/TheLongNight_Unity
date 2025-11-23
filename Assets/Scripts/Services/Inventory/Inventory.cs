namespace TheLongNight.Services.Inventory
{
	using System;
	using System.Collections.Generic;
	using UnityEngine;
	using Items;

	public class Inventory : MonoBehaviour
	{
		public ItemDataStorage DataStorage;
		public List<InventoryItem> Items = new();

		public event Action OnChanged;

		public void Add(ItemType type, int amount = 1)
		{
			if (type == ItemType.None || amount <= 0) 
				return;

			InventoryItem existing = Items.Find(i => i.ItemType == type);
			if (existing != null)
				existing.Amount += amount;
			else
				Items.Add(new InventoryItem(type, amount));

			OnChanged?.Invoke();
		}

		public void Remove(ItemType type, int amount = 1)
		{
			InventoryItem existing = Items.Find(i => i.ItemType == type);
			if (existing == null)
				return;

			existing.Amount -= amount;
			if (existing.Amount <= 0)
				Items.Remove(existing);

			OnChanged?.Invoke();
		}

		public float GetTotalWeight()
		{
			float total = 0;

			foreach (InventoryItem it in Items)
			{
				PickableItemData data = DataStorage.GetItemData(it.ItemType);
				if (data == null)
					continue;

				total += data.Weight * it.Amount;
			}

			return total;
		}
	}
}
