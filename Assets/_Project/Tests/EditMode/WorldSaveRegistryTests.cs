using System;
using NUnit.Framework;
using TLN.Application.Saves;
using TLN.Gameplay.Inventory;
using TLN.Gameplay.Items;
using TLN.Gameplay.Saves;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TLN.Tests.EditMode
{
	public sealed class WorldSaveRegistryTests
	{
		[Test]
		public void UnloadedSceneContents_SurviveCheckpointAndSceneReload()
		{
			Scene originalActiveScene = SceneManager.GetActiveScene();
			string sceneName = "SaveTest_" + Guid.NewGuid().ToString("N");
			Scene scene = CreateTestScene(sceneName);
			ItemDefinition item = TestAssetFactory.CreateItem<ItemDefinition>("stick");
			ItemCatalog catalog = ScriptableObject.CreateInstance<ItemCatalog>();
			catalog.EditorSetItems(new[] { item });
			try
			{
				WorldSaveRegistry registry = new(null, null);
				WorldContainerActor container = CreateContainer(scene, catalog);
				container.EditorSeedContents(new() { new ItemStack(item, 7) });
				registry.CaptureScene(scene);
				EditorSceneManager.ClosePreviewScene(scene);

				WorldSaveData checkpoint = new();
				registry.Capture(checkpoint);
				Assert.That(checkpoint.entities.Exists(entity => entity.id == sceneName), Is.True);
				Assert.That(checkpoint.capturedSceneNames, Does.Contain(sceneName));

				scene = CreateTestScene(sceneName);
				container = CreateContainer(scene, catalog);
				container.EditorSeedContents(new() { new ItemStack(item, 1) });
				registry.RestoreScene(scene);
				Assert.That(container.Contents, Has.Count.EqualTo(1));
				Assert.That(container.Contents[0].Amount, Is.EqualTo(7));

				container.EditorSeedContents(new());
				registry.CaptureScene(scene);
				EditorSceneManager.ClosePreviewScene(scene);
				scene = CreateTestScene(sceneName);
				container = CreateContainer(scene, catalog);
				container.EditorSeedContents(new() { new ItemStack(item, 3) });
				registry.RestoreScene(scene);
				Assert.That(container.Contents, Is.Empty);
			}
			finally
			{
				if (scene.IsValid() && scene.isLoaded)
					EditorSceneManager.ClosePreviewScene(scene);
				if (originalActiveScene.IsValid() && originalActiveScene.isLoaded)
					SceneManager.SetActiveScene(originalActiveScene);
				UnityEngine.Object.DestroyImmediate(catalog);
				UnityEngine.Object.DestroyImmediate(item);
			}
		}

		private static Scene CreateTestScene(string sceneName)
		{
			// Preview scenes also work when the test runner has an unsaved untitled scene.
			Scene scene = EditorSceneManager.NewPreviewScene();
			scene.name = sceneName;
			return scene;
		}

		private static WorldContainerActor CreateContainer(Scene scene, ItemCatalog catalog)
		{
			GameObject gameObject = new("SavedContainer");
			SceneManager.MoveGameObjectToScene(gameObject, scene);
			PersistentWorldEntity entity = gameObject.AddComponent<PersistentWorldEntity>();
			TestAssetFactory.SetPrivateField(entity, "_id", scene.name);
			WorldContainerActor container = gameObject.AddComponent<WorldContainerActor>();
			container.Construct(null, catalog);
			return container;
		}
	}
}