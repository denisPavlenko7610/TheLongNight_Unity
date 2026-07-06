using TLN.Gameplay.Player;

namespace TLN.Gameplay.Wildlife
{
	public sealed class WildlifeTargetService
	{
		public PlayerRoot PlayerRoot { get; private set; }

		public void SetPlayerRoot(PlayerRoot playerRoot)
		{
			PlayerRoot = playerRoot;
		}
	}
}
