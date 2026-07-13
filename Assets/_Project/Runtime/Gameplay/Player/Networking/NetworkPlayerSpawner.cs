using System.Collections.Generic;
using Assign;
using TLN.Core.Logging;
using TLN.Core.Validation;
using TLN.Gameplay.Placement;
using TLN.Gameplay.Wildlife;
using Unity.Netcode;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace TLN.Gameplay.Player.Networking
{
	public sealed class NetworkPlayerSpawner : MonoBehaviour
	{
		[SerializeField] [Required] private PlayerRoot _playerPrefab;
		[SerializeField] [Assign(Mode.Scene)] [Required] private Transform[] _spawnPoints;

		[Header("Grounding")]
		[SerializeField] private LayerMask _spawnGroundMask = ~0;
		[SerializeField] private float _spawnRayStartHeight = 20f;
		[SerializeField] private float _spawnRayDistance = 80f;
		[SerializeField] private float _spawnGroundOffset = 0.05f;

		private readonly HashSet<ulong> _spawnedClientIds = new();

		private IObjectResolver _resolver;
		private PlacementService _placementService;
		private WildlifeTargetService _wildlifeTargetService;

		[Inject]
		public void Construct(
			IObjectResolver resolver,
			PlacementService placementService,
			WildlifeTargetService wildlifeTargetService
		)
		{
			_resolver = resolver;
			_placementService = placementService;
			_wildlifeTargetService = wildlifeTargetService;
		}

		private void OnDestroy()
		{
			NetworkManager networkManager = NetworkManager.Singleton;
			if (networkManager == null)
			{
				return;
			}

			networkManager.OnClientConnectedCallback -= OnClientConnected;
		}

		public void StartServerSpawning()
		{
			NetworkManager networkManager = NetworkManager.Singleton;
			if (networkManager == null)
			{
				TLNLogger.LogError("Cannot start network player spawning because NetworkManager is missing.", this);
				return;
			}

			if (!networkManager.IsServer)
			{
				return;
			}

			networkManager.OnClientConnectedCallback -= OnClientConnected;
			networkManager.OnClientConnectedCallback += OnClientConnected;

			foreach (ulong clientId in networkManager.ConnectedClientsIds)
			{
				SpawnPlayerForClient(clientId);
			}
		}

		private void OnClientConnected(ulong clientId)
		{
			SpawnPlayerForClient(clientId);
		}

		private void SpawnPlayerForClient(ulong clientId)
		{
			if (_spawnedClientIds.Contains(clientId))
			{
				return;
			}

			NetworkManager networkManager = NetworkManager.Singleton;
			if (networkManager == null || !networkManager.IsServer)
			{
				return;
			}

			Transform spawnPoint = GetSpawnPoint(clientId);
			if (spawnPoint == null)
			{
				return;
			}

			Vector3 spawnPosition = ResolveSpawnPosition(spawnPoint);

			PlayerRoot player = _resolver.Instantiate(
				_playerPrefab,
				spawnPosition,
				spawnPoint.rotation
			);

			NetworkObject networkObject = player.GetComponent<NetworkObject>();
			if (networkObject == null)
			{
				TLNLogger.LogError("Network player prefab must have NetworkObject.", player);
				Destroy(player.gameObject);
				return;
			}

			networkObject.SpawnAsPlayerObject(clientId, true);
			_spawnedClientIds.Add(clientId);
			_wildlifeTargetService.RegisterPlayer(player);

			if (clientId == networkManager.LocalClientId)
			{
				_placementService.SetPlayerRoot(player);
				_wildlifeTargetService.SetPlayerRoot(player);
			}
		}

		private Transform GetSpawnPoint(ulong clientId)
		{
			if (_spawnPoints == null || _spawnPoints.Length == 0)
			{
				TLNLogger.LogError("At least one network player spawn point is required.", this);
				return null;
			}

			int index = (int)(clientId % (ulong)_spawnPoints.Length);
			Transform spawnPoint = _spawnPoints[index];

			if (spawnPoint == null)
			{
				TLNLogger.LogError($"Network player spawn point at index {index} is missing.", this);
				return null;
			}

			return spawnPoint;
		}

		private Vector3 ResolveSpawnPosition(Transform spawnPoint)
		{
			Vector3 fallbackPosition = spawnPoint.position;

			if (!TryProjectSpawnPositionToGround(fallbackPosition, out Vector3 groundPosition))
			{
				TLNLogger.LogWarning(
					$"Network player spawn point '{spawnPoint.name}' has no ground below it. Using transform position.",
					spawnPoint
				);

				return fallbackPosition;
			}

			return groundPosition + Vector3.up * GetCharacterControllerGroundOffset();
		}

		private bool TryProjectSpawnPositionToGround(
			Vector3 spawnPosition,
			out Vector3 groundPosition
		)
		{
			Vector3 rayOrigin = spawnPosition + Vector3.up * _spawnRayStartHeight;
			float rayDistance = _spawnRayStartHeight + _spawnRayDistance;

			if (Physics.Raycast(
				    rayOrigin,
				    Vector3.down,
				    out RaycastHit hit,
				    rayDistance,
				    _spawnGroundMask,
				    QueryTriggerInteraction.Ignore))
			{
				groundPosition = hit.point;
				return true;
			}

			groundPosition = default;
			return false;
		}

		private float GetCharacterControllerGroundOffset()
		{
			if (_playerPrefab != null &&
			    _playerPrefab.TryGetComponent(out CharacterController characterController))
			{
				float bottomOffset =
					characterController.center.y - characterController.height * 0.5f;

				return -bottomOffset +
				       characterController.skinWidth +
				       Mathf.Max(0f, _spawnGroundOffset);
			}

			return Mathf.Max(0f, _spawnGroundOffset);
		}
	}
}
