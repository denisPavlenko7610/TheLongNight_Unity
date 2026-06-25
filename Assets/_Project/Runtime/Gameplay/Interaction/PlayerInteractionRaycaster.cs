using UnityEngine;

namespace TLN.Gameplay.Interaction
{
	public sealed class PlayerInteractionRaycaster
	{
		private const int MissCooldownFrames = 8;

		private readonly Camera _camera;
		private readonly float _maxDistance;
		private readonly LayerMask _layerMask;

		private Collider _lastHitCollider;
		private IInteractable _lastHitInteractable;
		private int _missFrameCounter;

		public PlayerInteractionRaycaster(Camera camera, float maxDistance, LayerMask layerMask)
		{
			_camera = camera;
			_maxDistance = maxDistance;
			_layerMask = layerMask;
		}

		public bool TryRaycast(out InteractionHit interactionHit)
		{
			if (_missFrameCounter > 0)
			{
				_missFrameCounter--;

				if (_lastHitCollider == null)
				{
					interactionHit = default;
					return false;
				}

				if (_lastHitCollider != null)
				{
					Ray ray = new Ray(_camera.transform.position, _camera.transform.forward);
					if (Physics.Raycast(ray, out RaycastHit hit, _maxDistance, _layerMask, QueryTriggerInteraction.Ignore) &&
					    hit.collider == _lastHitCollider)
					{
						interactionHit = new InteractionHit(
							_lastHitInteractable,
							hit.collider,
							hit.point,
							hit.normal,
							hit.distance
						);
						return true;
					}
				}

				if (_missFrameCounter <= 0)
				{
					_lastHitCollider = null;
					_lastHitInteractable = null;
				}

				interactionHit = default;
				return false;
			}

			Ray ray2 = new Ray(_camera.transform.position, _camera.transform.forward);

			if (Physics.Raycast(ray2, out RaycastHit hit2, _maxDistance, _layerMask, QueryTriggerInteraction.Ignore))
			{
				IInteractable interactable = hit2.collider.GetComponentInParent<IInteractable>();

				if (interactable != null)
				{
					_lastHitCollider = hit2.collider;
					_lastHitInteractable = interactable;
					_missFrameCounter = 0;

					interactionHit = new InteractionHit(
						interactable,
						hit2.collider,
						hit2.point,
						hit2.normal,
						hit2.distance
					);
					return true;
				}
			}

			_missFrameCounter = MissCooldownFrames;
			_lastHitCollider = null;
			_lastHitInteractable = null;
			interactionHit = default;
			return false;
		}
	}
}