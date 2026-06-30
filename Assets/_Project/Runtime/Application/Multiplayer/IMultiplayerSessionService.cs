using TLN.Core.Lifetime;

namespace TLN.Application.Multiplayer
{
	public interface IMultiplayerSessionService : IGameService
	{
		bool IsMultiplayer { get; }
		bool IsServer { get; }
		bool IsClient { get; }
		bool IsHost { get; }

		bool StartHost();
		bool StartClient(string address);
		void Shutdown();
	}
}
