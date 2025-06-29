using UnityEngine;

namespace TheLongNight.Items
{
    [CreateAssetMenu(fileName = "NewInteractableItem", menuName = "ScriptableObjects/InteractableData")]
    public class InteractableItemData : ScriptableObject
    {
        public ItemType itemType;
        public Sprite icon;
        public float calories;
        public float weight;
        public string itemName;
        [TextArea(3, 6)] public string itemDescription;
    }
}