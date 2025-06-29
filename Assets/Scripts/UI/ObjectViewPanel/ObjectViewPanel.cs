using TMPro;
using UnityEngine;

namespace TheLongNight.UI
{
    public class ObjectViewPanel : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _objectNameText;
        [SerializeField] private TextMeshProUGUI _objectDescriptionText;
        [SerializeField] private TextMeshProUGUI _objectConditionText;
        [SerializeField] private TextMeshProUGUI _objectWeightText;

        public void changeVisibility(bool isVisible) => gameObject.SetActive(isVisible);
    }
}