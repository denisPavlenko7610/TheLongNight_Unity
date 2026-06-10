using TLN.Application.GameStates;
using TLN.Core.GameStates;
using TLN.Gameplay.Campfire;
using TLN.Gameplay.Equipment;
using TLN.Gameplay.Survival;
using UnityEngine;

namespace TLN.Gameplay.Player
{
    public sealed class PlayerWarmthController : MonoBehaviour
    {
        private IWarmthService _warmthService;
        private IPlayerEquipmentService _equipmentService;
        private ISurvivalService _survivalService;
        private SurvivalConfig _survivalConfig;
        private IGameStateMachine _gameStateMachine;

        public void Construct(IWarmthService warmthService, IPlayerEquipmentService equipmentService, ISurvivalService survivalService,
            SurvivalConfig survivalConfig, IGameStateMachine gameStateMachine)
        {
            _warmthService = warmthService;
            _equipmentService = equipmentService;
            _survivalService = survivalService;
            _survivalConfig = survivalConfig;
            _gameStateMachine = gameStateMachine;
        }

        private void Update()
        {
            if (_survivalService == null)
            {
                return;
            }

            if (_survivalConfig == null)
            {
                return;
            }

            if (_gameStateMachine != null &&
                _gameStateMachine.CurrentState != GameStateId.Playing)
            {
                return;
            }

            ApplyColdExposure(UnityEngine.Time.deltaTime);
        }

        private void ApplyColdExposure(float deltaTime)
        {
            if (deltaTime <= 0f)
            {
                return;
            }

            float gameHours = ConvertRealDeltaTimeToGameHours(deltaTime);
            float coldChangePerGameHour = CalculateColdChangePerGameHour();

            if (coldChangePerGameHour > 0f)
            {
                _survivalService.AddCold(coldChangePerGameHour * gameHours);
                return;
            }

            if (coldChangePerGameHour < 0f)
            {
                _survivalService.ReduceCold(-coldChangePerGameHour * gameHours);
            }
        }

        private float CalculateColdChangePerGameHour()
        {
            float baseColdPerGameHour = Mathf.Max(0f, _survivalConfig.ColdPerHour);
            float fireWarmthPerGameHour = GetFireWarmthPerGameHour();
            float clothingWarmthPerGameHour = GetClothingWarmthPerGameHour();

            return baseColdPerGameHour
                   - fireWarmthPerGameHour
                   - clothingWarmthPerGameHour;
        }

        private float GetFireWarmthPerGameHour()
        {
            return _warmthService?.GetWarmthAt(transform.position) ?? 0f;
        }

        private float GetClothingWarmthPerGameHour()
        {
            return _equipmentService?.WarmthBonus ?? 0f;
        }

        private float ConvertRealDeltaTimeToGameHours(float deltaTime)
        {
            float realMinutes = deltaTime / 60f;
            return realMinutes * _survivalConfig.GameHoursPerRealMinute;
        }
    }
}
