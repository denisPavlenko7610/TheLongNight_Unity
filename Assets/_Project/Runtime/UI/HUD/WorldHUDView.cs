using TLN.Application.Notifications;
using TLN.Gameplay.Interaction;
using TLN.Gameplay.Player.Input;
using TLN.Gameplay.Survival;
using TLN.Gameplay.Time;
using TLN.UI.Common;
using UnityEngine;
using UnityEngine.UIElements;
using UnityTime = UnityEngine.Time;

namespace TLN.UI.HUD
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class WorldHUDView : MonoBehaviour, IInteractionPromptView, INotificationView, ITimeOverlayView
    {
        private const string HiddenClassName = "hidden";

        private const float SurvivalIconSize = 42f;

        private const string TimeOverlayVisibleClassName = "time-overlay-visible";
        private const float TimeOverlayVisibleSeconds = 5f;

        [SerializeField] private float _notificationDuration = 2f;

        private UIDocument _document;

        private Label _timeDayLabel;

        private UIFillIcon _hungerIcon;
        private UIFillIcon _thirstIcon;
        private UIFillIcon _fatigueIcon;
        private UIFillIcon _coldIcon;
        private UIFillIcon _conditionIcon;

        private Label _interactionPromptLabel;
        private Label _notificationLabel;

        private ISurvivalService _survivalService;
        private IGameTimeService _gameTimeService;

        private float _notificationHideTime;
        private bool _isInitialized;

        private VisualElement _timeOverlay;
        private VisualElement _timeSunMoonIcon;
        private Label _timePeriodLabel;

        private float _timeOverlayHideTime;

        private void Awake()
        {
            EnsureInitialized();
        }

        private void EnsureInitialized()
        {
            if (_isInitialized)
            {
                return;
            }

            _document = GetComponent<UIDocument>();
            VisualElement root = _document.rootVisualElement;

            _timeDayLabel = root.Q<Label>("time-day-label");

            _hungerIcon = root.RequiredFillIcon("hunger-fill-mask", SurvivalIconSize);
            _thirstIcon = root.RequiredFillIcon("thirst-fill-mask", SurvivalIconSize);
            _fatigueIcon = root.RequiredFillIcon("fatigue-fill-mask", SurvivalIconSize);
            _coldIcon = root.RequiredFillIcon("cold-fill-mask", SurvivalIconSize);
            _conditionIcon = root.RequiredFillIcon("condition-fill-mask", SurvivalIconSize);

            _timeOverlay = root.RequiredQ<VisualElement>("time-overlay");
            _timeSunMoonIcon = root.RequiredQ<VisualElement>("time-sun-moon-icon");
            _timePeriodLabel = root.RequiredQ<Label>("time-period-label");

            _interactionPromptLabel = root.Q<Label>("interaction-prompt-label");
            _notificationLabel = root.Q<Label>("notification-label");

            HideInteractionPrompt();
            HideNotification();

            _isInitialized = true;
        }

        private void Update()
        {
            EnsureInitialized();

            RefreshAll();
            UpdateNotificationLifetime();
            UpdateTimeOverlayLifetime();
        }

        public void Construct(ISurvivalService survivalService, IGameTimeService gameTimeService)
        {
            EnsureInitialized();

            _survivalService = survivalService;
            _gameTimeService = gameTimeService;

            RefreshAll();
        }

        public void ShowTimeOverlay()
        {
            EnsureInitialized();

            if (_timeOverlay == null)
            {
                return;
            }

            RefreshTime();

            _timeOverlay.AddToClassList(TimeOverlayVisibleClassName);
            _timeOverlayHideTime = UnityTime.unscaledTime + TimeOverlayVisibleSeconds;
        }

        private void UpdateTimeOverlayLifetime()
        {
            if (_timeOverlay == null)
            {
                return;
            }

            if (!_timeOverlay.ClassListContains(TimeOverlayVisibleClassName))
            {
                return;
            }

            if (UnityTime.unscaledTime < _timeOverlayHideTime)
            {
                return;
            }

            _timeOverlay.RemoveFromClassList(TimeOverlayVisibleClassName);
        }

        private void RefreshAll()
        {
            RefreshTime();
            RefreshSurvival();
        }

        private void RefreshTime()
        {
            if (_gameTimeService == null)
            {
                return;
            }

            GameTime time = _gameTimeService.CurrentTime;

            SetLabel(_timeDayLabel, $"DAY {time.Day}");
            SetLabel(_timePeriodLabel, GetTimePeriodLabel(time.Hour));

            bool isNight = time.Hour is < 6 or >= 20;

            if (isNight)
            {
                _timeSunMoonIcon.RemoveFromClassList("icon-sun");
                _timeSunMoonIcon.AddToClassList("icon-moon");
            }
            else
            {
                _timeSunMoonIcon.RemoveFromClassList("icon-moon");
                _timeSunMoonIcon.AddToClassList("icon-sun");
            }
        }

        private static string GetTimePeriodLabel(int hour)
        {
            if (hour >= 5 && hour < 11)
            {
                return "MORNING";
            }

            if (hour >= 11 && hour < 17)
            {
                return "AFTERNOON";
            }

            if (hour >= 17 && hour < 21)
            {
                return "EVENING";
            }

            return "NIGHT";
        }

        private void RefreshSurvival()
        {
            if (_survivalService == null)
            {
                return;
            }

            _hungerIcon.SetValue(_survivalService.Hunger.Value / 100f);
            _thirstIcon.SetValue(_survivalService.Thirst.Value / 100f);
            _fatigueIcon.SetValue(_survivalService.Fatigue.Value / 100f);
            _coldIcon.SetValue(_survivalService.Cold.Value / 100f);
            _conditionIcon.SetValue(_survivalService.Condition.Value / 100f);
        }

        private void ShowInteractionPrompt(string text)
        {
            if (_interactionPromptLabel == null)
            {
                return;
            }

            _interactionPromptLabel.text = $"E — {text}";
            _interactionPromptLabel.RemoveFromClassList(HiddenClassName);
        }

        private void HideInteractionPrompt()
        {
            if (_interactionPromptLabel == null)
            {
                return;
            }

            _interactionPromptLabel.text = string.Empty;
            _interactionPromptLabel.AddToClassList(HiddenClassName);
        }

        private void ShowNotification(string message)
        {
            if (_notificationLabel == null)
            {
                return;
            }

            _notificationLabel.text = message;
            _notificationLabel.RemoveFromClassList(HiddenClassName);
            _notificationHideTime = UnityTime.unscaledTime + _notificationDuration;
        }

        private void UpdateNotificationLifetime()
        {
            if (_notificationLabel == null)
            {
                return;
            }

            if (_notificationLabel.ClassListContains(HiddenClassName))
            {
                return;
            }

            if (UnityTime.unscaledTime < _notificationHideTime)
            {
                return;
            }

            HideNotification();
        }

        private void HideNotification()
        {
            if (_notificationLabel == null)
            {
                return;
            }

            _notificationLabel.text = string.Empty;
            _notificationLabel.AddToClassList(HiddenClassName);
        }

        public void Show(string text)
        {
            ShowInteractionPrompt(text);
        }

        public void Hide()
        {
            HideInteractionPrompt();
        }

        void INotificationView.Show(string message)
        {
            ShowNotification(message);
        }

        private static void SetLabel(Label label, string text)
        {
            if (label != null)
            {
                label.text = text;
            }
        }
    }
}
