using UnityEngine;
using UnityEngine.InputSystem;

namespace TLN.Gameplay.Player.Input
{
    public sealed class PlayerInputReader : MonoBehaviour, IPlayerInputReader
    {
        private TLN.Input.TLNInputActions _inputActions;

        public Vector2 Move { get; private set; }
        public Vector2 Look { get; private set; }

        public bool IsSprintHeld { get; private set; }
        public bool WasInteractPressedThisFrame { get; private set; }
        public bool WasPausePressedThisFrame { get; private set; }

        private void Awake()
        {
            _inputActions = new TLN.Input.TLNInputActions();
        }

        private void OnEnable()
        {
            _inputActions.Enable();

            _inputActions.Player.Move.performed += OnMovePerformed;
            _inputActions.Player.Move.canceled += OnMoveCanceled;

            _inputActions.Player.Look.performed += OnLookPerformed;
            _inputActions.Player.Look.canceled += OnLookCanceled;

            _inputActions.Player.Sprint.performed += OnSprintPerformed;
            _inputActions.Player.Sprint.canceled += OnSprintCanceled;

            _inputActions.Player.Interact.performed += OnInteractPerformed;
            _inputActions.Player.Pause.performed += OnPausePerformed;
        }

        private void OnDisable()
        {
            _inputActions.Player.Move.performed -= OnMovePerformed;
            _inputActions.Player.Move.canceled -= OnMoveCanceled;

            _inputActions.Player.Look.performed -= OnLookPerformed;
            _inputActions.Player.Look.canceled -= OnLookCanceled;

            _inputActions.Player.Sprint.performed -= OnSprintPerformed;
            _inputActions.Player.Sprint.canceled -= OnSprintCanceled;

            _inputActions.Player.Interact.performed -= OnInteractPerformed;
            _inputActions.Player.Pause.performed -= OnPausePerformed;

            _inputActions.Disable();
        }

        private void LateUpdate()
        {
            WasInteractPressedThisFrame = false;
            WasPausePressedThisFrame = false;
        }

        private void OnDestroy()
        {
            _inputActions.Dispose();
        }

        private void OnMovePerformed(InputAction.CallbackContext context)
        {
            Move = context.ReadValue<Vector2>();
        }

        private void OnMoveCanceled(InputAction.CallbackContext context)
        {
            Move = Vector2.zero;
        }

        private void OnLookPerformed(InputAction.CallbackContext context)
        {
            Look = context.ReadValue<Vector2>();
        }

        private void OnLookCanceled(InputAction.CallbackContext context)
        {
            Look = Vector2.zero;
        }

        private void OnSprintPerformed(InputAction.CallbackContext context)
        {
            IsSprintHeld = true;
        }

        private void OnSprintCanceled(InputAction.CallbackContext context)
        {
            IsSprintHeld = false;
        }

        private void OnInteractPerformed(InputAction.CallbackContext context)
        {
            WasInteractPressedThisFrame = true;
        }

        private void OnPausePerformed(InputAction.CallbackContext context)
        {
            WasPausePressedThisFrame = true;
        }
    }
}
