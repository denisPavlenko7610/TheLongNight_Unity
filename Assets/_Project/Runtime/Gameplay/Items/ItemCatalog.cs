using System.Collections.Generic;
using UnityEngine;

namespace TLN.Gameplay.Items
{
	[CreateAssetMenu(fileName = "ItemCatalog", menuName = "TLN/Items/Item Catalog")]
	public sealed class ItemCatalog : ScriptableObject
	{
		[SerializeField] private ItemDefinition[] _items;

		public IReadOnlyList<ItemDefinition> Items => _items ?? System.Array.Empty<ItemDefinition>();

		public bool TryGetItem(string id, out ItemDefinition item)
		{
			if (string.IsNullOrWhiteSpace(id))
			{
				item = null;
				return false;
			}

			IReadOnlyList<ItemDefinition> items = Items;

			for (int i = 0; i < items.Count; i++)
			{
				ItemDefinition currentItem = items[i];

				if (currentItem == null)
				{
					continue;
				}

				if (string.Equals(currentItem.Id, id, System.StringComparison.Ordinal))
				{
					item = currentItem;
					return true;
				}
			}

			item = null;
			return false;
		}

		#if UNITY_EDITOR
		public void EditorSetItems(ItemDefinition[] items)
		{
			_items = items == null ? System.Array.Empty<ItemDefinition>() : (ItemDefinition[])items.Clone();
			UnityEditor.EditorUtility.SetDirty(this);
		}
		#endif
	}
}
