using System.Collections.Generic;
using UnityEngine;

namespace TheLongNight.Items
{
    [CreateAssetMenu(fileName = "ItemDataStorage", menuName = "ScriptableObjects/ItemDataStorage")]
    public class ItemDataStorage : ScriptableObject
    {
        [SerializeField] private List<PickableItemData> _itemDataList;

        private Dictionary<ItemType, PickableItemData> _itemDataDictionary;

        private void OnValidate()
        {
            if (_itemDataDictionary != null)
                return;
            
            _itemDataDictionary = new Dictionary<ItemType, PickableItemData>();
            foreach (var data in _itemDataList)
            {
                _itemDataDictionary.TryAdd(data.ItemType, data);
            }
        }

        public PickableItemData GetItemData(ItemType itemType) => _itemDataDictionary.GetValueOrDefault(itemType);
    }
}