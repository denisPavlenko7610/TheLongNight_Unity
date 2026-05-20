using Assign;
using TLN.UI.HUD;
using TLN.UI.Inventory;
using TLN.UI.Pause;
using TLN.UI.Sleep;
using UnityEngine;

namespace TLN.UI.World
{
	public sealed class WorldUIRoot : MonoBehaviour
	{
		[field: Header("World UI")]
		[field: SerializeField][field: Assign(Mode.Scene)]
		public WorldHUDView HUD { get; private set; }

		[field: SerializeField][field: Assign(Mode.Scene)]
		public InventoryWindowView InventoryWindow { get; private set; }

		[field: SerializeField][field: Assign(Mode.Scene)]
		public SleepWindowView SleepWindow { get; private set; }

		[field: SerializeField][field: Assign(Mode.Scene)]
		public PauseMenuView PauseMenu { get; private set; }
	}
}
