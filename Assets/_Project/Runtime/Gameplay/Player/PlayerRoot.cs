using TLN.Gameplay.Player.Input;
using TLN.Gameplay.Player.Look;
using TLN.Gameplay.Player.Movement;
using UnityEngine;

namespace TLN.Gameplay.Player
{
	public sealed class PlayerRoot : MonoBehaviour
	{
		[field: SerializeField] public PlayerInputReader InputReader { get; private set; }
		[field: SerializeField] public PlayerMotor Motor { get; private set; }
		[field: SerializeField] public PlayerLook Look { get; private set; }
		[field: SerializeField] public Camera Camera { get; private set; }

		private void Reset()
		{
			InputReader = GetComponent<PlayerInputReader>();
			Motor = GetComponent<PlayerMotor>();
			Look = GetComponent<PlayerLook>();
			Camera = GetComponentInChildren<Camera>();
		}

		private void OnValidate()
		{
			if (InputReader == null)
			{
				InputReader = GetComponent<PlayerInputReader>();
			}

			if (Motor == null)
			{
				Motor = GetComponent<PlayerMotor>();
			}

			if (Look == null)
			{
				Look = GetComponent<PlayerLook>();
			}

			if (Camera == null)
			{
				Camera = GetComponentInChildren<Camera>();
			}
		}
	}
}
