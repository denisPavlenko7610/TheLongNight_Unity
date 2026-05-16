using UnityEngine;
using UnityEngine.InputSystem;

namespace TLN.Gameplay.Player.Input
{
    public sealed class PlayerInputReader : MonoBehaviour, IPlayerInputReader
    {
        private TLN.Input.TLNInputActions _inputActions;

        public Vector2 Move { get; private set; }
        public Vector2 Look { get; private set; }
        public bool IsLookFromPointer { get; private set; } = true;

        public bool IsSprintHeld { get; private set; }
        public bool WasInteractPressedThisFrame { get; private set; }
        public bool WasPausePressedThisFrame { get; private set; }
        public bool WasInventoryPressedThisFrame { get; private set; }
        public bool IsStatusHeld { get; private set; }

        private void Awake()
        {
            _inputActions = new TLN.Input.TLNInputActions();
        }

        private void OnEnable()
        {
            _inputActions.Enable();

            _inputActions.Gameplay.Move.performed += OnMovePerformed;
            _inputActions.Gameplay.Move.canceled += OnMoveCanceled;

            _inputActions.Gameplay.Look.performed += OnLookPerformed;
            _inputActions.Gameplay.Look.canceled += OnLookCanceled;

            _inputActions.Gameplay.Sprint.performed += OnSprintPerformed;
            _inputActions.Gameplay.Sprint.canceled += OnSprintCanceled;

            _inputActions.Gameplay.Interact.performed += OnInteractPerformed;
            _inputActions.Gameplay.Pause.performed += OnPausePerformed;
            _inputActions.Gameplay.Inventory.performed += OnInventoryPerformed;

            _inputActions.Gameplay.Status.performed += OnStatusPerformed;
            _inputActions.Gameplay.Status.canceled += OnStatusCanceled;
        }

        private void OnDisable()
        {
            _inputActions.Gameplay.Move.performed -= OnMovePerformed;
            _inputActions.Gameplay.Move.canceled -= OnMoveCanceled;

            _inputActions.Gameplay.Look.performed -= OnLookPerformed;
            _inputActions.Gameplay.Look.canceled -= OnLookCanceled;

            _inputActions.Gameplay.Sprint.performed -= OnSprintPerformed;
            _inputActions.Gameplay.Sprint.canceled -= OnSprintCanceled;

            _inputActions.Gameplay.Interact.performed -= OnInteractPerformed;
            _inputActions.Gameplay.Pause.performed -= OnPausePerformed;
            _inputActions.Gameplay.Inventory.performed -= OnInventoryPerformed;

            _inputActions.Gameplay.Status.performed -= OnStatusPerformed;
            _inputActions.Gameplay.Status.canceled -= OnStatusCanceled;

            _inputActions.Disable();
        }

        private void LateUpdate()
        {
            WasInteractPressedThisFrame = false;
            WasPausePressedThisFrame = false;
            WasInventoryPressedThisFrame = false;
        }

        private void OnDestroy()
        {
            _inputActions.Dispose();
        }

        public void ClearTransientInput()
        {
            Look = Vector2.zero;
            WasInteractPressedThisFrame = false;
            WasPausePressedThisFrame = false;
        }

        private void OnStatusPerformed(InputAction.CallbackContext context)
        {
            IsStatusHeld = true;
        }

        private void OnStatusCanceled(InputAction.CallbackContext context)
        {
            IsStatusHeld = false;
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
            IsLookFromPointer = context.control?.device is Pointer;
        }

        private void OnInventoryPerformed(InputAction.CallbackContext context)
        {
            WasInventoryPressedThisFrame = true;
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
