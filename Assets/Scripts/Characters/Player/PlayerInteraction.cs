using System;
using TheLongNight.Items;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TheLongNight
{
    public class PlayerInteraction : MonoBehaviour
    {
        [SerializeField] private float _maxDistance = 3f;
        [SerializeField] private LayerMask _interactionMask = ~0;
        [SerializeField] private Transform _objectViewTransform;
        [SerializeField] private InputActionAsset _inputActions;

        [SerializeField] private Camera _playerCamera;
        [SerializeField] private MonoBehaviour _playerMovementScript;

        public event Action<PickableItem> OnHoverEnter;
        public event Action<PickableItem> OnHoverExit;
        public event Action<PickableItem> OnClick;
        public event Action OnCanceled;

        private readonly RaycastHit[] _hitBuffer = new RaycastHit[1];
        private PickableItem _currentItem;

        private bool _isViewing = false;
        private bool _canFinalizePickup = false;

        private Vector3 _objectViewLastPosition;
        private Quaternion _objectViewLastRotation;
        private Transform _viewingTransform;

        private InputActionMap _actionMap;
        private InputAction _clickAction;
        private InputAction _cancelAction;
        private InputAction _lookAction;

        private void Awake()
        {
            if (_inputActions != null)
            {
                _actionMap = _inputActions.FindActionMap("Player");
                _clickAction = _actionMap.FindAction("Interact");
                _cancelAction = _actionMap.FindAction("Cancel");
                _lookAction = _actionMap.FindAction("Look");
            }

            _clickAction.Enable();
            _cancelAction.Enable();
            _lookAction.Enable();
        }

        private void OnDestroy()
        {
            _clickAction.Disable();
            _cancelAction.Disable();
            _lookAction.Disable();
        }
        
        private void Update()
        {
            if (!_isViewing)
                UpdateHoveredItem();

            HandlePickupInput();

            if (_isViewing)
                HandleDragRotate();
        }

        private void UpdateHoveredItem()
        {
            Ray ray = _playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            int hitCount = Physics.RaycastNonAlloc(ray, _hitBuffer, _maxDistance, _interactionMask.value);
            PickableItem hitItem = hitCount > 0 ? _hitBuffer[0].collider.GetComponentInParent<PickableItem>() : null;

            if (hitItem == _currentItem)
                return;

            if (_currentItem != null)
                OnHoverExit?.Invoke(_currentItem);

            _currentItem = hitItem;

            if (_currentItem != null)
                OnHoverEnter?.Invoke(_currentItem);
        }

        private void HandlePickupInput()
        {
            if (_clickAction.triggered)
            {
                OnClick?.Invoke(_currentItem);
                if (!_isViewing && _currentItem != null)
                {
                    StartView(_currentItem);
                }
                else if (_isViewing && !_canFinalizePickup)
                {
                    _canFinalizePickup = true;
                }
            }

            if (_isViewing && _cancelAction.triggered)
            {
                OnCanceled?.Invoke();
                EndView(returnBack: true);
            }

            if (_isViewing && _canFinalizePickup && !_clickAction.WasPressedThisFrame())
            {
                _viewingTransform.GetComponent<PickableItem>()?.PickUp();
                EndView(returnBack: false);
            }
        }

        private void StartView(PickableItem item)
        {
            _viewingTransform = item.transform;
            _objectViewLastPosition = _viewingTransform.position;
            _objectViewLastRotation = _viewingTransform.rotation;

            _viewingTransform.position = _objectViewTransform.position;
            _viewingTransform.rotation = _objectViewTransform.rotation;

            if (_playerMovementScript != null)
                _playerMovementScript.enabled = false;

            _isViewing = true;
            _canFinalizePickup = false;
        }

        private void HandleDragRotate()
        {
            if (_lookAction.enabled && !_canFinalizePickup)
            {
                Vector2 delta = _lookAction.ReadValue<Vector2>();
                _viewingTransform.Rotate(_playerCamera.transform.up, -delta.x * Time.deltaTime, Space.World);
                _viewingTransform.Rotate(_playerCamera.transform.right, delta.y * Time.deltaTime, Space.World);
            }
        }

        private void EndView(bool returnBack)
        {
            if (returnBack && _viewingTransform != null)
            {
                _viewingTransform.position = _objectViewLastPosition;
                _viewingTransform.rotation = _objectViewLastRotation;
            }

            _isViewing = false;
            _canFinalizePickup = false;
            _viewingTransform = null;

            if (_playerMovementScript != null)
                _playerMovementScript.enabled = true;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (_playerCamera == null) return;
            Gizmos.color = Color.red;
            Ray ray = _playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            Gizmos.DrawLine(ray.origin, ray.origin + ray.direction * _maxDistance);
        }
#endif
    }
}