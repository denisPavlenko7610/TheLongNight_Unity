using System;
using TLN.Application.Multiplayer;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

namespace TLN.Infrastructure.Multiplayer
{
	public sealed class NgoMultiplayerSessionService : IMultiplayerSessionService
	{
		private readonly NetworkManager _networkManager;
		private readonly UnityTransport _transport;

		public bool IsMultiplayer =>
			_networkManager != null &&
			(_networkManager.IsServer || _networkManager.IsClient);

		public bool IsServer => _networkManager != null && _networkManager.IsServer;
		public bool IsClient => _networkManager != null && _networkManager.IsClient;
		public bool IsHost => _networkManager != null && _networkManager.IsHost;

		public NgoMultiplayerSessionService(NetworkManager networkManager)
		{
			_networkManager = networkManager ?? throw new ArgumentNullException(nameof(networkManager));

			_transport = _networkManager.GetComponent<UnityTransport>();
			if (_transport == null)
			{
				throw new InvalidOperationException("UnityTransport is required on the same GameObject as NetworkManager.");
			}
		}

		public bool StartHost()
		{
			return _networkManager.StartHost();
		}

		public bool StartClient(string address)
		{
			_transport.SetConnectionData(address, 7777);
			return _networkManager.StartClient();
		}

		public void Shutdown()
		{
			if (_networkManager == null)
			{
				return;
			}

			if (!_networkManager.IsListening)
			{
				return;
			}

			_networkManager.Shutdown();
		}
	}
}
