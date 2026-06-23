using System;
using TLN.Gameplay.Player;
using TLN.Gameplay.World;
using UnityEngine;

namespace TLN.Gameplay.Placement
{
	public sealed class PlacementService
	{
		private PlayerRoot _playerRoot;
		public PlayerRoot PlayerRoot => _playerRoot;
		public event Action<GameObject> Placed;
		private readonly IWorldObjectFactory _worldObjectFactory;

		public PlacementService(IWorldObjectFactory worldObjectFactory)
		{
			_worldObjectFactory = worldObjectFactory;
		}

		public void SetPlayerRoot(PlayerRoot playerRoot)
		{
			_playerRoot = playerRoot;
		}

		public bool TryPlace(GameObject prefab, float distance, out GameObject instance)
		{
			instance = null;

			if (prefab == null)
			{
				return false;
			}

			if (_playerRoot == null || _playerRoot.Camera == null)
			{
				return false;
			}

			Transform cameraTransform = _playerRoot.Camera.transform;
			Vector3 placementForward = GetPlacementForward(cameraTransform);

			Vector3 forwardPoint = cameraTransform.position + placementForward * distance;

			if (!TryFindGroundPosition(forwardPoint, out Vector3 groundPosition))
			{
				return false;
			}

			Quaternion rotation = Quaternion.Euler(0f, cameraTransform.eulerAngles.y, 0f);

			instance = _worldObjectFactory.Create(prefab, groundPosition, rotation);
			Placed?.Invoke(instance);

			return true;
		}

		private Vector3 GetPlacementForward(Transform cameraTransform)
		{
			Vector3 forward = cameraTransform.forward;
			forward.y = 0f;

			if (forward.sqrMagnitude > 0.0001f)
			{
				return forward.normalized;
			}

			forward = _playerRoot.transform.forward;
			forward.y = 0f;

			return forward.sqrMagnitude > 0.0001f ? forward.normalized : Vector3.forward;
		}

		private static bool TryFindGroundPosition(Vector3 origin, out Vector3 groundPosition)
		{
			const float rayStartHeight = 5f;
			const float rayDistance = 12f;

			Vector3 rayOrigin = origin + Vector3.up * rayStartHeight;

			if (Physics.Raycast(
					rayOrigin,
					Vector3.down,
					out RaycastHit hit,
					rayDistance,
					Physics.DefaultRaycastLayers,
					QueryTriggerInteraction.Ignore
				))
			{
				groundPosition = hit.point;
				return true;
			}

			groundPosition = default;
			return false;
		}
	}
}
