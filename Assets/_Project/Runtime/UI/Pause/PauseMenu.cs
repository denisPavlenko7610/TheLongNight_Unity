using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace TheLongNight.UI.PauseMenu
{
    public class PauseMenu : MonoBehaviour
    {
        [SerializeField] private Transform _root;
        [SerializeField] private GamePause _gamePause;
        
        [SerializeField] private Button _resumeButton;
        [SerializeField] private Button _saveButton;
        [SerializeField] private Button _loadButton;
        [SerializeField] private Button _optionsButton;
        [SerializeField] private Button _quitButton;

        private void OnEnable()
        {
            Subscribe();
        }

        private void Subscribe()
        {
            Unsubscribe();
        }

        public void ChangeVisibility(bool isVisible) => _root.gameObject.SetActive(isVisible);

        private void Unsubscribe()
        {
            _gamePause.PauseStateChanged += OnPauseChanged;
            _resumeButton.onClick.AddListener(OnResumeButtonClicked());
            _quitButton.onClick.AddListener(OnQuitClicked);
        }

        private void OnDisable()
        {
            _gamePause.PauseStateChanged -= OnPauseChanged;
            _resumeButton.onClick.RemoveListener(OnResumeButtonClicked());
            _quitButton.onClick.RemoveListener(OnQuitClicked);
        }

        private UnityAction OnResumeButtonClicked() => () => _gamePause.TogglePause();

        private void OnQuitClicked()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void OnPauseChanged(bool isPaused)
        {
            ChangeVisibility(isPaused);
        }
    }
}