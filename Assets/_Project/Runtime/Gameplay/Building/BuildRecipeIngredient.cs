using System;
using TLN.Gameplay.Items;
using UnityEngine;

namespace TLN.Gameplay.Building
{
	[Serializable]
	public sealed class BuildRecipeIngredient
	{
		[SerializeField] private ItemDefinition _item;
		[SerializeField] private int _amount = 1;

		public ItemDefinition Item => _item;
		public int Amount => Mathf.Max(1, _amount);
	}
}