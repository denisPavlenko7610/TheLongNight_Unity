using UnityEngine;

namespace TheLongNight
{
    public class CoreLibUsage : MonoBehaviour
    {
        void Start()
        {
            string time = CoreLibWrapper.GetTimeString();
            Debug.Log("Current device time: " + time);
        }
    }
}