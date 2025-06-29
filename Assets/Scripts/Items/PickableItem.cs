#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace TheLongNight.Items
{
    [ExecuteAlways]
    public class PickableItem : MonoBehaviour
    {
        [SerializeField] private ItemType _itemType;
        [SerializeField] private ItemDataStorage _itemDataStorage;

        private ItemType _currentItemType = ItemType.None;
        private GameObject _currentPrefab;
        private bool _swapScheduled;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (Application.isPlaying || _itemDataStorage == null)
                return;

            if (_itemType == _currentItemType)
                return;

            if (PrefabUtility.IsPartOfPrefabAsset(gameObject))
                return;

            if (!_swapScheduled)
            {
                _swapScheduled = true;
                var thisRef = this;
                EditorApplication.delayCall += () =>
                {
                    if (thisRef != null && thisRef.gameObject != null)
                        thisRef.SwapPrefab();
                };
            }

            _currentItemType = _itemType;
        }

        private void SwapPrefab()
        {
            _swapScheduled = false;

            if (PrefabUtility.IsPartOfPrefabAsset(gameObject))
                return;

            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i);
                if (child != null)
                    DestroyImmediate(child.gameObject);
            }

            GameObject newPrefab = _itemDataStorage.GetItemData(_itemType).Prefab;
            if (newPrefab != null)
            {
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(newPrefab);
                instance.transform.SetParent(transform, false);
                _currentPrefab = instance;
                _currentPrefab.name = newPrefab.name;
            }
        }
#endif

        public PickableItemData GetData() => _itemDataStorage?.GetItemData(_itemType);

        public void PickUp()
        {
            Destroy(gameObject);
        }
    }
}
