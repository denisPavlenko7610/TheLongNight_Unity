using System;
using TLN.Application.Saves;
using TLN.Core.Logging;
using UnityEngine;

namespace TLN.Gameplay.Saves
{
	[DisallowMultipleComponent]
	public sealed class PersistentWorldEntity : MonoBehaviour
	{
		[SerializeField] private string _id;
		[SerializeField] private string _prefabId;

		public string Id => _id;
		public string PrefabId => _prefabId;

		public bool IsSceneObject => string.IsNullOrWhiteSpace(_prefabId);

		private void Awake()
		{
			if (IsSceneObject)
			{
				if (string.IsNullOrWhiteSpace(_id))
				{
					TLNLogger.LogWarning("Scene persistent entity has empty id.", this);
				}

				return;
			}

			if (string.IsNullOrWhiteSpace(_id))
			{
				_id = Guid.NewGuid().ToString("N");
			}
		}

		public void InitializeRuntimeEntity(string id)
		{
			if (IsSceneObject)
			{
				TLNLogger.LogWarning("Cannot initialize runtime id for scene entity.", this);

				return;
			}

			_id = id;
		}

		public WorldEntitySaveData CaptureState()
		{
			WorldEntitySaveData data = new WorldEntitySaveData
			{
				id = _id,
				prefabId = _prefabId,
				isSceneObject = IsSceneObject,
				position = Vector3SaveData.FromVector3(transform.position),
				rotation = QuaternionSaveData.FromQuaternion(transform.rotation)
			};

			MonoBehaviour[] behaviours = GetComponents<MonoBehaviour>();

			for (int i = 0; i < behaviours.Length; i++)
			{
				if (behaviours[i] is not IWorldSaveable saveable)
				{
					continue;
				}

				data.components.Add(
					new WorldComponentSaveData
					{
						typeId = saveable.SaveTypeId,
						json = saveable.CaptureStateJson()
					}
				);
			}

			return data;
		}

		public void RestoreState(WorldEntitySaveData data)
		{
			if (data == null)
			{
				return;
			}

			transform.SetPositionAndRotation(
				data.position.ToVector3(),
				data.rotation.ToQuaternion()
			);

			if (data.components == null)
			{
				return;
			}

			MonoBehaviour[] behaviours = GetComponents<MonoBehaviour>();

			for (int i = 0; i < data.components.Count; i++)
			{
				WorldComponentSaveData componentData =
					data.components[i];

				if (componentData == null)
				{
					continue;
				}

				RestoreComponent(behaviours, componentData);
			}
		}

		private static void RestoreComponent(MonoBehaviour[] behaviours, WorldComponentSaveData componentData)
		{
			if (string.IsNullOrWhiteSpace(componentData.typeId))
			{
				return;
			}

			for (int i = 0; i < behaviours.Length; i++)
			{
				if (behaviours[i] is not IWorldSaveable saveable)
				{
					continue;
				}

				if (saveable.SaveTypeId != componentData.typeId)
				{
					continue;
				}

				try
				{
					saveable.RestoreStateJson(componentData.json);
				}
				catch (Exception exception)
				{
					TLNLogger.LogError(
						$"Failed to restore world component save data. Type: {componentData.typeId}. {exception}",
						behaviours[i]
					);
				}

				return;
			}
		}

		#if UNITY_EDITOR
		public void EditorConfigureAsSceneObject()
		{
			_prefabId = string.Empty;

			if (string.IsNullOrWhiteSpace(_id))
			{
				_id = Guid.NewGuid().ToString("N");
			}

			UnityEditor.EditorUtility.SetDirty(this);
		}

		public void EditorRegenerateSceneId()
		{
			_prefabId = string.Empty;
			_id = Guid.NewGuid().ToString("N");

			UnityEditor.EditorUtility.SetDirty(this);
		}

		public void EditorConfigureAsPrefabAsset(string prefabId)
		{
			_prefabId = prefabId;
			_id = string.Empty;

			UnityEditor.EditorUtility.SetDirty(this);
		}
		#endif
	}
}
