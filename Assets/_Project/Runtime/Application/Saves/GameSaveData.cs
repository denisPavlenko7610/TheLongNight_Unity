using System;
using System.Collections.Generic;
using UnityEngine;

namespace TLN.Application.Saves
{
	[Serializable]
	public sealed class GameSaveData
	{
		public const int CurrentVersion = 1;

		public int version = CurrentVersion;
		public int slotId;
		public string savedAtUtc;
		public string sceneName;
		public string saveReason;

		public WorldSaveData world = new WorldSaveData();
		public PlayerSaveData player = new PlayerSaveData();
		public GameTimeSaveData time = new GameTimeSaveData();
		public SurvivalSaveData survival = new SurvivalSaveData();
		public InventorySaveData inventory = new InventorySaveData();

		public void NormalizeAfterLoad()
		{
			if (version <= 0)
			{
				version = CurrentVersion;
			}

			savedAtUtc ??= string.Empty;
			sceneName ??= string.Empty;
			saveReason ??= string.Empty;

			world ??= new WorldSaveData();
			player ??= new PlayerSaveData();
			time ??= new GameTimeSaveData();
			survival ??= new SurvivalSaveData();
			inventory ??= new InventorySaveData();

			world.NormalizeAfterLoad();
			inventory.NormalizeAfterLoad();
		}
	}

	[Serializable]
	public sealed class PlayerSaveData
	{
		public Vector3SaveData position;
		public QuaternionSaveData rotation;
	}

	[Serializable]
	public struct Vector3SaveData
	{
		public float x;
		public float y;
		public float z;

		public Vector3SaveData(float x, float y, float z)
		{
			this.x = x;
			this.y = y;
			this.z = z;
		}

		public static Vector3SaveData FromVector3(Vector3 value)
		{
			return new Vector3SaveData(value.x, value.y, value.z);
		}

		public Vector3 ToVector3()
		{
			return new Vector3(x, y, z);
		}
	}

	[Serializable]
	public struct QuaternionSaveData
	{
		public float x;
		public float y;
		public float z;
		public float w;

		public QuaternionSaveData(float x, float y, float z, float w)
		{
			this.x = x;
			this.y = y;
			this.z = z;
			this.w = w;
		}

		public static QuaternionSaveData FromQuaternion(Quaternion value)
		{
			return new QuaternionSaveData(value.x, value.y, value.z, value.w);
		}

		public Quaternion ToQuaternion()
		{
			return new Quaternion(x, y, z, w);
		}
	}

	[Serializable]
	public sealed class GameTimeSaveData
	{
		public int totalMinutes;
	}

	[Serializable]
	public sealed class SurvivalSaveData
	{
		public float hunger;
		public float thirst;
		public float fatigue;
		public float cold;
		public float condition;
	}

	[Serializable]
	public sealed class InventorySaveData
	{
		public List<InventoryItemSaveData> items = new();

		public void NormalizeAfterLoad()
		{
			items ??= new List<InventoryItemSaveData>();

			for (int i = 0; i < items.Count; i++)
			{
				items[i]?.NormalizeAfterLoad();
			}
		}
	}

	[Serializable]
	public sealed class InventoryItemSaveData
	{
		public string itemId;
		public int amount;

		public void NormalizeAfterLoad()
		{
			itemId ??= string.Empty;
			amount = Math.Max(0, amount);
		}
	}

	[Serializable]
	public sealed class WorldSaveData
	{
		public List<string> destroyedSceneEntityIds = new();
		public List<WorldEntitySaveData> entities = new();

		public void NormalizeAfterLoad()
		{
			destroyedSceneEntityIds ??= new List<string>();
			entities ??= new List<WorldEntitySaveData>();

			for (int i = 0; i < entities.Count; i++)
			{
				entities[i]?.NormalizeAfterLoad();
			}
		}
	}

	[Serializable]
	public sealed class WorldEntitySaveData
	{
		public string id;
		public string prefabId;
		public bool isSceneObject;

		public Vector3SaveData position;
		public QuaternionSaveData rotation;

		public List<WorldComponentSaveData> components = new();

		public void NormalizeAfterLoad()
		{
			id ??= string.Empty;
			prefabId ??= string.Empty;
			components ??= new List<WorldComponentSaveData>();

			for (int i = 0; i < components.Count; i++)
			{
				components[i]?.NormalizeAfterLoad();
			}
		}
	}

	[Serializable]
	public sealed class WorldComponentSaveData
	{
		public string typeId;
		public string json;

		public void NormalizeAfterLoad()
		{
			typeId ??= string.Empty;
			json ??= string.Empty;
		}
	}
}
