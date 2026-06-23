using UnityEngine;

namespace TLN.Gameplay.Interaction
{
	public sealed class PlayerInteractionRaycaster
	{
		private readonly Camera _camera;
		private readonly float _maxDistance;
		private readonly LayerMask _layerMask;

		public PlayerInteractionRaycaster(Camera camera, float maxDistance, LayerMask layerMask)
		{
			_camera = camera;
			_maxDistance = maxDistance;
			_layerMask = layerMask;
		}

		public bool TryRaycast(out InteractionHit interactionHit)
		{
			Ray ray = new Ray(_camera.transform.position, _camera.transform.forward);

			if (Physics.Raycast(ray, out RaycastHit hit, _maxDistance, _layerMask, QueryTriggerInteraction.Ignore))
			{
				IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();

				if (interactable != null)
				{
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

			interactionHit = default;
			return false;
		}
	}
}