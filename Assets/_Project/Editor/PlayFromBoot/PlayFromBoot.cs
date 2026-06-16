using UnityEditor;
using UnityEditor.SceneManagement;

namespace TLN.Editor.PlayFromBoot
{
	[InitializeOnLoad]
	public static class PlayFromBoot
	{
		private const string BootScenePath = "Assets/_Project/Scenes/Boot.unity";

		static PlayFromBoot()
		{
			EditorSceneManager.playModeStartScene =
				AssetDatabase.LoadAssetAtPath<SceneAsset>(BootScenePath);
		}
	}
}
