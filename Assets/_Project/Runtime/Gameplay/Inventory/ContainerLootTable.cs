using System;
using System.Collections.Generic;
using TLN.Gameplay.Items;
using UnityEngine;

namespace TLN.Gameplay.Inventory
{
	[CreateAssetMenu(
		fileName = "ContainerLootTable",
		menuName = "TLN/Inventory/Container Loot Table"
	)]
	public sealed class ContainerLootTable : ScriptableObject
	{
		[Serializable]
		public sealed class Entry
		{
			[SerializeField] private ItemDefinition _item;
			[SerializeField] private int _minAmount = 1;
			[SerializeField] private int _maxAmount = 1;
			[SerializeField]
			[Range(0f, 1f)]
			private float _dropChance = 0.5f;

			public ItemDefinition Item => _item;
			public int MinAmount => Mathf.Max(1, _minAmount);
			public int MaxAmount => Mathf.Max(MinAmount, _maxAmount);
			public float DropChance => Mathf.Clamp01(_dropChance);
		}

		[SerializeField] private Entry[] _entries;

		public IReadOnlyList<Entry> Entries => _entries;

		/// <summary>
		/// Rolls a random set of item stacks. Each entry is resolved independently.
		/// </summary>
		public List<ItemStack> Roll()
		{
			List<ItemStack> result = new();

			if (_entries == null)
			{
				return result;
			}

			for (int i = 0; i < _entries.Length; i++)
			{
				Entry entry = _entries[i];

				if (entry == null || entry.Item == null)
				{
					continue;
				}

				if (UnityEngine.Random.value > entry.DropChance)
				{
					continue;
				}

				int amount = UnityEngine.Random.Range(entry.MinAmount, entry.MaxAmount + 1);

				if (amount <= 0)
				{
					continue;
				}

				result.Add(new ItemStack(entry.Item, amount));
			}

			return result;
		}
	}
}
