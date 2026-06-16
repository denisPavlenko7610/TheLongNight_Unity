using Assign;
using TLN.Core.Validation;
using TLN.Gameplay.Flashlight;
using TLN.Gameplay.Interaction;
using TLN.Gameplay.Player.Input;
using TLN.Gameplay.Player.Look;
using TLN.Gameplay.Player.Movement;
using UnityEngine;

namespace TLN.Gameplay.Player
{
	public sealed class PlayerRoot : MonoBehaviour
	{
		[field: SerializeField, Assign, Required] public PlayerInputReader InputReader { get; private set; }
		[field: SerializeField, Assign, Required] public PlayerMotor Motor { get; private set; }
		[field: SerializeField, Assign, Required] public PlayerLook Look { get; private set; }
		[field: SerializeField, Assign(Mode.Children), Required] public Camera Camera { get; private set; }
		[field: SerializeField, Assign, Required] public PlayerPauseController PauseController { get; private set; }
		[field: SerializeField, Assign, Required] public PlayerInteractionController InteractionController { get; private set; }
		[field: SerializeField, Assign, Required] public PlayerInventoryController InventoryController { get; private set; }
		[field: SerializeField, Assign, Required] public PlayerTimeOverlayController TimeOverlayController { get; private set; }
		[field: SerializeField, Assign, Required] public PlayerBuildController BuildController { get; private set; }
		[field: SerializeField, Assign, Required] public PlayerWarmthController WarmthController { get; private set; }

		[Header("Equipment")]
		[SerializeField] private FlashlightController _flashlight;

		public void ToggleFlashlight()
		{
			if (_flashlight != null)
			{
				_flashlight.Toggle();
			}
		}
	}
}
