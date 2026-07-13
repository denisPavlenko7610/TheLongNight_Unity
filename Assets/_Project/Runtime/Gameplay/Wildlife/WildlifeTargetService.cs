using System.Collections.Generic;
using TLN.Gameplay.Player;
using UnityEngine;

namespace TLN.Gameplay.Wildlife
{
	public sealed class WildlifeTargetService
	{
		private readonly List<PlayerRoot> _players = new();

		public PlayerRoot PlayerRoot { get; private set; }

		public void SetPlayerRoot(PlayerRoot playerRoot)
		{
			PlayerRoot = playerRoot;
			RegisterPlayer(playerRoot);
		}

		public void RegisterPlayer(PlayerRoot playerRoot)
		{
			if (playerRoot == null || _players.Contains(playerRoot))
			{
				return;
			}

			_players.Add(playerRoot);
			PlayerRoot ??= playerRoot;
		}

		public PlayerRoot GetClosestPlayer(Vector3 position)
		{
			RemoveMissingPlayers();

			PlayerRoot closestPlayer = null;
			float closestSqrDistance = float.MaxValue;

			for (int i = 0; i < _players.Count; i++)
			{
				PlayerRoot player = _players[i];
				float sqrDistance = (player.transform.position - position).sqrMagnitude;
				if (sqrDistance >= closestSqrDistance)
				{
					continue;
				}

				closestPlayer = player;
				closestSqrDistance = sqrDistance;
			}

			return closestPlayer != null ? closestPlayer : PlayerRoot;
		}

		private void RemoveMissingPlayers()
		{
			for (int i = _players.Count - 1; i >= 0; i--)
			{
				if (_players[i] == null)
				{
					_players.RemoveAt(i);
				}
			}

			if (PlayerRoot == null && _players.Count > 0)
			{
				PlayerRoot = _players[0];
			}
		}
	}
}
