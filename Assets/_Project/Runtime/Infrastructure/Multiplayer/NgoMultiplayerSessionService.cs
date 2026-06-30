using System;
using System.Threading.Tasks;
using TLN.Application.Multiplayer;
using TLN.Core.Results;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using Unity.Netcode.Transports.UTP;

namespace TLN.Infrastructure.Multiplayer
{
	public sealed class NgoMultiplayerSessionService : IMultiplayerSessionService
	{
		private const int MaxPlayers = 4;

		private readonly NetworkManager _networkManager;
		private readonly UnityTransport _transport;

		private ISession _session;

		public bool IsMultiplayer =>
			_networkManager != null &&
			(_networkManager.IsServer || _networkManager.IsClient);

		public bool IsServer => _networkManager != null && _networkManager.IsServer;
		public bool IsClient => _networkManager != null && _networkManager.IsClient;
		public bool IsHost => _networkManager != null && _networkManager.IsHost;

		public string JoinCode { get; private set; } = string.Empty;

		public NgoMultiplayerSessionService(NetworkManager networkManager)
		{
			_networkManager = networkManager ?? throw new ArgumentNullException(nameof(networkManager));

			_transport = _networkManager.GetComponent<UnityTransport>();
			if (_transport == null)
			{
				throw new InvalidOperationException("UnityTransport is required on the same GameObject as NetworkManager.");
			}
		}

		public async Task<OperationResult<string>> CreateHostSession()
		{
			if (IsMultiplayer)
			{
				return OperationResult<string>.Failure("Multiplayer session is already running.");
			}

			try
			{
				await EnsureUnityServicesReady();

				SessionOptions options = new SessionOptions
				{
					MaxPlayers = MaxPlayers
				}.WithRelayNetwork();

				_session = await MultiplayerService.Instance.CreateSessionAsync(options);

				JoinCode = _session.Code;

				return OperationResult<string>.Success(JoinCode);
			}
			catch (Exception exception)
			{
				Shutdown();
				return OperationResult<string>.Failure($"Failed to create multiplayer session. {exception.Message}");
			}
		}

		public async Task<OperationResult> JoinSessionByCode(string joinCode)
		{
			if (IsMultiplayer)
			{
				return OperationResult.Failure("Multiplayer session is already running.");
			}

			if (string.IsNullOrWhiteSpace(joinCode))
			{
				return OperationResult.Failure("Join code is empty.");
			}

			try
			{
				await EnsureUnityServicesReady();

				_session = await MultiplayerService.Instance.JoinSessionByCodeAsync(joinCode.Trim());

				JoinCode = joinCode.Trim();

				return OperationResult.Success();
			}
			catch (Exception exception)
			{
				Shutdown();
				return OperationResult.Failure($"Failed to join multiplayer session. {exception.Message}");
			}
		}

		public void Shutdown()
		{
			JoinCode = string.Empty;

			if (_networkManager != null && _networkManager.IsListening)
			{
				_networkManager.Shutdown();
			}

			if (_session != null)
			{
				_ = _session.LeaveAsync();
				_session = null;
			}
		}

		private static async Task EnsureUnityServicesReady()
		{
			if (UnityServices.State == ServicesInitializationState.Uninitialized)
			{
				await UnityServices.InitializeAsync();
			}

			if (!AuthenticationService.Instance.IsSignedIn)
			{
				await AuthenticationService.Instance.SignInAnonymouslyAsync();
			}
		}
	}
}
