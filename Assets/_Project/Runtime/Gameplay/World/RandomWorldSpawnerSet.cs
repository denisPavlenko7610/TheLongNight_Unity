namespace TLN.Gameplay.World
{
	public sealed class RandomWorldSpawnerSet
	{
		private readonly RandomWorldSpawner[] _spawners;

		public RandomWorldSpawnerSet(RandomWorldSpawner[] spawners)
		{
			_spawners = spawners;
		}

		public void TrySpawnForWorldStart(bool wasSaveLoaded)
		{
			if (_spawners == null)
			{
				return;
			}

			for (int i = 0; i < _spawners.Length; i++)
			{
				RandomWorldSpawner spawner = _spawners[i];

				if (spawner == null)
				{
					continue;
				}

				spawner.TrySpawnForWorldStart(wasSaveLoaded);
			}
		}
	}
}
