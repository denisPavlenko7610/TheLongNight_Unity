using TLN.Application.Localization;
using TLN.Application.Notifications;
using TLN.Application.Saves;
using TLN.Gameplay.Survival;
using TLN.Gameplay.Time;

namespace TLN.Gameplay.Sleep
{
	public sealed class SleepService
	{
		private readonly SleepConfig _config;
		private readonly ISurvivalService _survivalService;
		private readonly INotificationService _notificationService;
		private readonly ILocalizationService _localizationService;
		private readonly IGameTimeService _gameTimeService;
		private readonly IGameSaveService _gameSaveService;

		public SleepService(
			SleepConfig config,
			ISurvivalService survivalService,
			INotificationService notificationService,
			ILocalizationService localizationService,
			IGameTimeService gameTimeService,
			IGameSaveService gameSaveService
		)
		{
			_config = config;
			_survivalService = survivalService;
			_notificationService = notificationService;
			_localizationService = localizationService;
			_gameTimeService = gameTimeService;
			_gameSaveService = gameSaveService;
		}

		public SleepResult Sleep(int hours)
		{
			if (hours < _config.MinSleepHours)
			{
				return SleepResult.Failure(_localizationService.Get(LocalizationTableNames.Gameplay, LocalizationKeys.Sleep.MinHours, _config.MinSleepHours));
			}

			if (hours > _config.MaxSleepHours)
			{
				return SleepResult.Failure(_localizationService.Get(LocalizationTableNames.Gameplay, LocalizationKeys.Sleep.MaxHours, _config.MaxSleepHours));
			}

			ApplySleepEffects(hours);
			_gameTimeService.AdvanceHours(hours);

			string message = _localizationService.Get(LocalizationTableNames.Gameplay, LocalizationKeys.Sleep.Result, hours);
			_notificationService.Show(message);

			_ = _gameSaveService.SaveCheckpoint(SaveTrigger.Sleep);

			return SleepResult.Success(message);
		}

		private void ApplySleepEffects(int hours)
		{
			_survivalService.ReduceFatigue(_config.FatigueRecoveryPerHour * hours);
			_survivalService.AddHunger(_config.HungerIncreasePerHour * hours);
			_survivalService.AddThirst(_config.ThirstIncreasePerHour * hours);
			_survivalService.AddCold(_config.ColdIncreasePerHour * hours);
			_survivalService.RestoreCondition(_config.ConditionRecoveryPerHour * hours);
		}
	}
}
