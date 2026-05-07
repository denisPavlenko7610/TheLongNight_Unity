using TLN.Application.GameStates;
using TLN.Application.Scenes;
using TLN.Core.GameStates;
using UnityEngine;

namespace TLN.UI.Pause
{
	public sealed class PauseDebugView : MonoBehaviour
	{
		private IGameStateMachine _gameStateMachine;

		private ISceneLoader _sceneLoader;

		public void Construct(IGameStateMachine gameStateMachine, ISceneLoader sceneLoader)
		{
			_gameStateMachine = gameStateMachine;
			_sceneLoader = sceneLoader;
		}

		private void OnGUI()
		{
			if (_gameStateMachine == null)
			{
				return;
			}

			if (_gameStateMachine.CurrentState != GameStateId.Paused)
			{
				return;
			}

			DrawPauseMenu();
		}

		private void DrawPauseMenu()
		{
			const int width = 260;
			const int height = 160;

			Rect boxRect = new Rect(
				(Screen.width - width) * 0.5f,
				(Screen.height - height) * 0.5f,
				width,
				height);

			GUI.Box(boxRect, "Paused");

			Rect resumeRect = new Rect(
				boxRect.x + 30,
				boxRect.y + 50,
				width - 60,
				40);

			if (GUI.Button(resumeRect, "Resume"))
			{
				_gameStateMachine.Enter(GameStateId.Playing);
			}

			Rect quitRect = new Rect(
				boxRect.x + 30,
				boxRect.y + 100,
				width - 60,
				40);

			if (GUI.Button(quitRect, "Quit to Main Menu"))
			{
				_sceneLoader.LoadMainMenu();			}
		}
	}
}
