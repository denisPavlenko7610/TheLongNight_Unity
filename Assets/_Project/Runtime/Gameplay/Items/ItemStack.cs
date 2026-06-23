using System;

namespace TLN.Gameplay.Items
{
	[Serializable]
	public struct ItemStack
	{
		public ItemDefinition Definition { get; private set; }
		public int Amount { get; private set; }

		public ItemStack(ItemDefinition definition, int amount)
		{
			if (definition == null)
			{
				throw new ArgumentNullException(nameof(definition));
			}

			if (amount <= 0)
			{
				throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be greater than zero.");
			}

			Definition = definition;
			Amount = amount;
		}

		public void AddAmount(int amount)
		{
			if (amount <= 0)
			{
				throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be greater than zero.");
			}

			Amount += amount;
		}

		public void RemoveAmount(int amount)
		{
			if (amount <= 0)
			{
				throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be greater than zero.");
			}

			if (amount > Amount)
			{
				throw new InvalidOperationException("Cannot remove more items than stack contains.");
			}

			Amount -= amount;
		}
	}
}