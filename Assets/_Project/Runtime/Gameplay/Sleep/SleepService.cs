using TLN.Application.Localization;
using TLN.Application.Multiplayer;
using TLN.Application.Notifications;
using TLN.Application.Saves;
using TLN.Gameplay.Player;
using TLN.Gameplay.Survival;
using TLN.Gameplay.Survival.Networking;
using TLN.Gameplay.Time;

namespace TLN.Gameplay.Sleep
{
	public sealed class SleepService
	{
		private readonly SleepConfig _config;
		private readonly ISurvivalService _survivalService;
		private readonly IMultiplayerSessionService _multiplayerSessionService;
		private readonly LocalPlayerService _localPlayerService;
		private readonly INotificationService _notificationService;

		private readonly IGameTimeService _gameTimeService;
		private readonly IGameSaveService _gameSaveService;

		public SleepService(
			SleepConfig config,
			ISurvivalService survivalService,
			IMultiplayerSessionService multiplayerSessionService,
			LocalPlayerService localPlayerService,
			INotificationService notificationService,

			IGameTimeService gameTimeService,
			IGameSaveService gameSaveService
		)
		{
			_config = config;
			_survivalService = survivalService;
			_multiplayerSessionService = multiplayerSessionService;
			_localPlayerService = localPlayerService;
			_notificationService = notificationService;

			_gameTimeService = gameTimeService;
			_gameSaveService = gameSaveService;
		}

		public SleepResult Sleep(int hours)
		{
			if (hours < _config.MinSleepHours)
			{
				return SleepResult.Failure(Loc.MinHours(_config.MinSleepHours));
			}

			if (hours > _config.MaxSleepHours)
			{
				return SleepResult.Failure(Loc.MaxHours(_config.MaxSleepHours));
			}

			if (TrySleepMultiplayer(hours, out SleepResult multiplayerResult))
			{
				return multiplayerResult;
			}

			ApplySleepEffects(_survivalService, hours);
			_gameTimeService.AdvanceHours(hours);

			string message = Loc.Result(hours);
			_notificationService.Show(message);

			_ = _gameSaveService.SaveCheckpoint(SaveTrigger.Sleep);

			return SleepResult.Success(message);
		}

		private bool TrySleepMultiplayer(int hours, out SleepResult result)
		{
			result = default;

			if (_multiplayerSessionService is not { IsMultiplayer: true })
			{
				return false;
			}

			if (_localPlayerService?.SurvivalService is not NetworkPlayerSurvival networkSurvival ||
			    !networkSurvival.RequestSleep(hours))
			{
				string failureMessage = Loc.CannotUse;
				_notificationService.Show(failureMessage);
				result = SleepResult.Failure(failureMessage);
				return true;
			}

			string message = Loc.Result(hours);
			_notificationService.Show(message);
			result = SleepResult.Success(message);
			return true;
		}

		private void ApplySleepEffects(ISurvivalService survivalService, int hours)
		{
			survivalService.ReduceFatigue(_config.FatigueRecoveryPerHour * hours);
			survivalService.AddHunger(_config.HungerIncreasePerHour * hours);
			survivalService.AddThirst(_config.ThirstIncreasePerHour * hours);
			survivalService.AddCold(_config.ColdIncreasePerHour * hours);
			survivalService.RestoreCondition(_config.ConditionRecoveryPerHour * hours);
		}
	}
}
