using System;
using System.Collections.Generic;
using TLN.Application.Multiplayer;
using TLN.Core.Results;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;

namespace TLN.Infrastructure.Multiplayer
{
	public sealed class NgoMultiplayerSessionService : IMultiplayerSessionService
	{
		private const int MaxPlayers = 4;
		private const int BrowseSessionCount = 20;
		private const int NetworkShutdownWaitFrames = 120;
		private const string SessionType = "the-long-night";
		private const string CompatibilityPropertyKey = "tln-session";
		private const string CompatibilityPropertyValue = "the-long-night-v1";

		private readonly NetworkManager _networkManager;

		private ISession _session;
		private bool _isSessionOperationInProgress;

		public bool IsMultiplayer =>
			_session != null ||
			_networkManager != null &&
			(_networkManager.IsServer || _networkManager.IsClient);

		public bool IsServer => _networkManager != null && _networkManager.IsServer;

		public NgoMultiplayerSessionService(NetworkManager networkManager)
		{
			_networkManager = networkManager ?? throw new ArgumentNullException(nameof(networkManager));

			if (_networkManager.GetComponent<UnityTransport>() == null)
			{
				throw new InvalidOperationException("UnityTransport is required on the same GameObject as NetworkManager.");
			}
		}

		public async Awaitable<OperationResult<string>> CreateHostSession()
		{
			if (!TryBeginSessionOperation(true, out string failureMessage))
			{
				return OperationResult<string>.Failure(failureMessage);
			}

			try
			{
				await EnsureUnityServicesReady();

				SessionOptions options = CreateHostSessionOptions();

				_session = await MultiplayerService.Instance.CreateSessionAsync(options);

				string joinCode = _session.Code ?? string.Empty;

				return OperationResult<string>.Success(joinCode);
			}
			catch (Exception exception)
			{
				await ShutdownAsync();
				return OperationResult<string>.Failure($"Failed to create multiplayer session. {exception.Message}");
			}
			finally
			{
				EndSessionOperation();
			}
		}

		public async Awaitable<OperationResult<IReadOnlyList<MultiplayerSessionInfo>>> BrowseSessions()
		{
			if (!TryBeginSessionOperation(false, out string failureMessage))
			{
				return OperationResult<IReadOnlyList<MultiplayerSessionInfo>>.Failure(
					failureMessage
				);
			}

			try
			{
				await EnsureUnityServicesReady();

				QuerySessionsResults results = await MultiplayerService.Instance.QuerySessionsAsync(
					CreateBrowseSessionsOptions()
				);

				List<MultiplayerSessionInfo> sessions = new();

				if (results?.Sessions != null)
				{
					for (int i = 0; i < results.Sessions.Count; i++)
					{
						ISessionInfo session = results.Sessions[i];

						if (!IsJoinableSession(session))
						{
							continue;
						}

						sessions.Add(CreateSessionInfo(session));
					}
				}

				return OperationResult<IReadOnlyList<MultiplayerSessionInfo>>.Success(sessions);
			}
			catch (Exception exception)
			{
				return OperationResult<IReadOnlyList<MultiplayerSessionInfo>>.Failure(
					$"Failed to browse multiplayer sessions. {exception.Message}"
				);
			}
			finally
			{
				EndSessionOperation();
			}
		}

		public async Awaitable<OperationResult> JoinSessionById(string sessionId)
		{
			if (!TryBeginSessionOperation(true, out string failureMessage))
			{
				return OperationResult.Failure(failureMessage);
			}

			try
			{
				string trimmedSessionId = sessionId?.Trim();
				if (string.IsNullOrWhiteSpace(trimmedSessionId))
				{
					return OperationResult.Failure("Session id is empty.");
				}

				await EnsureUnityServicesReady();

				_session = await MultiplayerService.Instance.JoinSessionByIdAsync(
					trimmedSessionId,
					CreateJoinSessionOptions()
				);

				return OperationResult.Success();
			}
			catch (Exception exception)
			{
				await ShutdownAsync();
				return OperationResult.Failure(CreateJoinFailureMessage(exception));
			}
			finally
			{
				EndSessionOperation();
			}
		}

		public async Awaitable<OperationResult> JoinSessionByCode(string joinCode)
		{
			if (!TryBeginSessionOperation(true, out string failureMessage))
			{
				return OperationResult.Failure(failureMessage);
			}

			try
			{
				string trimmedJoinCode = joinCode?.Trim();
				if (string.IsNullOrWhiteSpace(trimmedJoinCode))
				{
					return OperationResult.Failure("Join code is empty.");
				}

				await EnsureUnityServicesReady();

				_session = await MultiplayerService.Instance.JoinSessionByCodeAsync(
					trimmedJoinCode,
					CreateJoinSessionOptions()
				);

				return OperationResult.Success();
			}
			catch (Exception exception)
			{
				await ShutdownAsync();
				return OperationResult.Failure(CreateJoinFailureMessage(exception));
			}
			finally
			{
				EndSessionOperation();
			}
		}

		public async Awaitable ShutdownAsync()
		{
			ISession session = _session;
			_session = null;

			if (session != null)
			{
				try
				{
					await session.LeaveAsync();
				}
				catch
				{
					ShutdownNetworkManagerIfNeeded();
				}
			}
			else
			{
				ShutdownNetworkManagerIfNeeded();
			}

			await WaitForNetworkManagerShutdown();
		}

		private bool TryBeginSessionOperation(bool requireNoActiveSession, out string failureMessage)
		{
			if (_isSessionOperationInProgress)
			{
				failureMessage = "Multiplayer session operation is already in progress.";
				return false;
			}

			if (requireNoActiveSession && IsMultiplayer)
			{
				failureMessage = "Multiplayer session is already running.";
				return false;
			}

			_isSessionOperationInProgress = true;
			failureMessage = string.Empty;
			return true;
		}

		private void EndSessionOperation()
		{
			_isSessionOperationInProgress = false;
		}

		private static SessionOptions CreateHostSessionOptions()
		{
			return new SessionOptions
			{
				Type = SessionType,
				MaxPlayers = MaxPlayers,
				Name = CreateSessionName(),
				IsPrivate = false,
				SessionProperties = new Dictionary<string, SessionProperty>
				{
					[CompatibilityPropertyKey] = new SessionProperty(
						CompatibilityPropertyValue,
						VisibilityPropertyOptions.Public,
						PropertyIndex.String1
					)
				}
			}.WithRelayNetwork();
		}

		private static QuerySessionsOptions CreateBrowseSessionsOptions()
		{
			return new QuerySessionsOptions
			{
				Count = BrowseSessionCount,
				FilterOptions = new List<FilterOption>
				{
					new(
						FilterField.StringIndex1,
						CompatibilityPropertyValue,
						FilterOperation.Equal
					)
				}
			};
		}

		private static JoinSessionOptions CreateJoinSessionOptions()
		{
			return new JoinSessionOptions
			{
				Type = SessionType
			};
		}

		private static bool IsJoinableSession(ISessionInfo session)
		{
			return session != null &&
			       !session.IsLocked &&
			       session.AvailableSlots > 0;
		}

		private static MultiplayerSessionInfo CreateSessionInfo(ISessionInfo session)
		{
			string sessionName = string.IsNullOrWhiteSpace(session.Name)
				? "The Long Night"
				: session.Name;

			return new MultiplayerSessionInfo(
				session.Id,
				sessionName,
				session.AvailableSlots,
				session.MaxPlayers,
				session.HasPassword
			);
		}

		private static string CreateSessionName()
		{
			return $"The Long Night {DateTime.Now:HH:mm}";
		}

		private static string CreateJoinFailureMessage(Exception exception)
		{
			if (IsUnreachableSessionFailure(exception))
			{
				return "Failed to join multiplayer session. The selected session is no longer reachable. Refresh the public games list and try again.";
			}

			return $"Failed to join multiplayer session. {exception.Message}";
		}

		private static bool IsUnreachableSessionFailure(Exception exception)
		{
			for (Exception currentException = exception;
			     currentException != null;
			     currentException = currentException.InnerException)
			{
				string message = currentException.Message;
				if (message.Contains("Unexpected exception processing network metadata") ||
				    message.Contains("Joining network failed") ||
				    message.Contains("not connected"))
				{
					return true;
				}
			}

			return false;
		}

		private static async Awaitable EnsureUnityServicesReady()
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

		private void ShutdownNetworkManagerIfNeeded()
		{
			if (_networkManager != null && _networkManager.IsListening)
			{
				_networkManager.Shutdown();
			}
		}

		private async Awaitable WaitForNetworkManagerShutdown()
		{
			if (_networkManager == null)
			{
				return;
			}

			for (int i = 0; i < NetworkShutdownWaitFrames && _networkManager.IsListening; i++)
			{
				await Awaitable.NextFrameAsync();
			}
		}

	}
}
