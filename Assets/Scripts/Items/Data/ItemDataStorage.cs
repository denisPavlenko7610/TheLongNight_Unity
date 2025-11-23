using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TheLongNight.Items
{
    [CreateAssetMenu(fileName = "ItemDataStorage", menuName = "ScriptableObjects/ItemDataStorage")]
    public class ItemDataStorage : ScriptableObject
    {
        [SerializeField] private List<PickableItemData> _itemDataList;

        private Dictionary<ItemType, PickableItemData> _itemDataDictionary;

		private void OnEnable()
		{
			BuildDictionary();
		}

		private void OnValidate()
        {
            if (_itemDataDictionary != null)
                return;
            
			BuildDictionary();
        }
		
        public PickableItemData GetItemData(ItemType itemType)
		{
			if (_itemDataDictionary == null || _itemDataDictionary.Count == 0)
				BuildDictionary();

			_itemDataDictionary.TryGetValue(itemType, out PickableItemData data);
			return data;
		}

		private void BuildDictionary()
		{
			_itemDataDictionary = new Dictionary<ItemType, PickableItemData>();
			foreach (PickableItemData data in _itemDataList.Where(data => data != null)) {
				_itemDataDictionary[data.ItemType] = data;
			}
		}
    }
}