using TLN.Gameplay.Player.Input;
using UnityEngine;

namespace TLN.Gameplay.Player.Animation
{
	public sealed class PlayerVisualAnimator : MonoBehaviour
	{
		[SerializeField] private PlayerInputReader _inputReader;
		[SerializeField] private Animator _animator;
		[SerializeField] private float _movementThreshold = 0.05f;
		[SerializeField] private float _speedDampTime = 0.08f;

		private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");
		private static readonly int IsSprintingHash = Animator.StringToHash("IsSprinting");
		private static readonly int MovementSpeedHash = Animator.StringToHash("MovementSpeed");

		private void Awake()
		{
			if (_inputReader == null)
			{
				_inputReader = GetComponent<PlayerInputReader>();
			}

			if (_animator == null)
			{
				_animator = GetComponentInChildren<Animator>();
			}

			if (_animator != null)
			{
				_animator.applyRootMotion = false;
			}
		}

		private void Update()
		{
			if (_inputReader == null || _animator == null)
			{
				return;
			}

			float movementAmount = Mathf.Clamp01(_inputReader.Move.magnitude);
			bool isMoving = movementAmount > _movementThreshold;
			bool isSprinting = isMoving && _inputReader.IsSprintHeld;

			_animator.SetBool(IsMovingHash, isMoving);
			_animator.SetBool(IsSprintingHash, isSprinting);
			_animator.SetFloat(MovementSpeedHash, isSprinting ? 1f : movementAmount, _speedDampTime, UnityEngine.Time.deltaTime);
		}
	}
}
