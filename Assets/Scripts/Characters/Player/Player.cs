using UnityEngine;

namespace TheLongNight
{
    public class Player : MonoBehaviour
    {
		[SerializeField] PlayerInteraction _playerInteraction;

		public PlayerInteraction PlayerInteraction => _playerInteraction;

	}
}
