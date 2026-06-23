using TLN.Gameplay.Player;
using UnityEngine;

namespace TLN.Gameplay.Interaction
{
	public readonly struct InteractionContext
	{
		public PlayerRoot Player { get; }
		public Camera Camera { get; }

		public InteractionContext(PlayerRoot player, Camera camera)
		{
			Player = player;
			Camera = camera;
		}
	}
}