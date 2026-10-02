using TLN.Application.Input;
using TLN.Gameplay.Inventory;
using TLN.Gameplay.Player.Input;
using TLN.Gameplay.Survival;
using UnityEngine;
using VContainer;

namespace TLN.Gameplay.Player.Movement
{
	[RequireComponent(typeof(CharacterController))]
	public sealed class PlayerMotor : MonoBehaviour
	{
		private const float GroundedSnapVelocity = -2f;

		[Header("References")]
		[SerializeField] private PlayerInputReader _inputReader;

		[Header("Movement")]
		[SerializeField] private float _walkSpeed = 3.5f;
		[SerializeField] private float _sprintSpeed = 5.5f;
		[SerializeField] private float _gravity = -20f;

		[Header("Carry Weight (The Long Dark style)")]
		[SerializeField]
		[Range(0f, 0.7f)]
		private float _maxWeightSlowdown = 0.4f;

		[Header("Endurance")]
		[SerializeField]
		[Range(0f, 100f)]
		private float _sprintBlockedFatigue = 95f;

		private CharacterController _characterController;
		private float _verticalVelocity;

		private IInputModeService _inputModeService;
		private IInventoryService _inventoryService;
		private ISurvivalService _survivalService;

		private void Awake()
		{
			_characterController = GetComponent<CharacterController>();

			if (_inputReader == null)
			{
				_inputReader = GetComponent<PlayerInputReader>();
			}
		}

		private void Update()
		{
			if (_inputModeService != null && _inputModeService.CanUseMovementInput)
			{
				Move(UnityEngine.Time.deltaTime);
			}
		}

		[Inject]
		public void Construct(
			IInputModeService inputModeService,
			IInventoryService inventoryService,
			ISurvivalService survivalService
		)
		{
			_inputModeService = inputModeService;
			_inventoryService = inventoryService;
			_survivalService = survivalService;
		}

		private void Move(float deltaTime)
		{
			Vector2 moveInput = _inputReader.Move;

			Vector3 localMoveDirection = new Vector3(moveInput.x, 0f, moveInput.y);

			localMoveDirection = Vector3.ClampMagnitude(localMoveDirection, 1f);

			Vector3 worldMoveDirection = transform.TransformDirection(localMoveDirection);

			float baseSpeed = _inputReader.IsSprintHeld && CanSprint()
				? _sprintSpeed
				: _walkSpeed;

			float speed = baseSpeed * GetCarryWeightFactor();

			Vector3 horizontalVelocity = worldMoveDirection * speed;

			ApplyGravity(deltaTime);

			Vector3 velocity = horizontalVelocity;
			velocity.y = _verticalVelocity;

			_characterController.Move(velocity * deltaTime);
		}

		/// <summary>
		/// A heavy backpack slows the player down — up to _maxWeightSlowdown at the carry limit.
		/// </summary>
		private float GetCarryWeightFactor()
		{
			if (_inventoryService == null)
			{
				return 1f;
			}

			float maxWeight = Mathf.Max(0.01f, _inventoryService.MaxCarryWeight);
			float ratio = Mathf.Clamp01(_inventoryService.CurrentWeight / maxWeight);

			return 1f - _maxWeightSlowdown * ratio;
		}

		private bool CanSprint()
		{
			return _survivalService == null ||
			       _survivalService.Fatigue.Value < _sprintBlockedFatigue;
		}

		private void ApplyGravity(float deltaTime)
		{
			if (_characterController.isGrounded && _verticalVelocity < 0f)
			{
				_verticalVelocity = GroundedSnapVelocity;
			}

			_verticalVelocity += _gravity * deltaTime;
		}
	}
}
