using System;
using UnityEngine;

namespace TheLongNight.UI.PauseMenu
{
    [DisallowMultipleComponent]
    public class GamePause : MonoBehaviour
    {
        public bool IsPaused { get; private set; }

        public event Action<bool> PauseStateChanged;

        public void TogglePause()
        {
            SetPause(!IsPaused);
        }

        private void SetPause(bool pause)
        {
            if (IsPaused == pause)
                return;

            IsPaused = pause;

            Time.timeScale = pause ? 0f : 1f;
            Cursor.lockState = pause ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = pause;

            PauseStateChanged?.Invoke(pause);
        }
    }
}