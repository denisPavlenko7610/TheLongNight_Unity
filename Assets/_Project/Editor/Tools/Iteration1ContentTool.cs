using System.Collections.Generic;
using TLN.Gameplay.Inventory;
using TLN.Gameplay.Items;
using TLN.Gameplay.Saves;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Localization;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Localization.Tables;
using UnityEngine.UIElements;

namespace TLN.Editor.Tools
{
	/// <summary>
	/// One-shot content tool for iteration 1 (TLD-style inventory + containers).
	/// Idempotent: re-running skips assets that already exist.
	/// </summary>
	public static class Iteration1ContentTool
	{
		private const string ItemsContentDir = "Assets/_Project/Content/Items";
		private const string PrefabsItemsDir = "Assets/_Project/Prefabs/Items";
		private const string IconsDir = "Assets/_Project/Art/UI/Textures/Icons/Items";
		private const string ContainersContentDir = "Assets/_Project/Content/Inventory";
		private const string ContainersPrefabDir = "Assets/_Project/Prefabs/Containers";
		private const string WorldScenePath = "Assets/_Project/Scenes/World/World.unity";

		[MenuItem("TLN/Tools/Iteration 1: Create Container Content")]
		public static void Run()
		{
			var log = new List<string>();

			var itemsCollection = LocalizationEditorSettings.GetStringTableCollection("Items");
			string itemsTableGuid =
				AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(itemsCollection.SharedData));

			var keyIds = new Dictionary<string, long>();
			foreach (SharedTableData.SharedTableEntry entry in itemsCollection.SharedData.Entries)
			{
				keyIds[entry.Key] = entry.Id;
			}

			// ---------- 1) Item definitions ----------
			ItemDefinition bandage = CreateConsumableItem(
				ItemsContentDir + "/ITM_Bandage.asset",
				"bandage",
				itemsTableGuid,
				keyIds,
				ItemCategory.Medicine,
				0.3f,
				5,
				hungerRestore: 0f,
				fatigueRestore: 0f,
				conditionRestore: 15f,
				iconName: "Bandage",
				log);

			ItemDefinition chocolate = CreateConsumableItem(
				ItemsContentDir + "/ITM_Chocolate.asset",
				"chocolate",
				itemsTableGuid,
				keyIds,
				ItemCategory.Food,
				0.2f,
				5,
				hungerRestore: 20f,
				fatigueRestore: 10f,
				conditionRestore: 0f,
				iconName: "Chocolate",
				log);

			ItemDefinition rope = CreateBaseItem(
				ItemsContentDir + "/ITM_Rope.asset",
				"rope",
				itemsTableGuid,
				keyIds,
				ItemCategory.Material,
				1f,
				3,
				"Rope",
				log);

			AssetDatabase.SaveAssets();

			// ---------- 2) World item prefabs for the new items ----------
			GameObject worldItemBase = AssetDatabase.LoadAssetAtPath<GameObject>(
				PrefabsItemsDir + "/PF_WorldItem_Base.prefab");

			GameObject bandagePrefab = CreateWorldItemPrefab(
				worldItemBase, PrefabsItemsDir + "/PF_Bandage.prefab", bandage, log);
			GameObject chocolatePrefab = CreateWorldItemPrefab(
				worldItemBase, PrefabsItemsDir + "/PF_Chocolate.prefab", chocolate, log);
			GameObject ropePrefab = CreateWorldItemPrefab(
				worldItemBase, PrefabsItemsDir + "/PF_Rope.prefab", rope, log);

			// ---------- 3) World item prefab catalog ----------
			WorldItemPrefabCatalog prefabCatalog =
				LoadOrCreateAsset<WorldItemPrefabCatalog>(ContainersContentDir + "/CAT_WorldItemPrefabs.asset");

			if (prefabCatalog != null)
			{
				prefabCatalog.EditorSetEntries(new List<WorldItemPrefabCatalog.EntryDraft>
				{
					new WorldItemPrefabCatalog.EntryDraft("apple", LoadOrNull<GameObject>(PrefabsItemsDir + "/PF_Apple.prefab")),
					new WorldItemPrefabCatalog.EntryDraft("soda", LoadOrNull<GameObject>(PrefabsItemsDir + "/PF_Soda.prefab")),
					new WorldItemPrefabCatalog.EntryDraft("stick", LoadOrNull<GameObject>(PrefabsItemsDir + "/PF_Stick.prefab")),
					new WorldItemPrefabCatalog.EntryDraft("water_purified", LoadOrNull<GameObject>(PrefabsItemsDir + "/PF_Water.prefab")),
					new WorldItemPrefabCatalog.EntryDraft("parka", LoadOrNull<GameObject>(PrefabsItemsDir + "/PF_Parka.prefab")),
					new WorldItemPrefabCatalog.EntryDraft("wool_hat", LoadOrNull<GameObject>(PrefabsItemsDir + "/PF_WoolHat.prefab")),
					new WorldItemPrefabCatalog.EntryDraft("mittens", LoadOrNull<GameObject>(PrefabsItemsDir + "/PF_Mittens.prefab")),
					new WorldItemPrefabCatalog.EntryDraft("bedroll", LoadOrNull<GameObject>(PrefabsItemsDir + "/PF_Badroll_Rolled.prefab")),
					new WorldItemPrefabCatalog.EntryDraft("bandage", bandagePrefab),
					new WorldItemPrefabCatalog.EntryDraft("chocolate", chocolatePrefab),
					new WorldItemPrefabCatalog.EntryDraft("rope", ropePrefab),
				});

				EditorUtility.SetDirty(prefabCatalog);
				log.Add("world item prefab catalog updated");
			}

			// ---------- 4) Item catalog: append new items ----------
			ItemCatalog itemCatalog =
				LoadOrCreateAsset<ItemCatalog>(ItemsContentDir + "/ItemCatalog.asset");

			if (itemCatalog != null)
			{
				var itemIds = new List<string>();
				foreach (ItemDefinition item in itemCatalog.Items)
				{
					if (item != null)
					{
						itemIds.Add(item.Id);
					}
				}

				var allItems = new List<ItemDefinition>(itemCatalog.Items);
				AppendIfMissing(allItems, itemIds, bandage, "bandage", log);
				AppendIfMissing(allItems, itemIds, chocolate, "chocolate", log);
				AppendIfMissing(allItems, itemIds, rope, "rope", log);

				itemCatalog.EditorSetItems(allItems.ToArray());
				log.Add($"item catalog now has {allItems.Count} items");
			}

			// ---------- 5) Loot tables ----------
			ContainerLootTable crateLoot = LoadOrCreateAsset<ContainerLootTable>(
				ContainersContentDir + "/CAT_Loot_Crate.asset");
			ContainerLootTable cabinetLoot = LoadOrCreateAsset<ContainerLootTable>(
				ContainersContentDir + "/CAT_Loot_Cabinet.asset");
			ContainerLootTable toolboxLoot = LoadOrCreateAsset<ContainerLootTable>(
				ContainersContentDir + "/CAT_Loot_Toolbox.asset");

			SerializedObject crateSo = new SerializedObject(crateLoot);
			WriteLootEntries(crateSo, new[]
			{
				new LootEntry(stickOrError(), 1, 3, 0.8f),
				new LootEntry(rope, 1, 1, 0.35f),
				new LootEntry(bandage, 1, 2, 0.4f),
				new LootEntry(chocolate, 1, 1, 0.3f),
			});
			log.Add("crate loot table written");

			SerializedObject cabinetSo = new SerializedObject(cabinetLoot);
			WriteLootEntries(cabinetSo, new[]
			{
				new LootEntry(LoadOrNull<ItemDefinition>(ItemsContentDir + "/ITM_Apple.asset"), 1, 2, 0.7f),
				new LootEntry(LoadOrNull<ItemDefinition>(ItemsContentDir + "/ITM_Soda.asset"), 1, 1, 0.6f),
				new LootEntry(LoadOrNull<ItemDefinition>(ItemsContentDir + "/ITM_Water.asset"), 1, 1, 0.4f),
				new LootEntry(chocolate, 1, 1, 0.25f),
			});
			log.Add("cabinet loot table written");

			SerializedObject toolboxSo = new SerializedObject(toolboxLoot);
			WriteLootEntries(toolboxSo, new[]
			{
				new LootEntry(rope, 1, 1, 0.5f),
				new LootEntry(bandage, 1, 1, 0.3f),
				new LootEntry(stickOrError(), 1, 2, 0.4f),
			});
			log.Add("toolbox loot table written");

			AssetDatabase.SaveAssets();

			// ---------- 6) Container prefabs ----------
			GameObject cratePrefab = CreateContainerPrefab(
				ContainersPrefabDir + "/PF_Container_Crate.prefab",
				"PF_Container_Crate",
				new Color32(122, 88, 55, 255),
				new Vector3(0.8f, 0.6f, 0.6f),
				crateLoot,
				log);

			GameObject cabinetPrefab = CreateContainerPrefab(
				ContainersPrefabDir + "/PF_Container_Cabinet.prefab",
				"PF_Container_Cabinet",
				new Color32(120, 126, 130, 255),
				new Vector3(0.6f, 1.1f, 0.55f),
				cabinetLoot,
				log);

			GameObject toolboxPrefab = CreateContainerPrefab(
				ContainersPrefabDir + "/PF_Container_Toolbox.prefab",
				"PF_Container_Toolbox",
				new Color32(150, 62, 48, 255),
				new Vector3(0.7f, 0.35f, 0.45f),
				toolboxLoot,
				log);

			// ---------- 7) Scene: container window + container instances ----------
			ConfigureScene(cratePrefab, cabinetPrefab, toolboxPrefab, log);

			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();

			Debug.Log("[Iteration1ContentTool] Done:\n" + string.Join("\n", log.ToArray()));
		}

		private static ItemDefinition stickOrError()
		{
			return LoadOrNull<ItemDefinition>(ItemsContentDir + "/ITM_Stick.asset");
		}

		private static void AppendIfMissing(
			List<ItemDefinition> allItems,
			List<string> existingIds,
			ItemDefinition item,
			string itemId,
			List<string> log)
		{
			if (item == null)
			{
				return;
			}

			if (existingIds.Contains(itemId))
			{
				log.Add($"item '{itemId}' already in catalog");
				return;
			}

			allItems.Add(item);
			log.Add($"item '{itemId}' added to catalog");
		}

		private static T LoadOrCreateAsset<T>(string path) where T : ScriptableObject
		{
			T asset = AssetDatabase.LoadAssetAtPath<T>(path);

			if (asset != null)
			{
				return asset;
			}

			EnsureFolder(path);

			asset = ScriptableObject.CreateInstance<T>();
			AssetDatabase.CreateAsset(asset, path);
			return asset;
		}

		private static T LoadOrNull<T>(string path) where T : Object
		{
			return AssetDatabase.LoadAssetAtPath<T>(path);
		}

		private static void EnsureFolder(string assetPath)
		{
			string directory = System.IO.Path.GetDirectoryName(assetPath)?.Replace('\\', '/');

			if (string.IsNullOrEmpty(directory))
			{
				return;
			}

			string[] segments = directory.Split('/');
			string current = segments[0];

			for (int i = 1; i < segments.Length; i++)
			{
				string next = current + "/" + segments[i];

				if (!AssetDatabase.IsValidFolder(next))
				{
					AssetDatabase.CreateFolder(current, segments[i]);
				}

				current = next;
			}
		}

		private static void SetLocString(SerializedProperty property, string tableGuid, long keyId)
		{
			property.FindPropertyRelative("m_TableReference")
				.FindPropertyRelative("m_TableCollectionName")
				.stringValue = "GUID:" + tableGuid;

			property.FindPropertyRelative("m_TableEntryReference")
				.FindPropertyRelative("m_KeyId")
				.longValue = keyId;
		}

		private static void SetIconRef(SerializedProperty iconRefProperty, string iconName)
		{
			string texturePath = IconsDir + "/" + iconName + ".png";
			string guid = AssetDatabase.AssetPathToGUID(texturePath);

			if (string.IsNullOrEmpty(guid))
			{
				Debug.LogWarning($"Icon not found: {texturePath}");
				return;
			}

			// Make sure the sprite is addressable (same group as existing item icons).
			AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;

			if (settings != null && settings.FindAssetEntry(guid) == null)
			{
				AddressableAssetGroup group = null;

				string appleIconPath = IconsDir + "/Apple.png";
				string appleGuid = AssetDatabase.AssetPathToGUID(appleIconPath);

				if (!string.IsNullOrEmpty(appleGuid))
				{
					AddressableAssetEntry existing = settings.FindAssetEntry(appleGuid);
					group = existing != null ? existing.parentGroup : null;
				}

				settings.CreateOrMoveEntry(guid, group != null ? group : settings.DefaultGroup);
			}

			iconRefProperty.FindPropertyRelative("m_AssetGUID").stringValue = guid;
		}

		private static ItemDefinition CreateBaseItem(
			string assetPath,
			string id,
			string itemsTableGuid,
			Dictionary<string, long> keyIds,
			ItemCategory category,
			float weight,
			int maxStackSize,
			string iconName,
			List<string> log)
		{
			ItemDefinition existing = AssetDatabase.LoadAssetAtPath<ItemDefinition>(assetPath);

			if (existing != null)
			{
				log.Add($"item '{id}' already exists");
				return existing;
			}

			ItemDefinition item = ScriptableObject.CreateInstance<ItemDefinition>();
			SerializedObject so = new SerializedObject(item);

			so.FindProperty("_id").stringValue = id;
			SetLocString(so.FindProperty("_displayName"), itemsTableGuid, keyIds[id]);
			SetLocString(so.FindProperty("_description"), itemsTableGuid, keyIds[id + "_desc"]);
			so.FindProperty("_category").enumValueIndex = CategoryToEnumIndex(category);
			so.FindProperty("_weight").floatValue = weight;
			so.FindProperty("_isStackable").boolValue = true;
			so.FindProperty("_maxStackSize").intValue = maxStackSize;
			so.FindProperty("_useKind").enumValueIndex = 0;
			SetIconRef(so.FindProperty("_iconReference"), iconName);

			so.ApplyModifiedPropertiesWithoutUndo();
			AssetDatabase.CreateAsset(item, assetPath);
			log.Add($"item '{id}' created");

			return item;
		}

		private static ConsumableItemDefinition CreateConsumableItem(
			string assetPath,
			string id,
			string itemsTableGuid,
			Dictionary<string, long> keyIds,
			ItemCategory category,
			float weight,
			int maxStackSize,
			float hungerRestore,
			float fatigueRestore,
			float conditionRestore,
			string iconName,
			List<string> log)
		{
			ConsumableItemDefinition existing =
				AssetDatabase.LoadAssetAtPath<ConsumableItemDefinition>(assetPath);

			if (existing != null)
			{
				log.Add($"item '{id}' already exists");
				return existing;
			}

			ConsumableItemDefinition item =
				ScriptableObject.CreateInstance<ConsumableItemDefinition>();
			SerializedObject so = new SerializedObject(item);

			so.FindProperty("_id").stringValue = id;
			SetLocString(so.FindProperty("_displayName"), itemsTableGuid, keyIds[id]);
			SetLocString(so.FindProperty("_description"), itemsTableGuid, keyIds[id + "_desc"]);
			so.FindProperty("_category").enumValueIndex = CategoryToEnumIndex(category);
			so.FindProperty("_weight").floatValue = weight;
			so.FindProperty("_isStackable").boolValue = true;
			so.FindProperty("_maxStackSize").intValue = maxStackSize;
			so.FindProperty("_useKind").enumValueIndex = 1;
			so.FindProperty("_hungerRestore").floatValue = hungerRestore;
			so.FindProperty("_fatigueRestore").floatValue = fatigueRestore;
			so.FindProperty("_conditionRestore").floatValue = conditionRestore;
			SetIconRef(so.FindProperty("_iconReference"), iconName);

			so.ApplyModifiedPropertiesWithoutUndo();
			AssetDatabase.CreateAsset(item, assetPath);
			log.Add($"item '{id}' created");

			return item;
		}

		private static int CategoryToEnumIndex(ItemCategory category)
		{
			switch (category)
			{
				case ItemCategory.Food: return 1;
				case ItemCategory.Water: return 2;
				case ItemCategory.Medicine: return 3;
				case ItemCategory.Tool: return 4;
				case ItemCategory.Weapon: return 5;
				case ItemCategory.Clothing: return 6;
				case ItemCategory.Fuel: return 7;
				case ItemCategory.Material: return 8;
				default: return 9;
			}
		}

		private static GameObject CreateWorldItemPrefab(
			GameObject basePrefab,
			string assetPath,
			ItemDefinition definition,
			List<string> log)
		{
			GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);

			if (existing != null)
			{
				log.Add($"world prefab '{assetPath}' already exists");
				return existing;
			}

			if (basePrefab == null || definition == null)
			{
				log.Add($"cannot create '{assetPath}': base prefab or definition missing");
				return null;
			}

			GameObject contents = (GameObject)PrefabUtility.InstantiatePrefab(basePrefab);
			SerializedObject so = new SerializedObject(contents.GetComponent<WorldItemActor>());
			so.FindProperty("_definition").objectReferenceValue = definition;
			so.FindProperty("_amount").intValue = 1;
			so.ApplyModifiedPropertiesWithoutUndo();

			GameObject saved = PrefabUtility.SaveAsPrefabAsset(contents, assetPath);
			Object.DestroyImmediate(contents);

			log.Add($"world prefab '{assetPath}' created");
			return saved;
		}

		private struct LootEntry
		{
			public ItemDefinition Item;
			public int Min;
			public int Max;
			public float Chance;

			public LootEntry(ItemDefinition item, int min, int max, float chance)
			{
				Item = item;
				Min = min;
				Max = max;
				Chance = chance;
			}
		}

		private static void WriteLootEntries(SerializedObject so, LootEntry[] entries)
		{
			SerializedProperty array = so.FindProperty("_entries");
			array.ClearArray();

			for (int i = 0; i < entries.Length; i++)
			{
				array.InsertArrayElementAtIndex(i);
				SerializedProperty element = array.GetArrayElementAtIndex(i);

				element.FindPropertyRelative("_item").objectReferenceValue = entries[i].Item;
				element.FindPropertyRelative("_minAmount").intValue = entries[i].Min;
				element.FindPropertyRelative("_maxAmount").intValue = entries[i].Max;
				element.FindPropertyRelative("_dropChance").floatValue = entries[i].Chance;
			}

			so.ApplyModifiedPropertiesWithoutUndo();
			EditorUtility.SetDirty(so.targetObject);
		}

		private static GameObject CreateContainerPrefab(
			string assetPath,
			string name,
			Color32 color,
			Vector3 size,
			ContainerLootTable lootTable,
			List<string> log)
		{
			GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);

			if (existing != null)
			{
				log.Add($"container prefab '{assetPath}' already exists");
				return existing;
			}

			EnsureFolder(assetPath);

			GameObject root = new GameObject(name);

			WorldContainerActor actor = root.AddComponent<WorldContainerActor>();
			SerializedObject actorSo = new SerializedObject(actor);
			actorSo.FindProperty("_lootTable").objectReferenceValue = lootTable;
			actorSo.FindProperty("_interactionText").stringValue = string.Empty;
			actorSo.FindProperty("_maxInteractionDistance").floatValue = 3.5f;
			actorSo.ApplyModifiedPropertiesWithoutUndo();

			root.AddComponent<PersistentWorldEntity>();

			Shader litShader = Shader.Find("HDRP/Lit");
			string materialDir = ContainersPrefabDir + "/Materials";
			EnsureFolder(materialDir + "/placeholder.asset");

			string materialPath = materialDir + "/MAT_" + name + ".mat";
			Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);

			if (material == null)
			{
				material = new Material(litShader != null ? litShader : Shader.Find("Standard"));
				material.color = color;
				AssetDatabase.CreateAsset(material, materialPath);
			}

			GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
			body.name = "Body";
			body.transform.SetParent(root.transform, false);
			body.transform.localPosition = new Vector3(0f, size.y * 0.5f, 0f);
			body.transform.localScale = size;
			body.GetComponent<MeshRenderer>().sharedMaterial = material;

			GameObject lid = GameObject.CreatePrimitive(PrimitiveType.Cube);
			lid.name = "Lid";
			lid.transform.SetParent(root.transform, false);
			lid.transform.localPosition = new Vector3(0f, size.y + size.y * 0.06f, 0f);
			lid.transform.localScale = new Vector3(size.x * 1.05f, size.y * 0.12f, size.z * 1.05f);
			lid.GetComponent<MeshRenderer>().sharedMaterial = material;

			GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, assetPath);
			Object.DestroyImmediate(root);

			log.Add($"container prefab '{assetPath}' created");
			return saved;
		}

		private static void ConfigureScene(
			GameObject cratePrefab,
			GameObject cabinetPrefab,
			GameObject toolboxPrefab,
			List<string> log)
		{
			var scene = EditorSceneManager.OpenScene(
				WorldScenePath,
				OpenSceneMode.Single);

			// --- Container window GO ---
			GameObject campfireWindow = GameObject.Find("CampfireWindow");
			GameObject containerWindow = GameObject.Find("ContainerWindow");

			if (containerWindow == null)
			{
				containerWindow = new GameObject("ContainerWindow");

				if (campfireWindow != null)
				{
					containerWindow.transform.SetParent(campfireWindow.transform.parent, false);
				}

				UIDocument document = containerWindow.AddComponent<UIDocument>();

				if (campfireWindow != null)
				{
					UIDocument campfireDocument = campfireWindow.GetComponent<UIDocument>();

					if (campfireDocument != null)
					{
						document.panelSettings = campfireDocument.panelSettings;
					}
				}

				VisualTreeAsset uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
					"Assets/_Project/Runtime/UI/Toolkit/UXML_USS/Inventory/ContainerWindow.uxml");
				document.visualTreeAsset = uxml;

				MonoScript viewScript = (MonoScript)AssetDatabase.LoadAssetAtPath(
					"Assets/_Project/Runtime/UI/Inventory/ContainerWindowView.cs",
					typeof(MonoScript));
				System.Type viewType = viewScript != null ? viewScript.GetClass() : null;

				if (viewType != null)
				{
					containerWindow.AddComponent(viewType);
					log.Add("ContainerWindowView added to scene window");
				}
				else
				{
					log.Add("ERROR: ContainerWindowView class not found");
				}

				log.Add("ContainerWindow GO created");
			}

			// --- Wire WorldUIRoot.ContainerWindow ---
			GameObject uiRootObject = GameObject.Find("WorldUI");

			if (uiRootObject == null && campfireWindow != null)
			{
				uiRootObject = campfireWindow.transform.parent != null
					? campfireWindow.transform.parent.gameObject
					: null;
			}

			if (uiRootObject != null)
			{
				MonoScript uiRootScript = (MonoScript)AssetDatabase.LoadAssetAtPath(
					"Assets/_Project/Runtime/UI/World/WorldUIRoot.cs",
					typeof(MonoScript));
				System.Type uiRootType = uiRootScript != null ? uiRootScript.GetClass() : null;

				Component uiRoot = uiRootType != null
					? uiRootObject.GetComponent(uiRootType)
					: null;

				if (uiRoot != null)
				{
					SerializedObject uiRootSo = new SerializedObject(uiRoot);
					SerializedProperty containerWindowProperty =
						uiRootSo.FindProperty("<ContainerWindow>k__BackingField");

					if (containerWindowProperty != null)
					{
						containerWindowProperty.objectReferenceValue =
							containerWindow.GetComponent("ContainerWindowView");

						uiRootSo.ApplyModifiedPropertiesWithoutUndo();
						log.Add("WorldUIRoot.ContainerWindow wired");
					}
					else
					{
						log.Add("ERROR: WorldUIRoot backing field not found");
					}
				}
				else
				{
					log.Add("ERROR: WorldUIRoot component not found on WorldUI");
				}
			}
			else
			{
				log.Add("ERROR: WorldUI root object not found");
			}

			// --- Place containers near player spawn points ---
			if (cratePrefab != null && cabinetPrefab != null && toolboxPrefab != null)
			{
				Transform itemsRoot = null;
				GameObject itemsObject = GameObject.Find("Items");

				if (itemsObject != null)
				{
					itemsRoot = itemsObject.transform;
				}

				var spawnAnchors = new List<Vector3>();
				GameObject spawnPoints = GameObject.Find("PlayerSpawnPoints");

				if (spawnPoints != null)
				{
					foreach (Transform child in spawnPoints.transform)
					{
						spawnAnchors.Add(child.position);
					}
				}

				if (spawnAnchors.Count == 0)
				{
					spawnAnchors.Add(Vector3.zero);
				}

				int placedCount = 0;

				var placements = new[]
				{
					new Placement(cratePrefab, 0, 3.5f, 1.5f),
					new Placement(cratePrefab, 0, -4.0f, 2.5f),
					new Placement(cratePrefab, 1, 5.0f, -2.0f),
					new Placement(cabinetPrefab, 0, 2.0f, 4.5f),
					new Placement(cabinetPrefab, 1, -2.5f, -4.5f),
					new Placement(cabinetPrefab, 2, 6.5f, 3.0f),
					new Placement(toolboxPrefab, 0, -5.5f, 1.0f),
					new Placement(toolboxPrefab, 1, 1.0f, -6.0f),
				};

				foreach (Placement placement in placements)
				{
					Vector3 anchor = spawnAnchors[placement.AnchorIndex % spawnAnchors.Count];
					Vector3 candidate = anchor + new Vector3(placement.OffsetX, 0f, placement.OffsetZ);

					Vector3 position = candidate;

					if (Physics.Raycast(candidate + Vector3.up * 50f, Vector3.down, out RaycastHit hit, 200f))
					{
						position = hit.point;
					}

					GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(placement.Prefab);
					instance.transform.position = position;
					instance.transform.rotation = Quaternion.Euler(0f, placement.OffsetX * 37f, 0f);

					PersistentWorldEntity entity = instance.GetComponent<PersistentWorldEntity>();

					if (entity != null)
					{
						entity.EditorConfigureAsSceneObject();
					}

					if (itemsRoot != null)
					{
						instance.transform.SetParent(itemsRoot, true);
					}

					placedCount++;
				}

				log.Add($"containers placed in scene: {placedCount}");
			}

			EditorSceneManager.MarkSceneDirty(scene);
			EditorSceneManager.SaveOpenScenes();
			log.Add("World scene saved");
		}

		private struct Placement
		{
			public GameObject Prefab;
			public int AnchorIndex;
			public float OffsetX;
			public float OffsetZ;

			public Placement(GameObject prefab, int anchorIndex, float offsetX, float offsetZ)
			{
				Prefab = prefab;
				AnchorIndex = anchorIndex;
				OffsetX = offsetX;
				OffsetZ = offsetZ;
			}
		}
	}
}
