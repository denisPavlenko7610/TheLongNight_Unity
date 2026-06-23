#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TLN.Gameplay.Items;
using TLN.Gameplay.Saves;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TLN.Editor.Saves
{
	public static class SaveSetupTools
	{
		private const string ItemCatalogPath = "Assets/_Project/Config/Saves/ItemCatalog.asset";

		private const string WorldPrefabCatalogPath = "Assets/_Project/Config/Saves/WorldPrefabCatalog.asset";

		private const string SearchRoot = "Assets/_Project";

		[MenuItem("Tools/TLN/Saves/Setup Current Scene Saves")]
		public static void SetupCurrentSceneSaves()
		{
			AddMissingPersistentEntitiesToSceneSaveables();
			FixScenePersistentIds();
			ConfigurePrefabPersistentIds();
			RebuildItemCatalog();
			RebuildWorldPrefabCatalog();
			ValidateCurrentSceneSaveSetup();

			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();

			Debug.Log("[TLN Saves] Current scene save setup completed.");
		}

		private static void AddMissingPersistentEntitiesToSceneSaveables()
		{
			MonoBehaviour[] behaviours = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(
				FindObjectsInactive.Include,
				FindObjectsSortMode.None
			);

			int addedCount = 0;

			for (int i = 0; i < behaviours.Length; i++)
			{
				MonoBehaviour behaviour = behaviours[i];

				if (behaviour == null)
				{
					continue;
				}

				if (behaviour is not IWorldSaveable)
				{
					continue;
				}

				if (behaviour.TryGetComponent<PersistentWorldEntity>(out _))
				{
					continue;
				}

				Undo.AddComponent<PersistentWorldEntity>(behaviour.gameObject);

				EditorUtility.SetDirty(behaviour.gameObject);
				addedCount++;
			}

			if (addedCount > 0)
			{
				MarkActiveSceneDirty();
			}

			Debug.Log($"[TLN Saves] Added PersistentWorldEntity components: {addedCount}");
		}

		private static void FixScenePersistentIds()
		{
			PersistentWorldEntity[] entities = UnityEngine.Object.FindObjectsByType<PersistentWorldEntity>(
				FindObjectsInactive.Include,
				FindObjectsSortMode.None
			);

			HashSet<string> usedIds = new();
			int fixedCount = 0;

			for (int i = 0; i < entities.Length; i++)
			{
				PersistentWorldEntity entity = entities[i];

				if (entity == null)
				{
					continue;
				}

				// Everything currently placed in the open scene must be saved
				// as a scene object, even if it came from a prefab.
				SerializedObject serializedObject = new SerializedObject(entity);

				SerializedProperty idProperty = serializedObject.FindProperty("_id");

				SerializedProperty prefabIdProperty = serializedObject.FindProperty("_prefabId");

				if (idProperty == null || prefabIdProperty == null)
				{
					continue;
				}

				prefabIdProperty.stringValue = string.Empty;

				string currentId = idProperty.stringValue;

				bool shouldRegenerate = string.IsNullOrWhiteSpace(currentId) || usedIds.Contains(currentId);

				if (shouldRegenerate)
				{
					idProperty.stringValue = Guid.NewGuid().ToString("N");

					fixedCount++;
				}

				usedIds.Add(idProperty.stringValue);

				serializedObject.ApplyModifiedProperties();
				EditorUtility.SetDirty(entity);
			}

			if (fixedCount > 0)
			{
				MarkActiveSceneDirty();
			}

			Debug.Log($"[TLN Saves] Fixed scene persistent ids: {fixedCount}");
		}

		private static void ConfigurePrefabPersistentIds()
		{
			string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { SearchRoot });

			int configuredCount = 0;

			for (int i = 0; i < prefabGuids.Length; i++)
			{
				string path = AssetDatabase.GUIDToAssetPath(prefabGuids[i]);

				GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

				if (prefab == null)
				{
					continue;
				}

				PersistentWorldEntity entity = prefab.GetComponent<PersistentWorldEntity>();

				if (entity == null)
				{
					continue;
				}

				SerializedObject serializedObject = new SerializedObject(entity);

				SerializedProperty idProperty = serializedObject.FindProperty("_id");

				SerializedProperty prefabIdProperty = serializedObject.FindProperty("_prefabId");

				if (idProperty == null || prefabIdProperty == null)
				{
					continue;
				}

				string prefabId = AssetDatabase.AssetPathToGUID(path);

				bool changed = false;

				if (prefabIdProperty.stringValue != prefabId)
				{
					prefabIdProperty.stringValue = prefabId;
					changed = true;
				}

				if (!string.IsNullOrWhiteSpace(idProperty.stringValue))
				{
					idProperty.stringValue = string.Empty;
					changed = true;
				}

				if (!changed)
				{
					continue;
				}

				serializedObject.ApplyModifiedProperties();
				EditorUtility.SetDirty(entity);
				configuredCount++;
			}

			Debug.Log($"[TLN Saves] Configured prefab persistent ids: {configuredCount}");
		}

		private static void RebuildItemCatalog()
		{
			ItemCatalog catalog = FindOrCreateAsset<ItemCatalog>(ItemCatalogPath);

			ItemDefinition[] items = FindAssets<ItemDefinition>()
				.Where(item => item != null)
				.OrderBy(item => item.Id)
				.ToArray();

			catalog.EditorSetItems(items);

			Debug.Log($"[TLN Saves] ItemCatalog rebuilt. Items: {items.Length}");
		}

		private static void RebuildWorldPrefabCatalog()
		{
			WorldPrefabCatalog catalog = FindOrCreateAsset<WorldPrefabCatalog>(WorldPrefabCatalogPath);

			List<WorldPrefabCatalog.EntryDraft> entries = new List<WorldPrefabCatalog.EntryDraft>();

			string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { SearchRoot });

			HashSet<string> usedIds = new HashSet<string>();

			for (int i = 0; i < prefabGuids.Length; i++)
			{
				string path = AssetDatabase.GUIDToAssetPath(prefabGuids[i]);

				GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

				if (prefab == null)
				{
					continue;
				}

				PersistentWorldEntity entity = prefab.GetComponent<PersistentWorldEntity>();

				if (entity == null)
				{
					continue;
				}

				string prefabId = AssetDatabase.AssetPathToGUID(path);

				if (string.IsNullOrWhiteSpace(prefabId))
				{
					continue;
				}

				if (!usedIds.Add(prefabId))
				{
					Debug.LogWarning($"[TLN Saves] Duplicate prefab id: {prefabId}. Prefab: {path}", prefab);

					continue;
				}

				entries.Add(new WorldPrefabCatalog.EntryDraft(prefabId, prefab));
			}

			catalog.EditorSetEntries(entries);

			Debug.Log($"[TLN Saves] WorldPrefabCatalog rebuilt. Prefabs: {entries.Count}");
		}

		private static void ValidateCurrentSceneSaveSetup()
		{
			PersistentWorldEntity[] entities = UnityEngine.Object.FindObjectsByType<PersistentWorldEntity>(
				FindObjectsInactive.Include,
				FindObjectsSortMode.None
			);

			Dictionary<string, PersistentWorldEntity> ids = new();

			int errorCount = 0;

			for (int i = 0; i < entities.Length; i++)
			{
				PersistentWorldEntity entity = entities[i];

				if (entity == null)
				{
					continue;
				}

				if (string.IsNullOrWhiteSpace(entity.Id))
				{
					Debug.LogError("[TLN Saves] Scene persistent entity has empty Id.", entity);

					errorCount++;
					continue;
				}

				if (ids.TryGetValue(entity.Id, out PersistentWorldEntity duplicate))
				{
					Debug.LogError($"[TLN Saves] Duplicate scene persistent Id: {entity.Id}", entity);

					Debug.LogError($"[TLN Saves] First object with duplicate Id: {entity.Id}", duplicate);

					errorCount++;
					continue;
				}

				ids.Add(entity.Id, entity);
			}

			if (errorCount == 0)
			{
				Debug.Log($"[TLN Saves] Scene save setup is valid. Persistent entities: {entities.Length}");
			}
			else
			{
				Debug.LogError($"[TLN Saves] Scene save setup has errors: {errorCount}");
			}
		}

		private static T[] FindAssets<T>() where T : UnityEngine.Object
		{
			string filter = $"t:{typeof(T).Name}";

			string[] guids = AssetDatabase.FindAssets(filter, new[] { SearchRoot });

			List<T> result = new();

			for (int i = 0; i < guids.Length; i++)
			{
				string path = AssetDatabase.GUIDToAssetPath(guids[i]);

				T asset = AssetDatabase.LoadAssetAtPath<T>(path);

				if (asset != null)
				{
					result.Add(asset);
				}
			}

			return result.ToArray();
		}

		private static T FindOrCreateAsset<T>(string assetPath) where T : ScriptableObject
		{
			T existing = AssetDatabase.LoadAssetAtPath<T>(assetPath);

			if (existing != null)
			{
				return existing;
			}

			string folder = Path.GetDirectoryName(assetPath);

			if (!string.IsNullOrWhiteSpace(folder) && !AssetDatabase.IsValidFolder(folder))
			{
				Directory.CreateDirectory(folder);
				AssetDatabase.Refresh();
			}

			T created = ScriptableObject.CreateInstance<T>();

			AssetDatabase.CreateAsset(created, assetPath);

			AssetDatabase.SaveAssets();

			return created;
		}

		private static void MarkActiveSceneDirty()
		{
			Scene activeScene = SceneManager.GetActiveScene();

			if (activeScene.IsValid())
			{
				EditorSceneManager.MarkSceneDirty(activeScene);
			}
		}
	}
}
#endif
