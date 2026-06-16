using TLN.Application.Input;
using TLN.Gameplay.Player.Input;
using UnityEngine;
using VContainer;

namespace TLN.Gameplay.Player.Movement
{
	[RequireComponent(typeof(CharacterController))]
	public sealed class PlayerMotor : MonoBehaviour
	{
		[Header("References")]
		[SerializeField] private PlayerInputReader _inputReader;

		[Header("Movement")]
		[SerializeField] private float _walkSpeed = 3.5f;
		[SerializeField] private float _sprintSpeed = 5.5f;
		[SerializeField] private float _gravity = -20f;

		private CharacterController _characterController;
		private float _verticalVelocity;

		private IInputModeService _inputModeService;

		private void Awake()
		{
			_characterController = GetComponent<CharacterController>();

			if (_inputReader == null) {
				_inputReader = GetComponent<PlayerInputReader>();
			}
		}

		private void Update()
		{
			if (_inputModeService != null && _inputModeService.CanUseMovementInput) {
				Move(UnityEngine.Time.deltaTime);
			}
		}

		[Inject]
		public void Construct(IInputModeService inputModeService)
		{
			_inputModeService = inputModeService;
		}

		private void Move(float deltaTime)
		{
			Vector2 moveInput = _inputReader.Move;

			Vector3 localMoveDirection = new Vector3(moveInput.x, 0f, moveInput.y);

			localMoveDirection = Vector3.ClampMagnitude(localMoveDirection, 1f);

			Vector3 worldMoveDirection = transform.TransformDirection(localMoveDirection);

			float speed = _inputReader.IsSprintHeld
				? _sprintSpeed
				: _walkSpeed;

			Vector3 horizontalVelocity = worldMoveDirection * speed;

			ApplyGravity(deltaTime);

			Vector3 velocity = horizontalVelocity;
			velocity.y = _verticalVelocity;

			_characterController.Move(velocity * deltaTime);
		}

		private void ApplyGravity(float deltaTime)
		{
			if (_characterController.isGrounded && _verticalVelocity < 0f) {
				_verticalVelocity = -2f;
			}

			_verticalVelocity += _gravity * deltaTime;
		}
	}
}
