using TLN.Application.App;
using TLN.Application.GameStates;
using TLN.Application.Input;
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
		[field: SerializeField] public PlayerPauseController PauseController { get; private set; }

		public void Construct(IInputModeService inputModeService, IGameStateMachine gameStateMachine)
		{
			Motor.Construct(inputModeService);
			Look.Construct(inputModeService);
			PauseController.Construct(gameStateMachine);
		}

		private void Reset()
		{
			InputReader = GetComponent<PlayerInputReader>();
			Motor = GetComponent<PlayerMotor>();
			Look = GetComponent<PlayerLook>();
			Camera = GetComponentInChildren<Camera>();
			PauseController = GetComponent<PlayerPauseController>();
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

			if (PauseController == null)
			{
				PauseController = GetComponent<PlayerPauseController>();
			}
		}
	}
}
