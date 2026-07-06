using UnityEngine;

namespace TLN.Gameplay.Interaction
{
	public sealed class PlayerInteractionRaycaster
	{
		private readonly Camera _camera;
		private readonly float _maxDistance;
		private readonly LayerMask _layerMask;

		private Collider _lastHitCollider;
		private IInteractable _lastHitInteractable;

		public PlayerInteractionRaycaster(Camera camera, float maxDistance, LayerMask layerMask)
		{
			_camera = camera;
			_maxDistance = maxDistance;
			_layerMask = layerMask;
		}

		public bool TryRaycast(out InteractionHit interactionHit)
		{
			if (_camera == null)
			{
				interactionHit = default;
				return false;
			}

			Ray ray = new Ray(_camera.transform.position, _camera.transform.forward);

			if (Physics.Raycast(ray, out RaycastHit hit, _maxDistance, _layerMask, QueryTriggerInteraction.Ignore))
			{
				IInteractable interactable = hit.collider == _lastHitCollider
					? _lastHitInteractable
					: hit.collider.GetComponentInParent<IInteractable>();

				if (interactable != null)
				{
					_lastHitCollider = hit.collider;
					_lastHitInteractable = interactable;

					interactionHit = new InteractionHit(
						interactable,
						hit.collider,
						hit.point,
						hit.normal,
						hit.distance
					);
					return true;
				}
			}

			_lastHitCollider = null;
			_lastHitInteractable = null;
			interactionHit = default;
			return false;
		}
	}
}
