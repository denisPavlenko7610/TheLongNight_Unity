using UnityEngine;
using UnityApplication = UnityEngine.Application;

namespace TLN.Gameplay.Cheats
{
	[CreateAssetMenu(fileName = "CheatSettings", menuName = "TLN/Cheats/Cheat Settings")]
	public sealed class CheatSettings : ScriptableObject
	{
		[SerializeField] private bool _enabled = true;
		[SerializeField] private bool _allowInPlayerBuilds;
		[SerializeField] private bool _showRuntimeOverlay;
		[SerializeField] private KeyCode _runtimeOverlayToggleKey = KeyCode.BackQuote;

		public bool Enabled => _enabled;
		public bool AllowInPlayerBuilds => _allowInPlayerBuilds;
		public bool ShowRuntimeOverlay => _showRuntimeOverlay;
		public KeyCode RuntimeOverlayToggleKey => _runtimeOverlayToggleKey;

		public bool CanOpenRuntimeOverlay => _showRuntimeOverlay && (UnityApplication.isEditor || _allowInPlayerBuilds);
		public bool CanUseCheats => _enabled && (UnityApplication.isEditor || _allowInPlayerBuilds);

		#if UNITY_EDITOR
		public void EditorSetEnabled(bool enabled)
		{
			_enabled = enabled;
			UnityEditor.EditorUtility.SetDirty(this);
		}

		public void EditorSetAllowInPlayerBuilds(bool allowInPlayerBuilds)
		{
			_allowInPlayerBuilds = allowInPlayerBuilds;
			UnityEditor.EditorUtility.SetDirty(this);
		}

		public void EditorSetShowRuntimeOverlay(bool showRuntimeOverlay)
		{
			_showRuntimeOverlay = showRuntimeOverlay;
			UnityEditor.EditorUtility.SetDirty(this);
		}

		public void EditorSetRuntimeOverlayToggleKey(KeyCode runtimeOverlayToggleKey)
		{
			_runtimeOverlayToggleKey = runtimeOverlayToggleKey;
			UnityEditor.EditorUtility.SetDirty(this);
		}
		#endif
	}
}
