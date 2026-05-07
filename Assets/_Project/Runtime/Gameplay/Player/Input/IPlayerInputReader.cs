using UnityEngine;

namespace TLN.Gameplay.Player.Input
{
	public interface IPlayerInputReader
	{
		Vector2 Move { get; }
		Vector2 Look { get; }

		bool IsSprintHeld { get; }
		bool WasInteractPressedThisFrame { get; }
		bool WasPausePressedThisFrame { get; }
	}
}
