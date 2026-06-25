using System.Collections.Generic;
using TLN.Application.Saves;
using TLN.Core.Logging;
using TLN.Gameplay.World;
using UnityEngine;

namespace TLN.Gameplay.Saves
{
	public sealed class WorldSaveRegistry
	{
		private readonly IWorldObjectFactory _worldObjectFactory;
		private readonly WorldPrefabCatalog _prefabCatalog;

		private readonly HashSet<string> _destroyedSceneEntityIds = new();

		public WorldSaveRegistry(IWorldObjectFactory worldObjectFactory, WorldPrefabCatalog prefabCatalog)
		{
			_worldObjectFactory = worldObjectFactory;
			_prefabCatalog = prefabCatalog;
		}

		public void MarkSceneEntityDestroyed(string entityId)
		{
			if (string.IsNullOrWhiteSpace(entityId))
			{
				return;
			}

			_destroyedSceneEntityIds.Add(entityId);
		}

		public void Capture(WorldSaveData worldData)
		{
			if (worldData == null)
			{
				return;
			}

			worldData.destroyedSceneEntityIds.Clear();
			worldData.destroyedSceneEntityIds.AddRange(_destroyedSceneEntityIds);

			worldData.entities.Clear();

			PersistentWorldEntity[] entities = Object.FindObjectsByType<PersistentWorldEntity>(
				FindObjectsInactive.Include,
				FindObjectsSortMode.None
			);

			for (int i = 0; i < entities.Length; i++)
			{
				PersistentWorldEntity entity = entities[i];

				if (entity == null)
				{
					continue;
				}

				if (string.IsNullOrWhiteSpace(entity.Id))
				{
					continue;
				}

				if (entity.IsSceneObject && _destroyedSceneEntityIds.Contains(entity.Id))
				{
					continue;
				}

				worldData.entities.Add(entity.CaptureState());
			}
		}

		public void Restore(WorldSaveData worldData)
		{
			if (worldData == null)
			{
				return;
			}

			_destroyedSceneEntityIds.Clear();

			if (worldData.destroyedSceneEntityIds != null)
			{
				for (int i = 0; i < worldData.destroyedSceneEntityIds.Count; i++)
				{
					string entityId = worldData.destroyedSceneEntityIds[i];

					if (!string.IsNullOrWhiteSpace(entityId))
					{
						_destroyedSceneEntityIds.Add(entityId);
					}
				}
			}

			Dictionary<string, PersistentWorldEntity> entitiesById = FindEntitiesById();

			DestroySceneEntitiesMarkedAsDestroyed(entitiesById);

			DestroyRuntimeEntitiesMissingFromSave(entitiesById, worldData);

			if (worldData.entities == null)
			{
				return;
			}

			for (int i = 0; i < worldData.entities.Count; i++)
			{
				RestoreEntity(worldData.entities[i], entitiesById);
			}
		}

		private void RestoreEntity(WorldEntitySaveData data, Dictionary<string, PersistentWorldEntity> entitiesById)
		{
			if (data == null || string.IsNullOrWhiteSpace(data.id))
			{
				return;
			}

			if (_destroyedSceneEntityIds.Contains(data.id))
			{
				return;
			}

			if (entitiesById.TryGetValue(data.id, out PersistentWorldEntity existingEntity))
			{
				existingEntity.RestoreState(data);
				return;
			}

			if (data.isSceneObject)
			{
				TLNLogger.LogWarning($"Saved scene entity was not found: {data.id}");

				return;
			}

			if (TryCreateRuntimeEntity(data, out PersistentWorldEntity createdEntity))
			{
				entitiesById[data.id] = createdEntity;
			}
		}

		private bool TryCreateRuntimeEntity(WorldEntitySaveData data, out PersistentWorldEntity entity)
		{
			entity = null;

			if (!_prefabCatalog.TryGetPrefab(data.prefabId, out GameObject prefab))
			{
				TLNLogger.LogWarning($"Cannot restore world entity. " + $"Unknown prefab id: {data.prefabId}");
				return false;
			}

			GameObject instance = _worldObjectFactory.Create(prefab, data.position.ToVector3(), data.rotation.ToQuaternion());
			if (instance == null)
			{
				TLNLogger.LogWarning($"Cannot restore world entity. Factory returned null: {data.prefabId}");
				return false;
			}

			if (!instance.TryGetComponent(out entity))
			{
				TLNLogger.LogWarning($"Restored prefab has no PersistentWorldEntity: {data.prefabId}", instance);
				return false;
			}

			entity.InitializeRuntimeEntity(data.id);
			entity.RestoreState(data);
			return true;
		}

		private void DestroySceneEntitiesMarkedAsDestroyed(Dictionary<string, PersistentWorldEntity> entitiesById)
		{
			foreach (string entityId in _destroyedSceneEntityIds)
			{
				if (!entitiesById.TryGetValue(entityId, out PersistentWorldEntity entity))
				{
					continue;
				}

				if (!entity.IsSceneObject)
				{
					continue;
				}

				Object.Destroy(entity.gameObject);
				entitiesById.Remove(entityId);
			}
		}

		private void DestroyRuntimeEntitiesMissingFromSave(
			Dictionary<string, PersistentWorldEntity> entitiesById,
			WorldSaveData worldData
		)
		{
			HashSet<string> savedIds = new HashSet<string>();

			if (worldData.entities != null)
			{
				for (int i = 0; i < worldData.entities.Count; i++)
				{
					WorldEntitySaveData entityData = worldData.entities[i];

					if (entityData == null || string.IsNullOrWhiteSpace(entityData.id))
					{
						continue;
					}

					savedIds.Add(entityData.id);
				}
			}

			List<string> removedIds = new List<string>();

			foreach (KeyValuePair<string, PersistentWorldEntity> pair in entitiesById)
			{
				PersistentWorldEntity entity = pair.Value;

				if (entity == null || entity.IsSceneObject)
				{
					continue;
				}

				if (savedIds.Contains(entity.Id))
				{
					continue;
				}

				Object.Destroy(entity.gameObject);
				removedIds.Add(pair.Key);
			}

			for (int i = 0; i < removedIds.Count; i++)
			{
				entitiesById.Remove(removedIds[i]);
			}
		}

		private static Dictionary<string, PersistentWorldEntity> FindEntitiesById()
		{
			Dictionary<string, PersistentWorldEntity> result = new();

			PersistentWorldEntity[] entities = Object.FindObjectsByType<PersistentWorldEntity>(
				FindObjectsInactive.Include,
				FindObjectsSortMode.None
			);

			for (int i = 0; i < entities.Length; i++)
			{
				PersistentWorldEntity entity = entities[i];

				if (entity == null ||
					string.IsNullOrWhiteSpace(entity.Id))
				{
					continue;
				}

				if (!result.TryAdd(entity.Id, entity))
				{
					TLNLogger.LogWarning($"Duplicate persistent world entity id: {entity.Id}", entity);
				}
			}

			return result;
		}
	}
}
