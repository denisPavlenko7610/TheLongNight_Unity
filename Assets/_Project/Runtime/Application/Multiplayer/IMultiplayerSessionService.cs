using System.Threading.Tasks;
using TLN.Core.Lifetime;
using TLN.Core.Results;

namespace TLN.Application.Multiplayer
{
	public interface IMultiplayerSessionService : IGameService
	{
		bool IsMultiplayer { get; }
		bool IsServer { get; }
		bool IsClient { get; }
		bool IsHost { get; }

		string JoinCode { get; }

		Task<OperationResult<string>> CreateHostSession();
		Task<OperationResult> JoinSessionByCode(string joinCode);

		void Shutdown();
	}
}
