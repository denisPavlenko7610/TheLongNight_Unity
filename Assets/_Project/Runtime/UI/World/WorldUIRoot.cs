using Assign;
using TLN.UI.Campfire;
using TLN.UI.HUD;
using TLN.UI.Pause;
using TLN.UI.Sleep;
using TLN.UI.SurvivalMenu;
using UnityEngine;

namespace TLN.UI.World
{
	public sealed class WorldUIRoot : MonoBehaviour
	{
		[field: Header("World UI")]
		[field: SerializeField][field: Assign(Mode.Scene)]
		public WorldHUDView HUD { get; private set; }

		[field: SerializeField][field: Assign(Mode.Scene)]
		public SurvivalMenuWindowView SurvivalMenu { get; private set; }

		[field: SerializeField][field: Assign(Mode.Scene)]
		public SleepWindowView SleepWindow { get; private set; }

		[field: SerializeField][field: Assign(Mode.Scene)]
		public PauseMenuView PauseMenu { get; private set; }

		[field: SerializeField][field: Assign(Mode.Scene)]
		public CampfireWindowView CampfireWindow { get; private set; }

		public bool HasAllRequiredReferences()
		{
			return HUD != null
				&& SurvivalMenu != null
				&& CampfireWindow != null
				&& SleepWindow != null
				&& PauseMenu != null;
		}
	}
}
