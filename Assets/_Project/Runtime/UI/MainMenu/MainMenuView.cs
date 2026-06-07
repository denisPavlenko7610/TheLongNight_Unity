using TLN.Application.Scenes;
using TLN.UI.Common;
using UnityEngine;
using UnityEngine.UIElements;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TLN.UI.MainMenu
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class MainMenuView : MonoBehaviour
    {
        private const string DisabledClassName = "main-menu-button-disabled";

        private Button _newGameButton;
        private Button _loadGameButton;
        private Button _optionsButton;
        private Button _quitButton;

        private ISceneLoader _sceneLoader;

        public void Construct(ISceneLoader sceneLoader)
        {
            _sceneLoader = sceneLoader;
        }

        private void Awake()
        {
            VisualElement root = GetComponent<UIDocument>().rootVisualElement;

            _newGameButton = root.RequiredQ<Button>("new-game-button");
            _loadGameButton = root.RequiredQ<Button>("load-game-button");
            _optionsButton = root.RequiredQ<Button>("options-button");
            _quitButton = root.RequiredQ<Button>("quit-button");

            _newGameButton.clicked += OnNewGameClicked;
            _loadGameButton.clicked += OnLoadGameClicked;
            _optionsButton.clicked += OnOptionsClicked;
            _quitButton.clicked += OnQuitClicked;

            SetComingSoon(_loadGameButton);
            SetComingSoon(_optionsButton);
        }

        private void OnDestroy()
        {
            if (_newGameButton != null)
            {
                _newGameButton.clicked -= OnNewGameClicked;
            }

            if (_loadGameButton != null)
            {
                _loadGameButton.clicked -= OnLoadGameClicked;
            }

            if (_optionsButton != null)
            {
                _optionsButton.clicked -= OnOptionsClicked;
            }

            if (_quitButton != null)
            {
                _quitButton.clicked -= OnQuitClicked;
            }
        }

        private static void SetComingSoon(Button button)
        {
            button.SetEnabled(false);
            button.AddToClassList(DisabledClassName);
        }

        private void OnNewGameClicked()
        {
            UnityEngine.Cursor.visible = false;

            if (_sceneLoader == null)
            {
                Debug.LogError("Cannot start a new game because scene loader is not available. Start the game through Boot scene.");
                return;
            }

            _sceneLoader.LoadWorld();
        }

        private static void OnLoadGameClicked()
        {
            Debug.Log("Load Game is not implemented yet.");
        }

        private static void OnOptionsClicked()
        {
            Debug.Log("Options are not implemented yet.");
        }

        private static void OnQuitClicked()
        {
#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
