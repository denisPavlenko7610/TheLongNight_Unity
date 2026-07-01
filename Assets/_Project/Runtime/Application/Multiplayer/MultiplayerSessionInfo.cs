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
			AvailableSlots = availableSlots;
			MaxPlayers = maxPlayers;
			HasPassword = hasPassword;
		}

		public string Id { get; }
		public string Name { get; }
		public int AvailableSlots { get; }
		public int MaxPlayers { get; }
		public bool HasPassword { get; }
		public int PlayerCount => MaxPlayers - AvailableSlots;
	}
}
