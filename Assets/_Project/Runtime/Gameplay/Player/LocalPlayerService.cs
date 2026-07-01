using System;
using TLN.Gameplay.Inventory;
using TLN.Gameplay.Items;
using TLN.Gameplay.Survival;

namespace TLN.Gameplay.Player
{
	public sealed class LocalPlayerService
	{
		public PlayerRoot PlayerRoot { get; private set; }
		public ISurvivalService SurvivalService { get; private set; }
		public IInventoryService InventoryService { get; private set; }
		public IItemUseService ItemUseService { get; private set; }

		public bool HasLocalPlayer => PlayerRoot != null;

		public event Action Changed;

		public void SetLocalPlayer(
			PlayerRoot playerRoot,
			ISurvivalService survivalService,
			IInventoryService inventoryService,
			IItemUseService itemUseService
		)
		{
			PlayerRoot = playerRoot ?? throw new ArgumentNullException(nameof(playerRoot));
			SurvivalService = survivalService ?? throw new ArgumentNullException(nameof(survivalService));
			InventoryService = inventoryService ?? throw new ArgumentNullException(nameof(inventoryService));
			ItemUseService = itemUseService ?? throw new ArgumentNullException(nameof(itemUseService));

			Changed?.Invoke();
		}

		public void Clear()
		{
			if (PlayerRoot == null &&
				SurvivalService == null &&
				InventoryService == null &&
				ItemUseService == null)
			{
				return;
			}

			PlayerRoot = null;
			SurvivalService = null;
			InventoryService = null;
			ItemUseService = null;

			Changed?.Invoke();
		}
	}
}
