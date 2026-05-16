using System.Collections.Generic;
using TLN.Gameplay.Inventory;
using TLN.Gameplay.Items;
using UnityEngine;

namespace TLN.UI.Inventory
{
	public sealed class InventoryDebugView : MonoBehaviour, IInventoryWindow
	{
		private IInventoryService _inventoryService;
		private ItemUseService _itemUseService;
		private bool _isVisible;

		public void Construct(IInventoryService inventoryService, ItemUseService itemUseService)
		{
			_inventoryService = inventoryService;
			_itemUseService = itemUseService;
		}

		public void Toggle()
		{
			_isVisible = !_isVisible;
		}

		public void Hide()
		{
			_isVisible = false;
		}

		private void OnGUI()
		{
			if (!_isVisible)
			{
				return;
			}

			if (_inventoryService == null)
			{
				return;
			}

			DrawInventoryWindow();
		}

		private void DrawInventoryWindow()
		{
			const int width = 360;
			const int height = 420;

			Rect windowRect = new Rect(
				Screen.width - width - 30,
				80,
				width,
				height);

			GUI.Box(windowRect, "Inventory Debug");
			GUI.Label(
				new Rect(windowRect.x + 20f, windowRect.y + 25f, width - 40f, 25f),
				$"Weight: {_inventoryService.CurrentWeight:0.##} / {_inventoryService.MaxCarryWeight:0.##} kg");
			float y = windowRect.y + 60f;

			IReadOnlyList<ItemStack> items = _inventoryService.Items;

			if (items.Count == 0)
			{
				GUI.Label(
					new Rect(windowRect.x + 20f, y, width - 40f, 25f),
					"Inventory is empty");

				return;
			}

			for (int i = 0; i < items.Count; i++)
			{
				ItemStack stack = items[i];

				string line = $"{stack.Definition.DisplayName} x{stack.Amount}";

				GUI.Label(
					new Rect(windowRect.x + 20f, y, width - 120f, 25f),
					line);

				bool canUse = stack.Definition is ConsumableItemDefinition;

				GUI.enabled = canUse && _itemUseService != null;

				if (GUI.Button(
					new Rect(windowRect.x + width - 90f, y, 60f, 22f),
					"Use"))
				{
					_itemUseService.UseItemAt(i);
					return;
				}

				GUI.enabled = true;

				y += 26f;
			}
		}
	}
}
