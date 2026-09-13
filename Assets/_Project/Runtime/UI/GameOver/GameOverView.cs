using TLN.Application.GameStates;
using TLN.Application.Localization;
using TLN.Application.Scenes;
using TLN.Gameplay.Time;
using UnityEngine;
using UnityEngine.UIElements;

namespace TLN.UI.GameOver
{
	[RequireComponent(typeof(UIDocument))]
	public sealed class GameOverView : MonoBehaviour
	{
		private VisualElement _root;
		private Label _daysSurvivedLabel;
		private Button _mainMenuButton;

		private IGameStateMachine _gameStateMachine;
		private ISceneLoader _sceneLoader;
		private IGameTimeService _gameTimeService;
		private bool _isConstructed;
		private bool _isReturning;

		public void Construct(
			IGameStateMachine gameStateMachine,
			ISceneLoader sceneLoader,
			IGameTimeService gameTimeService
		)
		{
			_gameStateMachine = gameStateMachine;
			_sceneLoader = sceneLoader;
			_gameTimeService = gameTimeService;

			if (!_isConstructed)
			{
				BuildUI();

				_gameStateMachine.StateChanged += OnGameStateChanged;
				_isConstructed = true;
			}
		}

		private void OnDestroy()
		{
			if (_gameStateMachine != null && _isConstructed)
			{
				_gameStateMachine.StateChanged -= OnGameStateChanged;
			}
		}

		private void OnGameStateChanged(GameStateId previousState, GameStateId nextState)
		{
			if (nextState == GameStateId.GameOver)
			{
				Show();
			}
			else
			{
				Hide();
			}
		}

		private void Show()
		{
			if (_daysSurvivedLabel != null && _gameTimeService != null)
			{
				_daysSurvivedLabel.text = Loc.GameOverDaysSurvived(_gameTimeService.CurrentTime.Day);
			}

			_isReturning = false;
			_root.style.display = DisplayStyle.Flex;
			_root.SetEnabled(true);
		}

		private void Hide()
		{
			if (_root == null)
			{
				return;
			}

			_root.style.display = DisplayStyle.None;
		}

		private void OnMainMenuClicked()
		{
			if (_isReturning || _sceneLoader == null)
			{
				return;
			}

			_isReturning = true;
			_mainMenuButton.SetEnabled(false);
			_ = _sceneLoader.LoadMainMenu();
		}

		private void BuildUI()
		{
			UIDocument document = GetComponent<UIDocument>();
			VisualElement documentRoot = document.rootVisualElement;

			_root = new VisualElement();
			_root.name = "game-over-root";
			_root.style.position = Position.Absolute;
			_root.style.left = 0f;
			_root.style.right = 0f;
			_root.style.top = 0f;
			_root.style.bottom = 0f;
			_root.style.backgroundColor = new Color(0.02f, 0.03f, 0.05f, 0.92f);
			_root.style.alignItems = Align.Center;
			_root.style.justifyContent = Justify.Center;
			_root.style.display = DisplayStyle.None;

			VisualElement container = new();
			container.style.alignItems = Align.Center;

			Label titleLabel = new(Loc.GameOverTitle)
			{
				name = "game-over-title"
			};
			titleLabel.style.fontSize = 56;
			titleLabel.style.color = new Color(0.85f, 0.25f, 0.2f);
			titleLabel.style.marginBottom = 16f;
			container.Add(titleLabel);

			_daysSurvivedLabel = new Label
			{
				name = "game-over-days"
			};
			_daysSurvivedLabel.style.fontSize = 22;
			_daysSurvivedLabel.style.color = new Color(0.85f, 0.87f, 0.9f);
			_daysSurvivedLabel.style.marginBottom = 40f;
			container.Add(_daysSurvivedLabel);

			_mainMenuButton = new Button(OnMainMenuClicked)
			{
				name = "game-over-menu-button",
				text = Loc.GameOverReturnMenu
			};
			_mainMenuButton.style.fontSize = 20;
			_mainMenuButton.style.paddingTop = 10f;
			_mainMenuButton.style.paddingBottom = 10f;
			_mainMenuButton.style.paddingLeft = 40f;
			_mainMenuButton.style.paddingRight = 40f;
			container.Add(_mainMenuButton);

			_root.Add(container);
			documentRoot.Add(_root);
		}
	}
}
