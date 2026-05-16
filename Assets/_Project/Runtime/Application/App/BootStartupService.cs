using TLN.Application.Scenes;
using TLN.Core.Lifetime;
using UnityEngine;

namespace TLN.Application.App
{
	public sealed class BootStartupService : IGameService
	{
		private const string EditorTargetScenePathKey = "TLN.PlayFromBoot.TargetScenePath";

		private readonly ISceneLoader _sceneLoader;

		public BootStartupService(ISceneLoader sceneLoader)
		{
			_sceneLoader = sceneLoader;
		}

		public void Start()
		{
			string targetScenePath = GetEditorTargetScenePath();

			if (!string.IsNullOrWhiteSpace(targetScenePath))
			{
				Debug.Log($"Boot startup: loading editor target scene: {targetScenePath}");
				_sceneLoader.LoadSceneForDevelopment(targetScenePath);
				ClearEditorTargetScenePath();
				return;
			}

			_sceneLoader.LoadMainMenu();
		}

		private static string GetEditorTargetScenePath()
		{
			#if UNITY_EDITOR
			return UnityEditor.EditorPrefs.GetString(EditorTargetScenePathKey, string.Empty);
			#else
            return string.Empty;
			#endif
		}

		private static void ClearEditorTargetScenePath()
		{
			#if UNITY_EDITOR
			UnityEditor.EditorPrefs.DeleteKey(EditorTargetScenePathKey);
			#endif
		}
	}
}
