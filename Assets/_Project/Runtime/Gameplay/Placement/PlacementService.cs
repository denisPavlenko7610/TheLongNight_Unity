using System;
using TLN.Gameplay.Player;
using UnityEngine;

namespace TLN.Gameplay.Placement
{
	public sealed class PlacementService
	{
		private readonly PlayerRoot _playerRoot;

		public event Action<GameObject> Placed;

		public PlacementService(PlayerRoot playerRoot)
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

			Vector3 position = cameraTransform.position + cameraTransform.forward * distance;
			position.y = _playerRoot.transform.position.y + 0.05f;

			Quaternion rotation = Quaternion.Euler(
				0f,
				cameraTransform.eulerAngles.y,
				0f);

			instance = UnityEngine.Object.Instantiate(prefab, position, rotation);

			Placed?.Invoke(instance);

			return true;
		}
	}
}
