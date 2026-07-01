using System;
using Unity.Collections;
using Unity.Netcode;

namespace TLN.Gameplay.Inventory.Networking
{
	public struct NetworkInventoryItem : INetworkSerializable, IEquatable<NetworkInventoryItem>
	{
		public FixedString64Bytes ItemId;
		public int Amount;

		public NetworkInventoryItem(string itemId, int amount)
		{
			ItemId = itemId;
			Amount = amount;
		}

		public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
		{
			serializer.SerializeValue(ref ItemId);
			serializer.SerializeValue(ref Amount);
		}

		public bool Equals(NetworkInventoryItem other)
		{
			return ItemId.Equals(other.ItemId) && Amount == other.Amount;
		}

		public override bool Equals(object obj)
		{
			return obj is NetworkInventoryItem other && Equals(other);
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(ItemId, Amount);
		}
	}
}
