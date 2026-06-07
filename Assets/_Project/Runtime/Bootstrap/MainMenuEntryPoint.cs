using TLN.Application.Scenes;
using TLN.UI.MainMenu;
using UnityEngine;
using VContainer;

namespace TLN.Bootstrap.MainMenu
{
	public sealed class MainMenuEntryPoint : MonoBehaviour
	{
		[SerializeField] private MainMenuView _menuView;

		private ISceneLoader _sceneLoader;

		[Inject]
		public void Construct(ISceneLoader sceneLoader)
		{
			_sceneLoader = sceneLoader;
			InitializeMenu();
		}

		private void Awake()
		{
			ResolveMenuView();
		}

		private void ResolveMenuView()
		{
			if (_menuView == null)
			{
				_menuView = FindFirstObjectByType<MainMenuView>();
			}
		}

		private void InitializeMenu()
		{
			ResolveMenuView();

			if (_menuView == null)
			{
				Debug.LogError("MainMenuView was not found in MainMenu scene.");
				return;
			}

			if (_sceneLoader == null)
			{
				Debug.LogError("Scene loader was not injected into MainMenuEntryPoint.");
				return;
			}

			_menuView.Construct(_sceneLoader);
		}
	}
}