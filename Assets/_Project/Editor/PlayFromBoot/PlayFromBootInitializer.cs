using TLN.Core.Scenes;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace TLN.Editor.PlayFromBoot
{
	[InitializeOnLoad]
	public static class PlayFromBootInitializer
	{
		static PlayFromBootInitializer()
		{
			EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
			EditorSceneManager.activeSceneChangedInEditMode += OnActiveSceneChangedInEditMode;
			EditorSceneManager.sceneOpened += OnSceneOpened;

			ConfigurePlayModeStartScene();
		}

		private static void OnPlayModeStateChanged(PlayModeStateChange state)
		{
			if (state == PlayModeStateChange.ExitingEditMode)
			{
				CaptureTargetScenePath();
				return;
			}

			if (state == PlayModeStateChange.EnteredEditMode)
			{
				EditorPrefs.DeleteKey(PlayFromBootSettings.TargetScenePathKey);
				ConfigurePlayModeStartScene();
			}
		}

		internal static void ConfigurePlayModeStartScene()
		{
			bool isEnabled = EditorPrefs.GetBool(PlayFromBootSettings.IsEnabledKey, true);

			if (!isEnabled)
			{
				EditorSceneManager.playModeStartScene = null;
				return;
			}

			Scene activeScene = SceneManager.GetActiveScene();

			if (activeScene.name == SceneNames.Boot)
			{
				EditorPrefs.DeleteKey(PlayFromBootSettings.TargetScenePathKey);
				EditorSceneManager.playModeStartScene = null;
				return;
			}

			SceneAsset bootScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(PlayFromBootSettings.BootScenePath);

			EditorSceneManager.playModeStartScene = bootScene;
		}

		private static void CaptureTargetScenePath()
		{
			bool isEnabled = EditorPrefs.GetBool(PlayFromBootSettings.IsEnabledKey, true);

			if (!isEnabled)
			{
				EditorPrefs.DeleteKey(PlayFromBootSettings.TargetScenePathKey);
				return;
			}

			Scene activeScene = SceneManager.GetActiveScene();

			if (activeScene.name == SceneNames.Boot || string.IsNullOrWhiteSpace(activeScene.path))
			{
				EditorPrefs.DeleteKey(PlayFromBootSettings.TargetScenePathKey);
				return;
			}

			EditorPrefs.SetString(PlayFromBootSettings.TargetScenePathKey, activeScene.path);
		}

		private static void OnActiveSceneChangedInEditMode(Scene previousScene, Scene newScene)
		{
			ConfigurePlayModeStartScene();
		}

		private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
		{
			ConfigurePlayModeStartScene();
		}
	}
}
