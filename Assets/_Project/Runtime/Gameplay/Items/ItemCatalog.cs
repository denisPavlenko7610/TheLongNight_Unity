using System.Collections.Generic;
using TLN.Core.Logging;
using UnityEngine;

namespace TLN.Gameplay.Items
{
    [CreateAssetMenu(fileName = "ItemCatalog", menuName = "TLN/Items/Item Catalog")]
    public sealed class ItemCatalog : ScriptableObject
    {
        [SerializeField] private ItemDefinition[] _items;

        private Dictionary<string, ItemDefinition> _itemsById;

        public IReadOnlyList<ItemDefinition> Items => _items;

        public bool TryGetItem(string id, out ItemDefinition item)
        {
            EnsureCache();

            if (string.IsNullOrWhiteSpace(id))
            {
                item = null;
                return false;
            }

            return _itemsById.TryGetValue(id, out item);
        }

        private void EnsureCache()
        {
            if (_itemsById != null)
            {
                return;
            }

            _itemsById = new Dictionary<string, ItemDefinition>();

            if (_items == null)
            {
                return;
            }

            for (int i = 0; i < _items.Length; i++)
            {
                ItemDefinition item = _items[i];

                if (item == null)
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(item.Id))
                {
                    TLNLogger.LogWarning("ItemCatalog contains item with empty id.", item);

                    continue;
                }

                if (!_itemsById.TryAdd(item.Id, item))
                {
                    TLNLogger.LogWarning($"Duplicate item id in ItemCatalog: {item.Id}", item);
                }
            }
        }

#if UNITY_EDITOR
        public void EditorSetItems(ItemDefinition[] items)
        {
            _items = items;
            _itemsById = null;
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
