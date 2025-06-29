using UnityEngine;

namespace TheLongNight.Items
{
    [CreateAssetMenu(fileName = "NewPickableItem", menuName = "ScriptableObjects/PickableData")]
    public class PickableItemData : ScriptableObject
    {
        public ItemType ItemType;
        public Sprite Icon;
        public float Calories;
        public float Weight;
        public string ItemName;
        [TextArea(3, 6)] public string ItemDescription;
        public GameObject Prefab;
    }
}