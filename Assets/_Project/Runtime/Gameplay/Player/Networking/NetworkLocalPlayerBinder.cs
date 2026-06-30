using TLN.Application.Multiplayer;
using TLN.Core.Logging;
using TLN.Gameplay.Placement;
using TLN.Gameplay.Wildlife;
using Unity.Netcode;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace TLN.Gameplay.Player.Networking
{
	public sealed class NetworkLocalPlayerBinder : MonoBehaviour
	{
		private IMultiplayerSessionService _multiplayerSessionService;
		private IObjectResolver _resolver;
		private PlacementService _placementService;
		private WildlifeTargetService _wildlifeTargetService;

		private PlayerRoot _boundPlayer;

		[Inject]
		public void Construct(
			IMultiplayerSessionService multiplayerSessionService,
			IObjectResolver resolver,
			PlacementService placementService,
			WildlifeTargetService wildlifeTargetService
		)
		{
			_multiplayerSessionService = multiplayerSessionService;
			_resolver = resolver;
			_placementService = placementService;
			_wildlifeTargetService = wildlifeTargetService;
		}

		private void Update()
		{
			if (_boundPlayer != null)
			{
				return;
			}

			if (_multiplayerSessionService is not { IsMultiplayer: true })
			{
				return;
			}

			if (_multiplayerSessionService.IsServer)
			{
				return;
			}

			NetworkManager networkManager = NetworkManager.Singleton;
			if (networkManager == null || !networkManager.IsClient)
			{
				return;
			}

			NetworkClient localClient = networkManager.LocalClient;
			if (localClient == null || localClient.PlayerObject == null)
			{
				return;
			}

			BindLocalPlayer(localClient.PlayerObject);
		}

		private void BindLocalPlayer(NetworkObject playerObject)
		{
			if (!playerObject.TryGetComponent(out PlayerRoot playerRoot))
			{
				TLNLogger.LogError("Local network player object must have PlayerRoot.", playerObject);
				return;
			}

			_resolver.InjectGameObject(playerRoot.gameObject);

			_placementService.SetPlayerRoot(playerRoot);
			_wildlifeTargetService.SetPlayerRoot(playerRoot);

			_boundPlayer = playerRoot;
			enabled = false;
		}
	}
}
