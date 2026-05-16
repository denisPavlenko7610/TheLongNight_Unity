using TLN.Application.Scenes;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace TheLongNight.UI.MainMenu
{
	public class MainMenu : MonoBehaviour
	{
		[SerializeField] private Button _newGameButton;
		[SerializeField] private Button _loadGameButton;
		[SerializeField] private Button _optionsButton;
		[SerializeField] private Button _quitButton;

		private ISceneLoader _sceneLoader;

		private void OnEnable()
		{
			_newGameButton.onClick.AddListener(OnNewGameClicked);
			_loadGameButton.onClick.AddListener(OnLoadGameClicked);
			_optionsButton.onClick.AddListener(OnOptionsClicked);
			_quitButton.onClick.AddListener(OnQuitClicked);
		}

		private void OnDisable()
		{
			_newGameButton.onClick.RemoveListener(OnNewGameClicked);
			_loadGameButton.onClick.RemoveListener(OnLoadGameClicked);
			_optionsButton.onClick.RemoveListener(OnOptionsClicked);
			_quitButton.onClick.RemoveListener(OnQuitClicked);
		}

		private void OnNewGameClicked()
		{
			Cursor.visible = false;

			if (_sceneLoader == null) {
				Debug.LogError(
					"Cannot start new game because scene loader is not available. Start the game through Boot scene."
				);
				return;
			}

			_sceneLoader.LoadWorld();
		}

		private void OnLoadGameClicked()
		{
			Debug.Log("Load Game Clicked");
		}

		private void OnOptionsClicked()
		{
			Debug.Log("Options Clicked");
		}

		private void OnQuitClicked()
		{
			#if UNITY_EDITOR
			EditorApplication.isPlaying = false;
			#else
            Application.Quit();
			#endif
		}
	}
}
