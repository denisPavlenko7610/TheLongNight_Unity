using TLN.Application.Scenes;
using TLN.Bootstrap.App;
using TLN.UI.MainMenu;
using UnityEngine;

namespace TLN.Bootstrap.MainMenu
{
	public sealed class MainMenuEntryPoint : MonoBehaviour
	{
		[SerializeField] private MainMenuDebugView _debugView;

		private void Awake()
		{
			if (_debugView == null)
			{
				_debugView = FindFirstObjectByType<MainMenuDebugView>();
			}

			if (_debugView == null)
			{
				Debug.LogError("MainMenuDebugView was not found in MainMenu scene.");
				return;
			}

			if (AppRoot.Instance == null || AppRoot.Instance.Services == null)
			{
				Debug.LogError("AppRoot is missing. Start the game from Boot scene.");
				return;
			}

			ISceneLoader sceneLoader = AppRoot.Instance.Services.Resolve<ISceneLoader>();

			_debugView.Construct(sceneLoader);
		}
	}
}
