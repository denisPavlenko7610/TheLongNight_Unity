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

			PlayerRoot player = _resolver.Instantiate(
				_playerPrefab,
				spawnPoint.position,
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
	}
}
