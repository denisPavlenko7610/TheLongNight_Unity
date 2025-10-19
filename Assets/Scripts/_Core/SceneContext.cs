using TheLongNight.Factory;
using TheLongNight.UI;
using UnityEngine;

namespace TheLongNight
{
	public class SceneContext : MonoBehaviour
	{
		[Header("Prefabs")]
		[SerializeField] private Player _playerPrefab;
		[SerializeField] private HUD _hudPrefab;

		[Header("Positions")]
		[SerializeField] private Transform _canvasParent;
		[SerializeField] private Transform _playerSpawnPoint;

		private void Awake()
		{
			var characterFactory = new PlayerFactory(_playerPrefab, _playerSpawnPoint);
			var hudFactory = new HudFactory(_hudPrefab, _canvasParent);

			Player player = characterFactory.Create();
			HUD hud = hudFactory.Create(player);
		}
	}
}
