using System;
using System.Collections.Generic;
using UnityEngine;

namespace TLN.Application.Saves
{
    [Serializable]
    public sealed class GameSaveData
    {
        public int version = 1;
        public int slotId;
        public string savedAtUtc;
        public string sceneName;
        public string saveReason;

        public WorldSaveData world = new WorldSaveData();
        public PlayerSaveData player = new PlayerSaveData();
        public GameTimeSaveData time = new GameTimeSaveData();
        public SurvivalSaveData survival = new SurvivalSaveData();
        public InventorySaveData inventory = new InventorySaveData();
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
    }

    [Serializable]
    public sealed class InventoryItemSaveData
    {
        public string itemId;
        public int amount;
    }

    [Serializable]
    public sealed class WorldSaveData
    {
        public List<string> destroyedSceneEntityIds = new();
        public List<WorldEntitySaveData> entities = new();
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
    }

    [Serializable]
    public sealed class WorldComponentSaveData
    {
        public string typeId;
        public string json;
    }
}
