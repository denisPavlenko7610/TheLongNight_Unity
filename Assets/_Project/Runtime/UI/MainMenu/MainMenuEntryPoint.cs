using TLN.Application.App;
using TLN.Application.Scenes;
using UnityEngine;

namespace TLN.UI.MainMenu
{
	public sealed class MainMenuEntryPoint : MonoBehaviour
	{
		[SerializeField] private MainMenuDebugView _debugView;

		private void Awake()
		{
			ISceneLoader sceneLoader = AppRoot.Instance.Services.Resolve<ISceneLoader>();

			_debugView.Construct(sceneLoader);
		}
	}
}
