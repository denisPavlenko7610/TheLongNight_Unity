using UnityEditor;

namespace TLN.Editor.PlayFromBoot
{
	public static class PlayFromBootMenu
	{
		private const string MenuPath = "TLN/Play From Boot/Enabled";

		[MenuItem(MenuPath)]
		private static void ToggleEnabled()
		{
			bool isEnabled = EditorPrefs.GetBool(PlayFromBootSettings.IsEnabledKey, true);

			EditorPrefs.SetBool(PlayFromBootSettings.IsEnabledKey, !isEnabled);
			PlayFromBootInitializer.ConfigurePlayModeStartScene();
		}

		[MenuItem(MenuPath, true)]
		private static bool ToggleEnabledValidate()
		{
			bool isEnabled = EditorPrefs.GetBool(PlayFromBootSettings.IsEnabledKey, true);

			Menu.SetChecked(MenuPath, isEnabled);
			return true;
		}
	}
}
