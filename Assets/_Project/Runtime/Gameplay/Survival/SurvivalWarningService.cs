using TLN.Application.Notifications;

namespace TLN.Gameplay.Survival
{
	public sealed class SurvivalWarningService
	{
		private const float DefaultWarningThreshold = 80f;
		private const float ConditionCriticalThreshold = 25f;

		private readonly ISurvivalService _survivalService;
		private readonly INotificationService _notificationService;

		private readonly float _warningCooldownSeconds;

		private float _nextHungerWarningTime;
		private float _nextThirstWarningTime;
		private float _nextFatigueWarningTime;
		private float _nextColdWarningTime;
		private float _nextConditionWarningTime;

		public SurvivalWarningService(ISurvivalService survivalService, INotificationService notificationService,
			float warningCooldownSeconds)
		{
			_survivalService = survivalService;
			_notificationService = notificationService;
			_warningCooldownSeconds = warningCooldownSeconds;
		}

		public void Tick(float unscaledTime)
		{
			CheckStatWarning(
				_survivalService.Hunger.Value,
				DefaultWarningThreshold,
				"You are hungry.",
				unscaledTime,
				ref _nextHungerWarningTime
			);

			CheckStatWarning(
				_survivalService.Thirst.Value,
				DefaultWarningThreshold,
				"You are thirsty.",
				unscaledTime,
				ref _nextThirstWarningTime
			);

			CheckStatWarning(
				_survivalService.Fatigue.Value,
				DefaultWarningThreshold,
				"You are exhausted.",
				unscaledTime,
				ref _nextFatigueWarningTime
			);

			CheckStatWarning(
				_survivalService.Cold.Value,
				DefaultWarningThreshold,
				"You are freezing.",
				unscaledTime,
				ref _nextColdWarningTime
			);

			CheckStatWarning(
				_survivalService.Condition.Value,
				ConditionCriticalThreshold,
				"Condition is critical.",
				unscaledTime,
				ref _nextConditionWarningTime,
				true
			);
		}

		private void CheckStatWarning(float value, float threshold, string message, float unscaledTime, ref float nextWarningTime,
			bool isLowerValueDangerous = false)
		{
			bool isDangerous = isLowerValueDangerous
				? value <= threshold
				: value >= threshold;

			if (!isDangerous)
			{
				return;
			}

			if (unscaledTime < nextWarningTime)
			{
				return;
			}

			_notificationService.Show(message);
			nextWarningTime = unscaledTime + _warningCooldownSeconds;
		}
	}
}
