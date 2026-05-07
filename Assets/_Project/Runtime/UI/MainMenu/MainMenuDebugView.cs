using TLN.Application.Scenes;
using UnityEngine;

namespace TLN.UI.MainMenu
{
	public sealed class MainMenuDebugView : MonoBehaviour
	{
		private ISceneLoader _sceneLoader;

		public void Construct(ISceneLoader sceneLoader)
		{
			_sceneLoader = sceneLoader;
		}

		private void OnGUI()
		{
			const int width = 220;
			const int height = 50;

			Rect rect = new Rect(
				(Screen.width - width) * 0.5f,
				(Screen.height - height) * 0.5f,
				width,
				height);

			if (GUI.Button(rect, "Start Game"))
			{
				_sceneLoader.LoadWorld();
			}
		}
	}
}
