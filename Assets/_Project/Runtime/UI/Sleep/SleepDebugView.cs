using TLN.Application.Input;
using TLN.Gameplay.Sleep;
using UnityEngine;

namespace TLN.UI.Sleep
{
	public sealed class SleepDebugView : MonoBehaviour, ISleepWindow
	{
		private SleepService _sleepService;
		private bool _isVisible;

		private IInputModeService _inputModeService;

		public void Construct(SleepService sleepService, IInputModeService inputModeService)
		{
			_sleepService = sleepService;
			_inputModeService = inputModeService;
		}

		public void Show()
		{
			_isVisible = true;
			_inputModeService?.SetUIMode();
		}

		public void Hide()
		{
			_isVisible = false;
			_inputModeService?.SetGameplayMode();
		}

		private void OnGUI()
		{
			if (!_isVisible)
			{
				return;
			}

			DrawWindow();
		}

		private void DrawWindow()
		{
			const int width = 300;
			const int height = 220;

			Rect boxRect = new Rect(
				(Screen.width - width) * 0.5f,
				(Screen.height - height) * 0.5f,
				width,
				height);

			GUI.Box(boxRect, "Sleep");

			DrawSleepButton(boxRect, 1, 45f);
			DrawSleepButton(boxRect, 2, 85f);
			DrawSleepButton(boxRect, 4, 125f);

			Rect cancelRect = new Rect(
				boxRect.x + 30f,
				boxRect.y + 165f,
				width - 60f,
				30f);

			if (GUI.Button(cancelRect, "Cancel"))
			{
				Hide();
			}
		}

		private void DrawSleepButton(Rect boxRect, int hours, float offsetY)
		{
			Rect buttonRect = new Rect(
				boxRect.x + 30f,
				boxRect.y + offsetY,
				boxRect.width - 60f,
				30f);

			if (GUI.Button(buttonRect, $"Sleep {hours} hour(s)"))
			{
				_sleepService?.Sleep(hours);
				Hide();
			}
		}
	}
}
