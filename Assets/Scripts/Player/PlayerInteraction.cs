using System;
using TheLongNight.Items;
using UnityEngine;

namespace TheLongNight.Player
{
    public class PlayerInteraction : MonoBehaviour
    {
        [SerializeField] private Camera _playerCamera;
        [SerializeField] private float _maxDistance = 3f;
        [SerializeField] private LayerMask _interactionMask = ~0;

        public event Action<PickableItem> OnHoverEnter;
        public event Action<PickableItem> OnHoverExit;

        private readonly RaycastHit[] _hitBuffer = new RaycastHit[1];
        private PickableItem _currentItem;

        private void Update()
        {
            UpdateHoveredItem();
            HandlePickupInput();
        }

        private void UpdateHoveredItem()
        {
            Ray ray = _playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            int hitCount = Physics.RaycastNonAlloc(ray, _hitBuffer, _maxDistance, _interactionMask.value);

            PickableItem hitItem = (hitCount > 0) ? _hitBuffer[0].collider.GetComponentInParent<PickableItem>() : null;

            if (_currentItem != null && hitItem == _currentItem)
                return;
            
            _currentItem = hitItem;
            
            if (_currentItem == null)
                OnHoverExit?.Invoke(_currentItem);
            else
                OnHoverEnter?.Invoke(hitItem);
        }

        private void HandlePickupInput()
        {
            if (_currentItem != null && Input.GetMouseButtonDown(0))
            {
                _currentItem.PickUp();
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Ray ray = _playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            Gizmos.DrawLine(ray.origin, ray.origin + ray.direction * _maxDistance);
        }
#endif
    }
}