using System;
using TLN.Core.Logging;
using TLN.Gameplay.Interaction;
using TLN.Gameplay.Time;
using UnityEngine;
using VContainer;

namespace TLN.Gameplay.Campfire
{
    public sealed class CampfireActor : MonoBehaviour, IInteractable
    {
        [Header("Interaction")]
        [SerializeField] private string _interactionText = "Use campfire";

        [Header("Burning")]
        [SerializeField] private int _startBurnMinutes;
        [SerializeField] private int _maxBurnMinutes = 720;
        [SerializeField] private int _minimumBurnMinutesToIgnite = 1;

        [Header("Warmth")]
        [SerializeField] private float _warmthBonus = 20f;

        [Header("Visuals")]
        [SerializeField] private GameObject _burningRoot;
        [SerializeField] private Light _fireLight;
        [SerializeField] private ParticleSystem _fireParticles;
        [SerializeField] private AudioSource _fireLoopAudio;

        private ICampfireWindow _campfireWindow;
        private IGameTimeService _gameTimeService;
        private int _lastKnownTotalMinutes;
        private int _remainingBurnMinutes;
        private CampfireState _state = CampfireState.Unlit;

        public string InteractionText => _interactionText;
        public CampfireState State => _state;
        public bool IsBurning => _state == CampfireState.Burning;
        public int RemainingBurnMinutes => _remainingBurnMinutes;
        public int MaxBurnMinutes => _maxBurnMinutes;
        public float WarmthBonus => IsBurning ? _warmthBonus : 0f;

        public float FuelNormalized
        {
            get
            {
                if (_maxBurnMinutes <= 0)
                {
                    return 0f;
                }

                return Mathf.Clamp01((float)_remainingBurnMinutes / _maxBurnMinutes);
            }
        }

        public event Action Changed;
        public event Action BurnedOut;

        [Inject]
        public void Construct(ICampfireWindow campfireWindow, IGameTimeService gameTimeService)
        {
            _campfireWindow = campfireWindow;
            _gameTimeService = gameTimeService;

            if (_gameTimeService != null)
            {
                _lastKnownTotalMinutes = _gameTimeService.TotalMinutes;
                _gameTimeService.Changed += OnGameTimeChanged;
            }
        }

        private void Awake()
        {
            _remainingBurnMinutes = Mathf.Clamp(_startBurnMinutes, 0, _maxBurnMinutes);

            if (_remainingBurnMinutes <= 0)
            {
                _state = CampfireState.Unlit;
            }

            ApplyVisualState();
        }

        private void OnDestroy()
        {
            if (_gameTimeService != null)
            {
                _gameTimeService.Changed -= OnGameTimeChanged;
            }
        }

        public bool CanInteract(InteractionContext context)
        {
            return true;
        }

        public void Interact(InteractionContext context)
        {
            if (_campfireWindow == null)
            {
                TLNLogger.Warning("Cannot open campfire window because CampfireActor was not constructed.", this);
                return;
            }

            _campfireWindow.Show(this);
        }

        public bool CanAddFuel(FuelItemDefinition fuelDefinition, int amount, out string failureReason)
        {
            if (fuelDefinition == null)
            {
                failureReason = "Fuel item is missing.";
                return false;
            }

            if (amount <= 0)
            {
                failureReason = "Fuel amount must be greater than zero.";
                return false;
            }

            if (fuelDefinition.BurnMinutes <= 0)
            {
                failureReason = "This item cannot burn.";
                return false;
            }

            if (_remainingBurnMinutes >= _maxBurnMinutes)
            {
                failureReason = "Campfire cannot accept more fuel.";
                return false;
            }

            failureReason = string.Empty;
            return true;
        }

        public bool AddFuel(FuelItemDefinition fuelDefinition, int amount, out string failureReason)
        {
            if (!CanAddFuel(fuelDefinition, amount, out failureReason))
            {
                return false;
            }

            int addedMinutes = fuelDefinition.BurnMinutes * amount;
            _remainingBurnMinutes = Mathf.Clamp(
                _remainingBurnMinutes + addedMinutes,
                0,
                _maxBurnMinutes);

            if (_state == CampfireState.BurnedOut && _remainingBurnMinutes > 0)
            {
                SetState(CampfireState.Unlit);
            }
            else
            {
                Changed?.Invoke();
            }

            return true;
        }

        public bool Ignite(out string failureReason)
        {
            if (IsBurning)
            {
                failureReason = "Campfire is already burning.";
                return false;
            }

            if (_remainingBurnMinutes < _minimumBurnMinutesToIgnite)
            {
                failureReason = "Not enough fuel to start a fire.";
                return false;
            }

            SetState(CampfireState.Burning);

            failureReason = string.Empty;
            return true;
        }

        public bool Extinguish(out string failureReason)
        {
            if (!IsBurning)
            {
                failureReason = "Campfire is not burning.";
                return false;
            }

            SetState(CampfireState.Unlit);

            failureReason = string.Empty;
            return true;
        }

        private void OnGameTimeChanged()
        {
            if (_gameTimeService == null)
            {
                return;
            }

            int currentTotalMinutes = _gameTimeService.TotalMinutes;
            int elapsedMinutes = currentTotalMinutes - _lastKnownTotalMinutes;
            _lastKnownTotalMinutes = currentTotalMinutes;

            if (elapsedMinutes <= 0)
            {
                return;
            }

            if (!IsBurning)
            {
                return;
            }

            Burn(elapsedMinutes);
        }

        private void Burn(int minutes)
        {
            _remainingBurnMinutes = Mathf.Max(0, _remainingBurnMinutes - minutes);

            if (_remainingBurnMinutes <= 0)
            {
                SetState(CampfireState.BurnedOut);
                BurnedOut?.Invoke();
                return;
            }

            Changed?.Invoke();
        }

        private void SetState(CampfireState state)
        {
            if (_state == state)
            {
                return;
            }

            _state = state;
            ApplyVisualState();
            Changed?.Invoke();
        }

        private void ApplyVisualState()
        {
            bool isBurning = IsBurning;

            if (_burningRoot != null)
            {
                _burningRoot.SetActive(isBurning);
            }

            if (_fireLight != null)
            {
                _fireLight.enabled = isBurning;
            }

            if (_fireParticles != null)
            {
                if (isBurning)
                {
                    _fireParticles.Play();
                }
                else
                {
                    _fireParticles.Stop();
                }
            }

            if (_fireLoopAudio != null)
            {
                if (isBurning && !_fireLoopAudio.isPlaying)
                {
                    _fireLoopAudio.Play();
                }
                else if (!isBurning && _fireLoopAudio.isPlaying)
                {
                    _fireLoopAudio.Stop();
                }
            }
        }
    }
}
