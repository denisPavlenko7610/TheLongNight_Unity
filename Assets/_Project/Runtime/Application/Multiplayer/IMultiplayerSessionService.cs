using System.Collections.Generic;
using TLN.Core.Lifetime;
using TLN.Core.Results;
using UnityEngine;

namespace TLN.Application.Multiplayer
{
	public interface IMultiplayerSessionService : IGameService
	{
		bool IsMultiplayer { get; }
		bool IsServer { get; }

		Awaitable<OperationResult<string>> CreateHostSession();
		Awaitable<OperationResult<IReadOnlyList<MultiplayerSessionInfo>>> BrowseSessions();
		Awaitable<OperationResult> JoinSessionById(string sessionId);
		Awaitable<OperationResult> JoinSessionByCode(string joinCode);

		Awaitable ShutdownAsync();
	}
}
