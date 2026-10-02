using System.Collections.Generic;
using TLN.Gameplay.Campfire;
using TLN.Gameplay.Inventory;
using TLN.Gameplay.Saves;
using TLN.Gameplay.World.Streaming;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Localization.Tables;
using UnityEngine.SceneManagement;

namespace TLN.Editor.Tools
{
	/// <summary>
	/// Iteration 2: handcrafted region locations. Structures are built with
	/// ground-height anchoring and overlap-safe joints (no gable seams).
	/// Streaming runs for real: locations load/unload by distance, no KeepLoaded.
	/// </summary>
	public static class Iteration2RegionTool
	{
		private const string WorldScenePath = "Assets/_Project/Scenes/World/World.unity";
		private const string FrozenLakeScenePath = "Assets/_Project/Scenes/World/World_FrozenLake.unity";
		private const string RangerCabinScenePath = "Assets/_Project/Scenes/World/World_RangerCabin.unity";
		private const string RailDepotScenePath = "Assets/_Project/Scenes/World/World_RailDepot.unity";

		private const string MaterialsDir = "Assets/_Project/Content/World/Building/Materials";
		private const string CabinDataDir = "Assets/_Project/Scenes/World/World_RangerCabin";

		private const string HdrpTerrainMaterialPath =
			"Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipelineResources/Material/DefaultHDTerrainMaterial.mat";

		private static readonly Vector3 CabinCenter = new Vector3(-55f, 0f, 85f);
		private static readonly Vector3 DepotCenter = new Vector3(100f, 0f, 93f);
		private static readonly Vector3 FishermanCenter = new Vector3(147f, 0f, 79f);

		private static Material _plankLight;
		private static Material _plankDark;
		private static Material _roofWood;
		private static Material _metalDark;

		[MenuItem("TLN/Tools/Iteration 2: Rebuild Structures (v2)")]
		public static void RebuildStructures()
		{
			var log = new List<string>();

			EnsureMaterials(log);
			AddLocationStrings(log);

			// ---------- Ranger Cabin sub-scene ----------
			Scene cabinScene = EditorSceneManager.OpenScene(RangerCabinScenePath, OpenSceneMode.Single);
			DestroyIfExists(cabinScene, "Cabin_Structure");
			DestroyIfExists(cabinScene, "Shed_Structure");
			GameObject cabinRoot = NewSceneChild(cabinScene, "Cabin_Structure");
			BuildCabinV2(cabinRoot.transform, CabinCenter, log);
			GameObject shedRoot = NewSceneChild(cabinScene, "Shed_Structure");
			BuildShedV2(shedRoot.transform, CabinCenter + new Vector3(5.6f, 0f, -0.5f), log);
			SceneCleanup(cabinScene, log);

			// ---------- Rail Depot sub-scene ----------
			Scene depotScene = EditorSceneManager.OpenScene(RailDepotScenePath, OpenSceneMode.Single);
			DestroyIfExists(depotScene, "Depot_Structure");
			GameObject depotRoot = NewSceneChild(depotScene, "Depot_Structure");
			BuildDepotV2(depotRoot.transform, DepotCenter, log);
			SceneCleanup(depotScene, log);

			// ---------- Frozen Lake: replace tents with a clean fishing spot ----------
			Scene lakeScene = EditorSceneManager.OpenScene(FrozenLakeScenePath, OpenSceneMode.Additive);
			GameObject oldCamp = GameObject.Find("FishermanCamp");

			if (oldCamp != null)
			{
				Object.DestroyImmediate(oldCamp);
			}

			GameObject campRoot = new GameObject("FishermanCamp");
			SceneManager.MoveGameObjectToScene(campRoot, lakeScene);
			campRoot.transform.position = FishermanCenter;
			BuildFishSpotV2(campRoot.transform, log);
			SceneCleanup(lakeScene, log);

			// ---------- World scene: streaming mode ----------
			Scene worldScene = EditorSceneManager.OpenScene(WorldScenePath, OpenSceneMode.Single);
			SetStreamingMode(worldScene, log);
			WireStreamingController(worldScene, log);
			SceneCleanup(worldScene, log);

			Debug.Log("[Iteration2RegionTool] Rebuild done:\n" + string.Join("\n", log.ToArray()));
		}

		[MenuItem("TLN/Tools/Iteration 2: Remove Stray World Props")]
		public static void RemoveStrayWorldProps()
		{
			Scene worldScene = EditorSceneManager.OpenScene(WorldScenePath, OpenSceneMode.Single);
			GameObject stray = GameObject.Find("FishermanCamp");

			if (stray != null)
			{
				Object.DestroyImmediate(stray);
				EditorSceneManager.MarkSceneDirty(worldScene);
				EditorSceneManager.SaveOpenScenes();
				Debug.Log("[Iteration2RegionTool] Stray FishermanCamp removed from World");
			}
			else
			{
				Debug.Log("[Iteration2RegionTool] No stray FishermanCamp in World");
			}
		}

		// ------------------------------------------------------------------
		// Location strings — unique names, not copied from The Long Dark
		// ------------------------------------------------------------------

		private static void AddLocationStrings(List<string> log)
		{
			const string sharedPath = "Assets/_Project/Localization/StringTables/UI/UI Shared Data.asset";
			SharedTableData shared = AssetDatabase.LoadAssetAtPath<SharedTableData>(sharedPath);

			if (shared == null)
			{
				log.Add("ERROR: UI shared table not found");
				return;
			}

			string[] localeFiles = { "UI_en", "UI_ru", "UI_uk" };
			string[] localeCodes = { "en", "ru", "uk" };
			var tables = new Dictionary<string, StringTable>();

			for (int i = 0; i < localeFiles.Length; i++)
			{
				StringTable table = AssetDatabase.LoadAssetAtPath<StringTable>(
					"Assets/_Project/Localization/StringTables/UI/" + localeFiles[i] + ".asset");

				if (table != null)
				{
					tables[localeCodes[i]] = table;
				}
			}

			var entries = new Dictionary<string, string[]>
			{
				{ "location.ranger_cabin", new[] { "FORESTER'S HUT", "ИЗБА ЛЕСНИКА", "ХАТА ЛІСНИКА" } },
				{ "location.rail_depot", new[] { "ABANDONED DEPOT", "ЗАБРОШЕННОЕ ДЕПО", "ЗАБУТЄ ДЕПО" } },
				{ "location.fisherman_camp", new[] { "ICE FISHING SPOT", "РЫБНЫЕ ЛУНКИ", "РИБНІ ЛУНКИ" } },
				{ "location.frozen_lake", new[] { "GOOSE LAKE", "ГУСИНОЕ ОЗЕРО", "ГУСИНЕ ОЗЕРО" } },
				{ "location.spawn_camp", new[] { "ZERO POINT", "ТОЧКА ОТСЧЁТА", "ТОЧКА ВІДЛІКУ" } },
			};

			foreach (KeyValuePair<string, string[]> pair in entries)
			{
				long keyId = 0;
				bool found = false;

				foreach (SharedTableData.SharedTableEntry entry in shared.Entries)
				{
					if (entry.Key == pair.Key)
					{
						keyId = entry.Id;
						found = true;
						break;
					}
				}

				if (!found)
				{
					keyId = shared.AddKey(pair.Key).Id;
				}

				for (int i = 0; i < localeCodes.Length; i++)
				{
					if (tables.TryGetValue(localeCodes[i], out StringTable table) && table != null)
					{
						table.AddEntry(keyId, pair.Value[i]);
						EditorUtility.SetDirty(table);
					}
				}
			}

			EditorUtility.SetDirty(shared);
			AssetDatabase.SaveAssets();
			log.Add("location strings ensured: " + entries.Count);
		}

		// ------------------------------------------------------------------
		// Structures v2 — anchored to ground height, overlap-safe
		// ------------------------------------------------------------------

		private static float GroundAt(Vector3 position)
		{
			if (Physics.Raycast(position + Vector3.up * 30f, Vector3.down, out RaycastHit hit, 80f))
			{
				return hit.point.y;
			}

			return position.y;
		}

		private static void BuildCabinV2(Transform parent, Vector3 center, List<string> log)
		{
			float g = GroundAt(center + Vector3.up * 10f);
			Quaternion id = Quaternion.identity;
			Vector3 c = new Vector3(center.x, g, center.z);

			// Foundation slab
			Box(parent, "Foundation", c + Vector3.up * 0.16f, new Vector3(7.4f, 0.32f, 6.4f), _plankDark, id);

			float wallBase = g + 0.32f;
			float wallTop = wallBase + 2.6f;

			// Solid back / side walls (front has the door gap)
			Box(parent, "Wall_Back", c + new Vector3(0f, (wallBase + wallTop) * 0.5f - g, -3.1f), new Vector3(6.8f, 2.6f, 0.22f), _plankLight, id);
			Box(parent, "Wall_Left", c + new Vector3(-3.3f, (wallBase + wallTop) * 0.5f - g, 0f), new Vector3(0.22f, 2.6f, 6.0f), _plankLight, id);
			Box(parent, "Wall_Right", c + new Vector3(3.3f, (wallBase + wallTop) * 0.5f - g, 0f), new Vector3(0.22f, 2.6f, 6.0f), _plankLight, id);

			// Front wall: two segments + lintel over a centered 1.1 x 2.0 door
			Box(parent, "Wall_Front_Left", c + new Vector3(-2.35f, (wallBase + wallTop) * 0.5f - g, 3.1f), new Vector3(2.1f, 2.6f, 0.22f), _plankLight, id);
			Box(parent, "Wall_Front_Right", c + new Vector3(2.35f, (wallBase + wallTop) * 0.5f - g, 3.1f), new Vector3(2.1f, 2.6f, 0.22f), _plankLight, id);
			Box(parent, "Wall_Front_Lintel", c + new Vector3(0f, wallTop - 0.3f - g, 3.1f), new Vector3(1.1f, 0.6f, 0.22f), _plankLight, id);

			// Single-pitch roof slab, wide overhang — no gable seams by design
			Box(parent, "Roof", c + new Vector3(0f, wallTop + 0.42f - g, 0f), new Vector3(8.0f, 0.16f, 7.6f), _roofWood, id);

			// Interior: stove with pipe, campfire inside it
			Box(parent, "Stove_Body", c + new Vector3(-2.4f, 0.62f, -2.2f), new Vector3(1.0f, 0.62f, 0.9f), _metalDark, id);
			Cylinder(parent, "Stove_Pipe", c + new Vector3(-2.4f, wallTop + 0.7f - g, -2.2f), new Vector3(0.16f, 1.4f, 0.16f), _metalDark, id);

			GameObject firePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
				"Assets/_Project/Prefabs/Items/PF_Campfire_Base.prefab");
			GameObject fire = (GameObject)PrefabUtility.InstantiatePrefab(firePrefab);
			fire.transform.position = c + new Vector3(-2.4f, 0.64f, -2.2f);
			fire.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
			ConfigureSceneEntity(fire);
			fire.transform.SetParent(parent, true);

			// Table + shelf
			Box(parent, "Table_Top", c + new Vector3(2.0f, 1.06f, -1.8f), new Vector3(1.7f, 0.09f, 0.95f), _plankDark, id);
			Box(parent, "Table_Leg_A", c + new Vector3(1.35f, 0.65f, -2.1f), new Vector3(0.09f, 0.78f, 0.09f), _plankDark, id);
			Box(parent, "Table_Leg_B", c + new Vector3(2.65f, 0.65f, -1.5f), new Vector3(0.09f, 0.78f, 0.09f), _plankDark, id);
			Box(parent, "Shelf", c + new Vector3(0f, 1.9f, -2.9f), new Vector3(2.0f, 0.07f, 0.42f), _plankDark, id);

			// Bed
			GameObject bedroll = AssetDatabase.LoadAssetAtPath<GameObject>(
				"Assets/_Project/Prefabs/Items/PF_Placed_Bedroll.prefab");
			if (bedroll != null)
			{
				GameObject bed = (GameObject)PrefabUtility.InstantiatePrefab(bedroll);
				bed.transform.position = c + new Vector3(0.6f, 0.35f, -2.1f);
				bed.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
				ConfigureSceneEntity(bed);
				bed.transform.SetParent(parent, true);
			}

			// Containers
			PlaceContainer(parent, "Assets/_Project/Prefabs/Containers/PF_Container_Crate.prefab",
				c + new Vector3(2.4f, 0.32f, 2.0f), 12f);
			PlaceContainer(parent, "Assets/_Project/Prefabs/Containers/PF_Container_Cabinet.prefab",
				c + new Vector3(-0.8f, 0.32f, 2.55f), 0f);

			// Porch: slab + posts + canopy, all anchored to the same ground
			Box(parent, "Porch_Floor", c + new Vector3(0.2f, 0.14f, 4.4f), new Vector3(3.4f, 0.28f, 2.0f), _plankDark, id);
			Box(parent, "Porch_Post_A", c + new Vector3(-1.1f, wallBase + 1.1f - g, 5.1f), new Vector3(0.14f, 2.2f, 0.14f), _plankDark, id);
			Box(parent, "Porch_Post_B", c + new Vector3(1.5f, wallBase + 1.1f - g, 5.1f), new Vector3(0.14f, 2.2f, 0.14f), _plankDark, id);
			Box(parent, "Porch_Canopy", c + new Vector3(0.2f, wallBase + 2.3f - g, 4.4f), new Vector3(3.6f, 0.12f, 2.3f), _roofWood, id);

			log.Add("cabin v2 built at " + center + " groundY=" + g.ToString("0.00"));
		}

		private static void BuildShedV2(Transform parent, Vector3 center, List<string> log)
		{
			float g = GroundAt(center + Vector3.up * 10f);
			Vector3 c = new Vector3(center.x, g, center.z);
			Quaternion id = Quaternion.identity;

			Box(parent, "Shed_Floor", c + Vector3.up * 0.1f, new Vector3(2.6f, 0.2f, 2.0f), _plankDark, id);
			Box(parent, "Shed_Post_FL", c + new Vector3(-1.1f, 1.15f, 0.85f), new Vector3(0.14f, 2.1f, 0.14f), _plankDark, id);
			Box(parent, "Shed_Post_FR", c + new Vector3(1.1f, 1.15f, 0.85f), new Vector3(0.14f, 2.1f, 0.14f), _plankDark, id);
			Box(parent, "Shed_Post_BL", c + new Vector3(-1.1f, 1.15f, -0.85f), new Vector3(0.14f, 2.1f, 0.14f), _plankDark, id);
			Box(parent, "Shed_Post_BR", c + new Vector3(1.1f, 1.15f, -0.85f), new Vector3(0.14f, 2.1f, 0.14f), _plankDark, id);
			Box(parent, "Shed_Roof", c + new Vector3(0f, 2.32f, 0f), new Vector3(2.9f, 0.12f, 2.3f), _roofWood, id);

			for (int i = 0; i < 6; i++)
			{
				int row = i / 3;
				int col = i % 3;
				Cylinder(parent, "Shed_Log_" + i,
					c + new Vector3(-0.65f + col * 0.42f, 0.14f + row * 0.26f, -0.35f + (row % 2) * 0.1f),
					new Vector3(0.22f, 0.55f, 0.22f), _plankDark,
					Quaternion.Euler(0f, 90f, 90f));
			}

			log.Add("shed v2 built");
		}

		private static void BuildDepotV2(Transform parent, Vector3 center, List<string> log)
		{
			float g = GroundAt(center + Vector3.up * 10f);
			Vector3 c = new Vector3(center.x, g, center.z);
			float yaw = 42f;
			Quaternion rot = Quaternion.Euler(0f, yaw, 0f);

			// Platform along the rails
			Box(parent, "Depot_Platform", c + Vector3.up * 0.26f, new Vector3(11f, 0.52f, 3.6f), _plankDark, rot);

			// Canopy: 4 posts + single slab
			Box(parent, "Depot_Post_FL", c + rot * new Vector3(-4.6f, 1.55f, 1.5f), new Vector3(0.16f, 3.1f, 0.16f), _plankDark, rot);
			Box(parent, "Depot_Post_FR", c + rot * new Vector3(4.6f, 1.55f, 1.5f), new Vector3(0.16f, 3.1f, 0.16f), _plankDark, rot);
			Box(parent, "Depot_Post_BL", c + rot * new Vector3(-4.6f, 1.55f, -1.5f), new Vector3(0.16f, 3.1f, 0.16f), _plankDark, rot);
			Box(parent, "Depot_Post_BR", c + rot * new Vector3(4.6f, 1.55f, -1.5f), new Vector3(0.16f, 3.1f, 0.16f), _plankDark, rot);
			Box(parent, "Depot_Canopy", c + rot * new Vector3(0f, 3.22f, 0f), new Vector3(11.4f, 0.14f, 4.6f), _roofWood, rot);

			// Lantern pole
			Cylinder(parent, "Lantern_Pole", c + rot * new Vector3(5.8f, 1.7f, 1.9f), new Vector3(0.1f, 3.4f, 0.1f), _metalDark, rot);
			Box(parent, "Lantern_Head", c + rot * new Vector3(5.8f, 3.5f, 1.9f), new Vector3(0.34f, 0.44f, 0.34f), _metalDark, rot);

			// Barrel + crate decor
			Cylinder(parent, "Depot_Barrel", c + rot * new Vector3(-3.6f, 0.52f, 1.2f), new Vector3(0.72f, 0.52f, 0.72f), _plankDark, rot);
			Box(parent, "Depot_Crate_A", c + rot * new Vector3(1.4f, 0.26f, 1.1f), new Vector3(0.72f, 0.52f, 0.72f), _plankLight, rot);
			Box(parent, "Depot_Crate_B", c + rot * new Vector3(1.5f, 0.78f, 1.15f), new Vector3(0.56f, 0.52f, 0.56f), _plankLight, Quaternion.Euler(0f, yaw + 22f, 0f));

			// Watchman hut — same overlap-safe pattern as the cabin
			Vector3 hut = c + rot * new Vector3(0f, 0f, 5.2f);
			float hg = g;
			Box(parent, "Hut_Floor", hut + Vector3.up * 0.14f, new Vector3(3.6f, 0.28f, 3.3f), _plankDark, rot);
			Box(parent, "Hut_Wall_Back", hut + rot * new Vector3(0f, 1.45f, -1.6f), new Vector3(3.4f, 2.3f, 0.2f), _plankLight, rot);
			Box(parent, "Hut_Wall_Left", hut + rot * new Vector3(-1.7f, 1.45f, 0f), new Vector3(0.2f, 2.3f, 3.2f), _plankLight, rot);
			Box(parent, "Hut_Wall_Right", hut + rot * new Vector3(1.7f, 1.45f, 0f), new Vector3(0.2f, 2.3f, 3.2f), _plankLight, rot);
			Box(parent, "Hut_Wall_Front_A", hut + rot * new Vector3(-1.25f, 1.45f, 1.6f), new Vector3(0.9f, 2.3f, 0.2f), _plankLight, rot);
			Box(parent, "Hut_Wall_Front_B", hut + rot * new Vector3(1.25f, 1.45f, 1.6f), new Vector3(0.9f, 2.3f, 0.2f), _plankLight, rot);
			Box(parent, "Hut_Wall_Lintel", hut + rot * new Vector3(0f, 2.1f, 1.6f), new Vector3(1.1f, 0.55f, 0.2f), _plankLight, rot);
			Box(parent, "Hut_Roof", hut + rot * new Vector3(0f, 2.85f, 0f), new Vector3(4.0f, 0.14f, 3.8f), _roofWood, rot);

			GameObject firePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
				"Assets/_Project/Prefabs/Items/PF_Campfire_Base.prefab");
			GameObject fire = (GameObject)PrefabUtility.InstantiatePrefab(firePrefab);
			fire.transform.position = hut + rot * new Vector3(-1.05f, 0.28f, -0.9f);
			fire.transform.localScale = new Vector3(0.42f, 0.42f, 0.42f);
			ConfigureSceneEntity(fire);
			fire.transform.SetParent(parent, true);

			PlaceContainer(parent, "Assets/_Project/Prefabs/Containers/PF_Container_Cabinet.prefab",
				hut + rot * new Vector3(1.15f, 0.28f, -1.15f), yaw + 180f);

			log.Add("depot v2 built at " + center + " groundY=" + hg.ToString("0.00"));
		}

		private static void BuildFishSpotV2(Transform parent, List<string> log)
		{
			// Ice holes
			Cylinder(parent, "Ice_Hole_A", parent.position + new Vector3(-1.6f, 0.02f, -0.9f), new Vector3(0.64f, 0.08f, 0.64f), _metalDark, Quaternion.identity);
			Cylinder(parent, "Ice_Hole_B", parent.position + new Vector3(1.9f, 0.02f, -1.5f), new Vector3(0.64f, 0.08f, 0.64f), _metalDark, Quaternion.identity);

			// Sled with runners
			Box(parent, "Sled_Bed", parent.position + new Vector3(0.5f, 0.16f, 2.7f), new Vector3(1.6f, 0.1f, 0.85f), _plankDark, Quaternion.Euler(0f, 12f, 0f));
			Box(parent, "Sled_Runner_A", parent.position + new Vector3(0.5f - 0.62f, 0.08f, 2.7f), new Vector3(0.1f, 0.12f, 1.05f), _plankDark, Quaternion.Euler(0f, 12f, 0f));
			Box(parent, "Sled_Runner_B", parent.position + new Vector3(0.5f + 0.62f, 0.08f, 2.7f), new Vector3(0.1f, 0.12f, 1.05f), _plankDark, Quaternion.Euler(0f, 12f, 0f));

			// Supply boxes (decor)
			Box(parent, "Fish_Box_A", parent.position + new Vector3(-0.6f, 0.18f, 0.6f), new Vector3(0.62f, 0.36f, 0.45f), _plankLight, Quaternion.Euler(0f, 24f, 0f));
			Box(parent, "Fish_Box_B", parent.position + new Vector3(-0.15f, 0.18f, 0.9f), new Vector3(0.5f, 0.36f, 0.4f), _plankLight, Quaternion.Euler(0f, -8f, 0f));

			// Loot crate
			PlaceContainer(parent, "Assets/_Project/Prefabs/Containers/PF_Container_Crate.prefab",
				FishermanCenter + new Vector3(0.7f, 0.12f, -0.3f), 30f);

			log.Add("ice fishing spot v2 built");
		}

		// ------------------------------------------------------------------
		// Streaming mode — real distance streaming, no KeepLoaded
		// ------------------------------------------------------------------

		private static void SetStreamingMode(Scene worldScene, List<string> log)
		{
			WorldStreamingController controller =
				Object.FindFirstObjectByType<WorldStreamingController>(FindObjectsInactive.Include);

			if (controller == null)
			{
				log.Add("ERROR: WorldStreamingController not found");
				return;
			}

			SerializedObject so = new SerializedObject(controller);
			SerializedProperty locations = so.FindProperty("_locations");
			locations.arraySize = 3;

			WriteLocation(locations.GetArrayElementAtIndex(0), "World_FrozenLake", new Vector3(150f, 0f, 75f), 80f, 120f, false);
			WriteLocation(locations.GetArrayElementAtIndex(1), "World_RangerCabin", CabinCenter, 70f, 110f, false);
			WriteLocation(locations.GetArrayElementAtIndex(2), "World_RailDepot", DepotCenter, 70f, 110f, false);

			so.ApplyModifiedPropertiesWithoutUndo();
			log.Add("streaming: 3 locations, distance-based (KeepLoaded off)");
		}

		private static void WriteLocation(
			SerializedProperty property,
			string sceneName,
			Vector3 center,
			float loadRadius,
			float unloadRadius,
			bool keepLoaded
		)
		{
			property.FindPropertyRelative("_sceneName").stringValue = sceneName;
			property.FindPropertyRelative("_center").vector3Value = center;
			property.FindPropertyRelative("_loadRadius").floatValue = loadRadius;
			property.FindPropertyRelative("_unloadRadius").floatValue = unloadRadius;
			property.FindPropertyRelative("_keepLoaded").boolValue = keepLoaded;
		}

		private static void WireStreamingController(Scene worldScene, List<string> log)
		{
			WorldStreamingController controller =
				Object.FindFirstObjectByType<WorldStreamingController>(FindObjectsInactive.Include);

			if (controller == null)
			{
				log.Add("ERROR: cannot wire streaming controller — not found");
				return;
			}

			MonoScript scopeScript = (MonoScript)AssetDatabase.LoadAssetAtPath(
				"Assets/_Project/Runtime/Bootstrap/WorldLifetimeScope.cs", typeof(MonoScript));
			System.Type scopeType = scopeScript != null ? scopeScript.GetClass() : null;

			if (scopeType == null)
			{
				log.Add("ERROR: WorldLifetimeScope type not found");
				return;
			}

			Component scope = FindComponentInScene(worldScene, scopeType);

			if (scope == null)
			{
				log.Add("ERROR: WorldLifetimeScope component not found in scene");
				return;
			}

			SerializedObject so = new SerializedObject(scope);
			SerializedProperty property = so.FindProperty("_worldStreamingController");

			if (property == null)
			{
				log.Add("ERROR: _worldStreamingController field not found");
				return;
			}

			property.objectReferenceValue = controller;
			so.ApplyModifiedPropertiesWithoutUndo();
			log.Add("WorldLifetimeScope._worldStreamingController wired");
		}

		private static Component FindComponentInScene(Scene scene, System.Type type)
		{
			foreach (GameObject root in scene.GetRootGameObjects())
			{
				Component[] components = root.GetComponentsInChildren(type, true);

				if (components.Length > 0)
				{
					return components[0];
				}
			}

			return null;
		}

		// ------------------------------------------------------------------
		// Helpers
		// ------------------------------------------------------------------

		private static void DestroyIfExists(Scene scene, string objectName)
		{
			foreach (GameObject root in scene.GetRootGameObjects())
			{
				if (root.name == objectName)
				{
					Object.DestroyImmediate(root);
					return;
				}
			}

			Transform nested = null;

			foreach (GameObject root in scene.GetRootGameObjects())
			{
				nested = root.transform.Find(objectName);

				if (nested != null)
				{
					Object.DestroyImmediate(nested.gameObject);
					return;
				}
			}
		}

		private static GameObject NewSceneChild(Scene scene, string name)
		{
			GameObject go = new GameObject(name);
			SceneManager.MoveGameObjectToScene(go, scene);
			return go;
		}

		private static void SceneCleanup(Scene scene, List<string> log)
		{
			EditorSceneManager.MarkSceneDirty(scene);
			EditorSceneManager.SaveScene(scene);
			log.Add("scene saved: " + scene.name);
		}

		private static void PlaceContainer(Transform parent, string prefabPath, Vector3 position, float yaw)
		{
			GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

			if (prefab == null)
			{
				Debug.LogWarning("[Iteration2RegionTool] Missing container prefab: " + prefabPath);
				return;
			}

			GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
			instance.transform.position = position;
			instance.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
			ConfigureSceneEntity(instance);
			instance.transform.SetParent(parent, true);
		}

		private static GameObject Box(
			Transform parent,
			string name,
			Vector3 position,
			Vector3 size,
			Material material,
			Quaternion rotation
		)
		{
			GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
			go.name = name;
			go.transform.position = position;
			go.transform.localScale = size;
			go.transform.rotation = rotation;

			if (material != null)
			{
				go.GetComponent<MeshRenderer>().sharedMaterial = material;
			}

			go.transform.SetParent(parent, true);
			return go;
		}

		private static GameObject Cylinder(
			Transform parent,
			string name,
			Vector3 position,
			Vector3 scale,
			Material material,
			Quaternion rotation
		)
		{
			GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
			go.name = name;
			go.transform.position = position;
			go.transform.localScale = scale;
			go.transform.rotation = rotation;

			if (material != null)
			{
				go.GetComponent<MeshRenderer>().sharedMaterial = material;
			}

			go.transform.SetParent(parent, true);
			return go;
		}

		private static void ConfigureSceneEntity(GameObject instance)
		{
			PersistentWorldEntity entity = instance.GetComponent<PersistentWorldEntity>();

			if (entity != null)
			{
				entity.EditorConfigureAsSceneObject();
			}
		}

		private static void EnsureMaterials(List<string> log)
		{
			EnsureFolder(MaterialsDir + "/x.asset");

			_plankLight = EnsureMaterial(MaterialsDir + "/MAT_Wood_Plank_Light.mat", new Color(0.62f, 0.48f, 0.34f), 0f, 0.28f, log);
			_plankDark = EnsureMaterial(MaterialsDir + "/MAT_Wood_Plank_Dark.mat", new Color(0.42f, 0.30f, 0.20f), 0f, 0.32f, log);
			_roofWood = EnsureMaterial(MaterialsDir + "/MAT_Wood_Roof.mat", new Color(0.24f, 0.17f, 0.12f), 0f, 0.38f, log);
			_metalDark = EnsureMaterial(MaterialsDir + "/MAT_Metal_Dark.mat", new Color(0.23f, 0.25f, 0.27f), 0.7f, 0.5f, log);
		}

		private static Material EnsureMaterial(
			string path,
			Color color,
			float metallic,
			float smoothness,
			List<string> log
		)
		{
			Material material = AssetDatabase.LoadAssetAtPath<Material>(path);

			if (material != null)
			{
				return material;
			}

			Shader shader = Shader.Find("HDRP/Lit");
			material = new Material(shader != null ? shader : Shader.Find("Standard"))
			{
				color = color
			};

			material.SetFloat("_Metallic", metallic);
			material.SetFloat("_Smoothness", smoothness);

			AssetDatabase.CreateAsset(material, path);
			log.Add("material created: " + path);
			return material;
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
	}
}
