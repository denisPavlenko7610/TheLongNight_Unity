using System;
using TheLongNight.Items;
using UnityEngine;

namespace TheLongNight.Services.Inventory
{
	[Serializable]
	public class InventoryItem
	{
		public ItemType ItemType;
		public int Amount = 1;

		[Range(0,100)] public float Condition = 100f;

		public InventoryItem(ItemType type, int amount)
		{
			ItemType = type;
			Amount = amount;
		}
	}
}
