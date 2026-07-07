using System;

namespace TLN.Application.Multiplayer
{
	public readonly struct MultiplayerSessionInfo
	{
		public MultiplayerSessionInfo(
			string id,
			string name,
			int availableSlots,
			int maxPlayers,
			bool hasPassword
		)
		{
			Id = id ?? string.Empty;
			Name = name ?? string.Empty;
			MaxPlayers = Math.Max(0, maxPlayers);
			AvailableSlots = Math.Max(0, Math.Min(MaxPlayers, availableSlots));
			HasPassword = hasPassword;
		}

		public string Id { get; }
		public string Name { get; }
		public int AvailableSlots { get; }
		public int MaxPlayers { get; }
		public bool HasPassword { get; }
		public int PlayerCount => Math.Max(0, MaxPlayers - AvailableSlots);
	}
}
