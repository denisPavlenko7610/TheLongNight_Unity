using System;
using System.Collections.Generic;
using TLN.Gameplay.Items;

namespace TLN.Gameplay.Inventory
{
	/// <summary>
	/// Read-only view of a world container's contents for the transfer UI.
	/// </summary>
	public interface IContainerView
	{
		IReadOnlyList<ItemStack> Contents { get; }

		event Action Changed;
	}
}
