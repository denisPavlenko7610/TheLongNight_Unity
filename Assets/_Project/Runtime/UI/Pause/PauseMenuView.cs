using TLN.Application.GameStates;
using TLN.Application.Input;
using TLN.Application.Scenes;
using TLN.Gameplay.Player.Input;
using TLN.UI.Common;
using UnityEngine;
using UnityEngine.UIElements;

namespace TLN.UI.Pause
{
	[RequireComponent(typeof(UIDocument))]
	public sealed class PauseMenuView : MonoBehaviour, IPauseMenuView
	{
		private const string VisibleClassName = "pause-menu-root-visible";

		private UIDocument _document;
		private VisualElement _root;

		private Button _resumeButton;
		private Button _settingsButton;
		private Button _quitButton;

		private Button _saveButton;
		private Button _loadButton;

		private IGameStateMachine _gameStateMachine;
		private IInputModeService _inputModeService;
		private ISceneLoader _sceneLoader;

		public void Construct(IGameStateMachine gameStateMachine, IInputModeService inputModeService, ISceneLoader sceneLoader)
		{
			_gameStateMachine = gameStateMachine;
			_inputModeService = inputModeService;
			_sceneLoader = sceneLoader;

			Hide();
		}

		private void Awake()
		{
			_document = GetComponent<UIDocument>();
			VisualElement documentRoot = _document.rootVisualElement;

			_root = documentRoot.RequiredQ<VisualElement>("pause-menu-root");

			_saveButton = documentRoot.RequiredQ<Button>("save-button");
			_loadButton = documentRoot.RequiredQ<Button>("load-button");

			_saveButton.SetEnabled(false);
			_loadButton.SetEnabled(false);

			_resumeButton = documentRoot.RequiredQ<Button>("resume-button");
			_settingsButton = documentRoot.RequiredQ<Button>("settings-button");
			_quitButton = documentRoot.RequiredQ<Button>("quit-button");

			_resumeButton.clicked += OnResumeClicked;
			_settingsButton.clicked += OnSettingsClicked;
			_quitButton.clicked += OnQuitClicked;
		}

		private void OnDestroy()
		{
			_resumeButton.clicked -= OnResumeClicked;
			_settingsButton.clicked -= OnSettingsClicked;
			_quitButton.clicked -= OnQuitClicked;
		}

		public void Show()
		{
			_root.AddToClassList(VisibleClassName);
		}

		public void Hide()
		{
			if (_root != null)
			{
				_root.RemoveFromClassList(VisibleClassName);
			}
		}

		private void OnResumeClicked()
		{
			_gameStateMachine.Enter(TLN.Core.GameStates.GameStateId.Playing);
			_inputModeService.SetGameplayMode();
			Hide();
		}

		private void OnSettingsClicked()
		{
			// Placeholder. Later we will open SettingsWindow.
		}

		private void OnQuitClicked()
		{
			_inputModeService.SetUIMode();
			_sceneLoader.LoadMainMenu();
		}
	}
}
