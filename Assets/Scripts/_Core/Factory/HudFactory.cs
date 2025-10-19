using TheLongNight.UI;
using UnityEngine;

namespace TheLongNight.Factory
{
	public class HudFactory
	{
		private HUD _hudPrefab;
		private Transform _parent;

		public HudFactory(HUD hudPrefab, Transform parent)
		{
			_hudPrefab = hudPrefab;
			_parent = parent;
		}

		public HUD Create(Player player)
		{
			HUD hud = Object.Instantiate(_hudPrefab, _parent);
			hud.Init(player);
			return hud;
		}
	}
}
