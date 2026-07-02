using System;
using System.Collections;
using System.IO;
using System.Reflection;
using TLN.Infrastructure.Audio;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;

namespace TLN.Editor.Audio
{
	public static class TlnAudioMixerAssetBuilder
	{
		private const string MixerPath = "Assets/_Project/Audio/TLN_AudioMixer.mixer";
		private const string ConfigPath = "Assets/_Project/Audio/TLN_AudioMixerConfig.asset";
		private const string ProjectLifetimeScopePath = "Assets/Plugins/VContainer/ProjectLifetimeScope.prefab";

		private const string MasterVolumeParameter = "MasterVolume";
		private const string MusicVolumeParameter = "MusicVolume";
		private const string AmbientVolumeParameter = "AmbientVolume";
		private const string SfxVolumeParameter = "SfxVolume";

		private static readonly BindingFlags ReflectionFlags =
			BindingFlags.Instance |
			BindingFlags.Static |
			BindingFlags.Public |
			BindingFlags.NonPublic;

		[MenuItem("Tools/TLN/Audio/Rebuild Audio Mixer")]
		public static void Build()
		{
			EnsureParentDirectory(MixerPath);

			object mixerController = LoadOrCreateMixerController();
			object masterGroup = GetPropertyValue(mixerController, "masterGroup");
			object musicGroup = GetOrCreateChildGroup(mixerController, masterGroup, "Music");
			object ambientGroup = GetOrCreateChildGroup(mixerController, masterGroup, "Ambient");
			object sfxGroup = GetOrCreateChildGroup(mixerController, masterGroup, "SFX");

			ExposeVolume(mixerController, masterGroup, MasterVolumeParameter);
			ExposeVolume(mixerController, musicGroup, MusicVolumeParameter);
			ExposeVolume(mixerController, ambientGroup, AmbientVolumeParameter);
			ExposeVolume(mixerController, sfxGroup, SfxVolumeParameter);

			AudioMixerConfig config = LoadOrCreateConfig();
			AssignConfig(
				config,
				(AudioMixer)mixerController,
				(AudioMixerGroup)masterGroup,
				(AudioMixerGroup)musicGroup,
				(AudioMixerGroup)ambientGroup,
				(AudioMixerGroup)sfxGroup
			);

			AssignConfigToProjectLifetimeScope(config);

			EditorUtility.SetDirty((UnityEngine.Object)mixerController);
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();

			Debug.Log($"Audio mixer rebuilt: {MixerPath}, config: {ConfigPath}.");
		}

		private static object LoadOrCreateMixerController()
		{
			AudioMixer existingMixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);

			if (existingMixer != null)
			{
				return existingMixer;
			}

			Type controllerType = GetUnityEditorAudioType("AudioMixerController");
			MethodInfo createMethod = controllerType.GetMethod(
				"CreateMixerControllerAtPath",
				ReflectionFlags
			);

			if (createMethod == null)
			{
				throw new MissingMethodException(
					controllerType.FullName,
					"CreateMixerControllerAtPath"
				);
			}

			return createMethod.Invoke(null, new object[] { MixerPath });
		}

		private static object GetOrCreateChildGroup(
			object mixerController,
			object parentGroup,
			string groupName
		)
		{
			object existingGroup = FindGroupByName(mixerController, groupName);

			if (existingGroup != null)
			{
				return existingGroup;
			}

			object group = Invoke(
				mixerController,
				"CreateNewGroup",
				groupName,
				false
			);

			Invoke(mixerController, "AddChildToParent", group, parentGroup);

			return group;
		}

		private static object FindGroupByName(object mixerController, string groupName)
		{
			IEnumerable groups = (IEnumerable)Invoke(mixerController, "GetAllAudioGroupsSlow");

			foreach (object group in groups)
			{
				if (group is UnityEngine.Object unityObject && unityObject.name == groupName)
				{
					return group;
				}
			}

			return null;
		}

		private static void ExposeVolume(
			object mixerController,
			object group,
			string parameterName
		)
		{
			object volumeGuid = Invoke(group, "GetGUIDForVolume");

			if (!ContainsExposedParameter(mixerController, volumeGuid))
			{
				Type pathType = GetUnityEditorAudioType("AudioGroupParameterPath");
				object path = Activator.CreateInstance(
					pathType,
					ReflectionFlags,
					null,
					new[] { group, volumeGuid },
					null
				);

				Invoke(mixerController, "AddExposedParameter", path);
			}

			RenameExposedParameter(mixerController, volumeGuid, parameterName);
		}

		private static bool ContainsExposedParameter(
			object mixerController,
			object parameterGuid
		)
		{
			Array exposedParameters = GetExposedParameters(mixerController);

			if (exposedParameters == null)
			{
				return false;
			}

			for (int i = 0; i < exposedParameters.Length; i++)
			{
				object exposedParameter = exposedParameters.GetValue(i);
				object exposedGuid = GetFieldValue(exposedParameter, "guid");

				if (Equals(exposedGuid, parameterGuid))
				{
					return true;
				}
			}

			return false;
		}

		private static void RenameExposedParameter(
			object mixerController,
			object parameterGuid,
			string parameterName
		)
		{
			PropertyInfo property = GetProperty(mixerController.GetType(), "exposedParameters");
			Array exposedParameters = (Array)property.GetValue(mixerController);

			if (exposedParameters == null)
			{
				return;
			}

			for (int i = 0; i < exposedParameters.Length; i++)
			{
				object exposedParameter = exposedParameters.GetValue(i);
				object exposedGuid = GetFieldValue(exposedParameter, "guid");

				if (!Equals(exposedGuid, parameterGuid))
				{
					continue;
				}

				SetFieldValue(ref exposedParameter, "name", parameterName);
				exposedParameters.SetValue(exposedParameter, i);
				property.SetValue(mixerController, exposedParameters);
				Invoke(mixerController, "OnChangedExposedParameter");
				return;
			}
		}

		private static Array GetExposedParameters(object mixerController)
		{
			PropertyInfo property = GetProperty(mixerController.GetType(), "exposedParameters");
			return (Array)property?.GetValue(mixerController);
		}

		private static AudioMixerConfig LoadOrCreateConfig()
		{
			AudioMixerConfig config =
				AssetDatabase.LoadAssetAtPath<AudioMixerConfig>(ConfigPath);

			if (config != null)
			{
				return config;
			}

			EnsureParentDirectory(ConfigPath);
			config = ScriptableObject.CreateInstance<AudioMixerConfig>();
			AssetDatabase.CreateAsset(config, ConfigPath);
			return config;
		}

		private static void AssignConfig(
			AudioMixerConfig config,
			AudioMixer mixer,
			AudioMixerGroup masterGroup,
			AudioMixerGroup musicGroup,
			AudioMixerGroup ambientGroup,
			AudioMixerGroup sfxGroup
		)
		{
			SerializedObject serializedConfig = new SerializedObject(config);
			SetObject(serializedConfig, "_mixer", mixer);
			SetObject(serializedConfig, "_masterGroup", masterGroup);
			SetObject(serializedConfig, "_musicGroup", musicGroup);
			SetObject(serializedConfig, "_ambientGroup", ambientGroup);
			SetObject(serializedConfig, "_sfxGroup", sfxGroup);
			SetString(serializedConfig, "_masterVolumeParameter", MasterVolumeParameter);
			SetString(serializedConfig, "_musicVolumeParameter", MusicVolumeParameter);
			SetString(serializedConfig, "_ambientVolumeParameter", AmbientVolumeParameter);
			SetString(serializedConfig, "_sfxVolumeParameter", SfxVolumeParameter);
			serializedConfig.ApplyModifiedPropertiesWithoutUndo();
			EditorUtility.SetDirty(config);
		}

		private static void AssignConfigToProjectLifetimeScope(AudioMixerConfig config)
		{
			GameObject prefab =
				AssetDatabase.LoadAssetAtPath<GameObject>(ProjectLifetimeScopePath);

			if (prefab == null)
			{
				Debug.LogWarning(
					$"ProjectLifetimeScope prefab not found at {ProjectLifetimeScopePath}."
				);
				return;
			}

			MonoBehaviour scope = FindProjectLifetimeScope(prefab);

			if (scope == null)
			{
				Debug.LogWarning(
					$"ProjectLifetimeScope component not found in {ProjectLifetimeScopePath}."
				);
				return;
			}

			SerializedObject serializedScope = new SerializedObject(scope);
			SetObject(serializedScope, "_audioMixerConfig", config);
			serializedScope.ApplyModifiedPropertiesWithoutUndo();
			EditorUtility.SetDirty(scope);
			PrefabUtility.SavePrefabAsset(prefab);
		}

		private static MonoBehaviour FindProjectLifetimeScope(GameObject prefab)
		{
			MonoScript projectLifetimeScopeScript =
				AssetDatabase.LoadAssetAtPath<MonoScript>(
					"Assets/_Project/Runtime/Bootstrap/ProjectLifetimeScope.cs"
				);

			if (projectLifetimeScopeScript == null)
			{
				return null;
			}

			MonoBehaviour[] behaviours = prefab.GetComponents<MonoBehaviour>();

			for (int i = 0; i < behaviours.Length; i++)
			{
				MonoBehaviour behaviour = behaviours[i];

				if (behaviour == null)
				{
					continue;
				}

				if (MonoScript.FromMonoBehaviour(behaviour) == projectLifetimeScopeScript)
				{
					return behaviour;
				}
			}

			return null;
		}

		private static void SetObject(
			SerializedObject serializedObject,
			string propertyName,
			UnityEngine.Object value
		)
		{
			SerializedProperty property = serializedObject.FindProperty(propertyName);

			if (property != null)
			{
				property.objectReferenceValue = value;
			}
		}

		private static void SetString(
			SerializedObject serializedObject,
			string propertyName,
			string value
		)
		{
			SerializedProperty property = serializedObject.FindProperty(propertyName);

			if (property != null)
			{
				property.stringValue = value;
			}
		}

		private static void EnsureParentDirectory(string assetPath)
		{
			string fullDirectoryPath = Path.GetDirectoryName(assetPath);

			if (!string.IsNullOrEmpty(fullDirectoryPath))
			{
				Directory.CreateDirectory(fullDirectoryPath);
			}
		}

		private static Type GetUnityEditorAudioType(string typeName)
		{
			Type type = typeof(UnityEditor.Editor).Assembly.GetType(
				$"UnityEditor.Audio.{typeName}",
				false
			);

			if (type == null)
			{
				throw new TypeLoadException($"UnityEditor.Audio.{typeName}");
			}

			return type;
		}

		private static object Invoke(object target, string methodName, params object[] args)
		{
			MethodInfo method = GetMethod(target.GetType(), methodName);

			if (method == null)
			{
				throw new MissingMethodException(target.GetType().FullName, methodName);
			}

			return method.Invoke(target, args);
		}

		private static MethodInfo GetMethod(Type type, string methodName)
		{
			for (Type current = type; current != null; current = current.BaseType)
			{
				MethodInfo method = current.GetMethod(methodName, ReflectionFlags);

				if (method != null)
				{
					return method;
				}
			}

			return null;
		}

		private static object GetPropertyValue(object target, string propertyName)
		{
			PropertyInfo property = GetProperty(target.GetType(), propertyName);

			if (property == null)
			{
				throw new MissingMemberException(target.GetType().FullName, propertyName);
			}

			return property.GetValue(target);
		}

		private static PropertyInfo GetProperty(Type type, string propertyName)
		{
			for (Type current = type; current != null; current = current.BaseType)
			{
				PropertyInfo property = current.GetProperty(propertyName, ReflectionFlags);

				if (property != null)
				{
					return property;
				}
			}

			return null;
		}

		private static object GetFieldValue(object target, string fieldName)
		{
			FieldInfo field = GetField(target.GetType(), fieldName);
			return field?.GetValue(target);
		}

		private static void SetFieldValue(
			ref object target,
			string fieldName,
			object value
		)
		{
			FieldInfo field = GetField(target.GetType(), fieldName);
			field?.SetValue(target, value);
		}

		private static FieldInfo GetField(Type type, string fieldName)
		{
			for (Type current = type; current != null; current = current.BaseType)
			{
				FieldInfo field = current.GetField(fieldName, ReflectionFlags);

				if (field != null)
				{
					return field;
				}
			}

			return null;
		}
	}
}
