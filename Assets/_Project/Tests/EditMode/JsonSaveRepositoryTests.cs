using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using TLN.Application.Saves;
using TLN.Infrastructure.Saves;

namespace TLN.Tests.EditMode
{
	public sealed class JsonSaveRepositoryTests
	{
		private string _directory;
		private JsonSaveRepository _repository;

		[SetUp]
		public void SetUp()
		{
			_directory = Path.Combine(
				Path.GetTempPath(),
				"tln_save_test_" + Guid.NewGuid().ToString("N")
			);
			_repository = new JsonSaveRepository(_directory);
		}

		[TearDown]
		public void TearDown()
		{
			if (Directory.Exists(_directory))
			{
				Directory.Delete(_directory, true);
			}
		}

		[Test]
		public void SaveThenLoad_PreservesAllFields()
		{
			GameSaveData saved = CreateFilledData(1);

			_repository.Save(saved);
			GameSaveData loaded = _repository.Load(1);

			Assert.That(loaded, Is.Not.Null);
			Assert.That(loaded.version, Is.EqualTo(GameSaveData.CurrentVersion));
			Assert.That(loaded.slotId, Is.EqualTo(1));
			Assert.That(loaded.sceneName, Is.EqualTo("World"));
			Assert.That(loaded.saveReason, Is.EqualTo("manual"));
			Assert.That(loaded.time.totalMinutes, Is.EqualTo(1440));
			Assert.That(loaded.player.position.x, Is.EqualTo(1.5f).Within(0.0001f));

			Assert.That(loaded.survival.hunger, Is.EqualTo(42f).Within(0.0001f));
			Assert.That(loaded.survival.condition, Is.EqualTo(80f).Within(0.0001f));

			Assert.That(loaded.inventory.items, Has.Count.EqualTo(2));
			Assert.That(loaded.inventory.items[0].itemId, Is.EqualTo("apple"));
			Assert.That(loaded.inventory.items[0].amount, Is.EqualTo(3));
			Assert.That(loaded.inventory.items[1].itemId, Is.EqualTo("stick"));

			Assert.That(loaded.world.entities, Has.Count.EqualTo(1));
			Assert.That(loaded.world.entities[0].id, Is.EqualTo("campfire_1"));
			Assert.That(loaded.world.entities[0].prefabId, Is.EqualTo("campfire"));
			Assert.That(loaded.world.entities[0].position.ToVector3(), Is.EqualTo(new UnityEngine.Vector3(10f, 0f, -5f)));
			Assert.That(loaded.world.destroyedSceneEntityIds, Has.Count.EqualTo(1));
		}

		[Test]
		public void Load_LegacySaveWithoutFields_NormalizesAllData()
		{
			Directory.CreateDirectory(_directory);
			string path = Path.Combine(_directory, "slot_2.json");
			File.WriteAllText(path, "{\"sceneName\":\"LegacyScene\"}");

			GameSaveData loaded = _repository.Load(2);

			Assert.That(loaded, Is.Not.Null);
			Assert.That(loaded.version, Is.EqualTo(GameSaveData.CurrentVersion));
			Assert.That(loaded.sceneName, Is.EqualTo("LegacyScene"));
			Assert.That(loaded.savedAtUtc, Is.EqualTo(string.Empty));
			Assert.That(loaded.world, Is.Not.Null);
			Assert.That(loaded.world.entities, Is.Not.Null);
			Assert.That(loaded.inventory, Is.Not.Null);
			Assert.That(loaded.inventory.items, Is.Not.Null);
		}

		[Test]
		public void Delete_RemovesSlotFile()
		{
			_repository.Save(CreateFilledData(3));
			Assert.That(_repository.SaveExists(3), Is.True);

			Assert.That(_repository.Delete(3), Is.True);
			Assert.That(_repository.SaveExists(3), Is.False);
			Assert.That(_repository.Load(3), Is.Null);
		}

		private static GameSaveData CreateFilledData(int slotId)
		{
			return new GameSaveData
			{
				slotId = slotId,
				savedAtUtc = "2026-09-13T12:00:00.0000000Z",
				sceneName = "World",
				saveReason = "manual",
				time = new GameTimeSaveData { totalMinutes = 1440 },
				player = new PlayerSaveData
				{
					position = new Vector3SaveData(1.5f, 2f, 3f),
					rotation = new QuaternionSaveData(0f, 0f, 0f, 1f)
				},
				survival = new SurvivalSaveData
				{
					hunger = 42f,
					thirst = 50f,
					fatigue = 10f,
					cold = 5f,
					condition = 80f
				},
				inventory = new InventorySaveData
				{
					items = new List<InventoryItemSaveData>
					{
						new() { itemId = "apple", amount = 3 },
						new() { itemId = "stick", amount = 1 }
					}
				},
				world = new WorldSaveData
				{
					destroyedSceneEntityIds = new List<string> { "tree_1" },
					entities = new List<WorldEntitySaveData>
					{
						new()
						{
							id = "campfire_1",
							prefabId = "campfire",
							isSceneObject = false,
							position = new Vector3SaveData(10f, 0f, -5f),
							rotation = new QuaternionSaveData(0f, 0f, 0f, 1f),
							components = new List<WorldComponentSaveData>
							{
								new() { typeId = "campfire", json = "{\"fuel\":10}" }
							}
						}
					}
				}
			};
		}
	}
}
