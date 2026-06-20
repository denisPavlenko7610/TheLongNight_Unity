using System.Collections.Generic;
using System.Reflection;
using TLN.Core.Logging;
using TLN.Core.Validation;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TLN.Editor.Validation
{
    [InitializeOnLoad]
    public static class RequiredReferencePlayModeValidator
    {
        private const bool BlockPlayModeOnError = true;
        private const string ProjectRoot = "Assets/_Project";
        private const string IgnoreValidationLabel = "IgnoreRequiredValidation";

        static RequiredReferencePlayModeValidator()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        [MenuItem("Tools/TLN/Validate Required References")]
        public static void ValidateFromMenu()
        {
            int errorCount = ValidateAll();

            if (errorCount <= 0)
            {
                Debug.Log("[Required] No missing required references found.");
                return;
            }

            TLNLogger.LogError($"[Required] Found {errorCount} missing required reference(s). See detailed errors above.");
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.ExitingEditMode)
            {
                return;
            }

            int errorCount = ValidateOpenScenes();

            if (errorCount <= 0)
            {
                return;
            }

            TLNLogger.LogError($"[Required] Found {errorCount} missing required scene reference(s). See detailed errors above.");

            if (BlockPlayModeOnError)
            {
                EditorApplication.isPlaying = false;
            }
        }

        private static int ValidateAll()
        {
            int errorCount = 0;

            errorCount += ValidateOpenScenes();
            errorCount += ValidateProjectPrefabs();

            return errorCount;
        }

        private static int ValidateOpenScenes()
        {
            int errorCount = 0;
            int sceneCount = SceneManager.sceneCount;

            for (int sceneIndex = 0; sceneIndex < sceneCount; sceneIndex++)
            {
                Scene scene = SceneManager.GetSceneAt(sceneIndex);

                if (!scene.isLoaded)
                {
                    continue;
                }

                GameObject[] rootObjects = scene.GetRootGameObjects();

                foreach (GameObject rootObject in rootObjects)
                {
                    errorCount += ValidateGameObject(
                        rootObject,
                        $"Scene: {scene.name}",
                        string.Empty);
                }
            }

            return errorCount;
        }

        private static int ValidateProjectPrefabs()
        {
            int errorCount = 0;

            string[] prefabGuids = AssetDatabase.FindAssets(
                "t:Prefab",
                new[] { ProjectRoot });

            foreach (string prefabGuid in prefabGuids)
            {
                string prefabPath = AssetDatabase.GUIDToAssetPath(prefabGuid);

                if (ShouldSkipPrefabAsset(prefabPath))
                {
                    continue;
                }

                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

                if (prefab == null)
                {
                    continue;
                }

                errorCount += ValidateGameObject(
                    prefab,
                    "Prefab Asset",
                    prefabPath);
            }

            return errorCount;
        }

        private static int ValidateGameObject(
            GameObject root,
            string source,
            string assetPath)
        {
            int errorCount = 0;
            MonoBehaviour[] behaviours = root.GetComponentsInChildren<MonoBehaviour>(true);

            foreach (MonoBehaviour behaviour in behaviours)
            {
                if (behaviour == null)
                {
                    continue;
                }

                errorCount += ValidateBehaviour(
                    behaviour,
                    source,
                    assetPath);
            }

            return errorCount;
        }

        private static bool ShouldSkipPrefabAsset(string prefabPath)
        {
            Object asset = AssetDatabase.LoadMainAssetAtPath(prefabPath);

            if (asset == null)
            {
                return false;
            }

            string[] labels = AssetDatabase.GetLabels(asset);
            foreach (string label in labels)
            {
                if (label == IgnoreValidationLabel)
                {
                    return true;
                }
            }

            return false;
        }

        private static int ValidateBehaviour(
            MonoBehaviour behaviour,
            string source,
            string assetPath)
        {
            int errorCount = 0;
            System.Type type = behaviour.GetType();

            FieldInfo[] fields = type.GetFields(
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic);

            foreach (FieldInfo field in fields)
            {
                RequiredAttribute attribute = field.GetCustomAttribute<RequiredAttribute>();

                if (attribute == null)
                {
                    continue;
                }

                object value = field.GetValue(behaviour);

                if (!IsMissing(value))
                {
                    continue;
                }

                string hierarchyPath = GetHierarchyPath(behaviour.transform);
                string fieldName = GetCleanFieldName(field.Name);
                string resolvedAssetPath = GetBestAssetPath(behaviour, assetPath);

                TLNLogger.LogError(
                    $"[Required] Missing reference.\n" +
                    $"Source: {source}\n" +
                    $"Asset: {resolvedAssetPath}\n" +
                    $"Object: {hierarchyPath}\n" +
                    $"Component: {type.Name}\n" +
                    $"Field: {fieldName}",
                    behaviour);

                errorCount++;
            }

            return errorCount;
        }

        private static bool IsMissing(object value)
        {
            if (value == null)
            {
                return true;
            }

            if (value is Object unityObject)
            {
                return unityObject == null;
            }

            return false;
        }

        private static string GetCleanFieldName(string fieldName)
        {
            if (fieldName.StartsWith("<") && fieldName.Contains(">"))
            {
                int endIndex = fieldName.IndexOf('>');
                return fieldName.Substring(1, endIndex - 1);
            }

            return fieldName;
        }

        private static string GetHierarchyPath(Transform transform)
        {
            if (transform == null)
            {
                return "<Missing Transform>";
            }

            List<string> names = new List<string>();
            Transform current = transform;

            while (current != null)
            {
                names.Add(current.name);
                current = current.parent;
            }

            names.Reverse();

            return string.Join("/", names);
        }

        private static string GetBestAssetPath(Object context, string fallbackAssetPath)
        {
            string contextPath = AssetDatabase.GetAssetPath(context);

            if (!string.IsNullOrWhiteSpace(contextPath))
            {
                return contextPath;
            }

            if (!string.IsNullOrWhiteSpace(fallbackAssetPath))
            {
                return fallbackAssetPath;
            }

            return "<Scene Object>";
        }
    }
}
