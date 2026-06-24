using TLN.Application.Input;
using TLN.Gameplay.Player.Input;
using UnityEngine;
using VContainer;

namespace TLN.Gameplay.Player.Look
{
	[RequireComponent(typeof(PlayerInputReader))]
	public sealed class PlayerLook : MonoBehaviour
	{
		[Header("References")]
		[SerializeField] private PlayerInputReader _inputReader;
		[SerializeField] private Transform _cameraRoot;

		[Header("Look")]
		[SerializeField] private float _mouseSensitivity = 0.08f;
		[SerializeField] private float _gamepadSensitivity = 120f;
		[SerializeField] private float _minPitch = -80f;
		[SerializeField] private float _maxPitch = 80f;

		private float _pitch;
		private IInputModeService _inputModeService;

		[Inject]
		public void Construct(IInputModeService inputModeService)
		{
			_inputModeService = inputModeService;
		}

		private void Awake()
		{
			if (_inputReader == null)
			{
				_inputReader = GetComponent<PlayerInputReader>();
			}
		}

		private void Update()
		{
			if (_inputModeService != null && _inputModeService.CanUseLookInput)
			{
				Look(UnityEngine.Time.deltaTime);
			}
		}

		private void Look(float deltaTime)
		{
			Vector2 lookInput = _inputReader.Look;
			if (lookInput.sqrMagnitude <= Mathf.Epsilon)
			{
				return;
			}

			float sensitivity = _inputReader.IsLookFromPointer
				? _mouseSensitivity
				: _gamepadSensitivity * deltaTime;

			float yaw = lookInput.x * sensitivity;
			float pitchDelta = lookInput.y * sensitivity;

			transform.Rotate(0f, yaw, 0f, Space.Self);

			_pitch += pitchDelta;
			_pitch = Mathf.Clamp(_pitch, _minPitch, _maxPitch);

			_cameraRoot.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
		}
	}
}