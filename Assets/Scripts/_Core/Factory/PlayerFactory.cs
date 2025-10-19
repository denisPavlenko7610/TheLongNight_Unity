using UnityEngine;

namespace TheLongNight.Factory
{
	public class PlayerFactory
	{
		private Player _characterPrefab;
		private Transform _spawnPoint;

		public PlayerFactory(Player characterPrefab, Transform spawnPoint)
		{
			_spawnPoint = spawnPoint;
			_characterPrefab = characterPrefab;
		}

		public Player Create()
		{
			return Object.Instantiate(_characterPrefab,  _spawnPoint.position, Quaternion.identity, _spawnPoint);
		}
	}
}
