using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TLN.Editor.PlayFromBoot
{
	[InitializeOnLoad]
	public static class PlayFromBoot
	{
		private const string BootScenePath = "Assets/_Project/Scenes/Boot.unity";

		private static bool _preparedForPendingPlayMode;

		static PlayFromBoot()
		{
			EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(BootScenePath);

			EditorApplication.update -= OnEditorUpdate;
			EditorApplication.update += OnEditorUpdate;

			EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
			EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

			AssemblyReloadEvents.beforeAssemblyReload -= OnBeforeAssemblyReload;
			AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;

			EditorApplication.delayCall -= ClearEditorSelection;
			EditorApplication.delayCall += ClearEditorSelection;

			if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
			{
				PrepareForPlayModeTransition();
			}
		}

		private static void OnEditorUpdate()
		{
			if (EditorApplication.isPlaying)
			{
				return;
			}

			if (!EditorApplication.isPlayingOrWillChangePlaymode)
			{
				_preparedForPendingPlayMode = false;
				return;
			}

			if (_preparedForPendingPlayMode)
			{
				return;
			}

			PrepareForPlayModeTransition();
			_preparedForPendingPlayMode = true;
		}

		private static void OnPlayModeStateChanged(PlayModeStateChange state)
		{
			switch (state)
			{
				case PlayModeStateChange.ExitingEditMode:
				case PlayModeStateChange.ExitingPlayMode:
					PrepareForPlayModeTransition();
					break;

				case PlayModeStateChange.EnteredEditMode:
				case PlayModeStateChange.EnteredPlayMode:
					_preparedForPendingPlayMode = false;
					EditorApplication.delayCall -= ClearEditorSelection;
					EditorApplication.delayCall += ClearEditorSelection;
					break;
			}
		}

		private static void OnBeforeAssemblyReload()
		{
			if (!EditorApplication.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode)
			{
				return;
			}

			PrepareForPlayModeTransition();
		}

		private static void PrepareForPlayModeTransition()
		{
			SelectStableEditorTarget();
		}

		private static void SelectStableEditorTarget()
		{
			SceneAsset bootScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(BootScenePath);

			if (bootScene == null)
			{
				ClearEditorSelection();
				return;
			}

			Selection.objects = new UnityEngine.Object[] { bootScene };
			Selection.activeObject = bootScene;
		}

		private static void ClearEditorSelection()
		{
			Selection.objects = Array.Empty<UnityEngine.Object>();
			Selection.activeObject = null;
		}
	}
}
