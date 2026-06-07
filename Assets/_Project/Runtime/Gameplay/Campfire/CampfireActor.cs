using System;
using UnityEngine;

namespace TLN.Gameplay.Campfire
{
    public sealed class CampfireActor : MonoBehaviour, IWarmthProvider
    {
        [Header("Burning")]
        [SerializeField] private float _startBurnSeconds;
        [SerializeField] private float _maxBurnSeconds = 7200f; //120m
        [SerializeField] private float _burnSecondsPerRealSecond = 1f;
        [SerializeField] private float _minimumBurnSecondsToIgnite = 10f;

        [Header("Warmth")]
        [SerializeField] private float _warmthBonus = 20f;

        [Header("Visuals")]
        [SerializeField] private GameObject _burningRoot;
        [SerializeField] private Light _fireLight;
        [SerializeField] private ParticleSystem _fireParticles;
        [SerializeField] private AudioSource _fireLoopAudio;

        private float _remainingBurnSeconds;
        private int _lastNotifiedRemainingBurnSeconds = -1;
        private CampfireState _state = CampfireState.Unlit;

        public CampfireState State => _state;
        public bool IsBurning => _state == CampfireState.Burning;
        public bool IsWarmthActive => IsBurning;
        public float WarmthBonus => IsBurning ? _warmthBonus : 0f;
        public Vector3 Position => transform.position;

        public float RemainingBurnSeconds => _remainingBurnSeconds;

        public float FuelNormalized
        {
            get
            {
                if (_maxBurnSeconds <= 0f)
                {
                    return 0f;
                }

                return Mathf.Clamp01(_remainingBurnSeconds / _maxBurnSeconds);
            }
        }

        public event Action Changed;
        public event Action BurnedOut;

        private void Awake()
        {
            _remainingBurnSeconds = Mathf.Clamp(_startBurnSeconds, 0f, _maxBurnSeconds);

            if (_remainingBurnSeconds <= 0f)
            {
                _state = CampfireState.Unlit;
            }

            ApplyVisualState();
        }

        private void Update()
        {
            if (!IsBurning)
            {
                return;
            }

            float burnAmount = _burnSecondsPerRealSecond * UnityEngine.Time.deltaTime;
            _remainingBurnSeconds = Mathf.Max(0f, _remainingBurnSeconds - burnAmount);

            if (_remainingBurnSeconds <= 0f)
            {
                SetState(CampfireState.BurnedOut);
                BurnedOut?.Invoke();
                return;
            }

            NotifyChangedIfBurnSecondsChanged();
        }

        public bool AddFuel(CampfireFuelDefinition fuelDefinition, int amount, out string failureReason)
        {
            if (fuelDefinition == null)
            {
                failureReason = "Fuel definition is missing.";
                return false;
            }

            if (amount <= 0)
            {
                failureReason = "Fuel amount must be greater than zero.";
                return false;
            }

            float addedBurnSeconds = fuelDefinition.BurnSeconds * amount;
            _remainingBurnSeconds = Mathf.Clamp(
                _remainingBurnSeconds + addedBurnSeconds,
                0f,
                _maxBurnSeconds);

            if (_state == CampfireState.BurnedOut && _remainingBurnSeconds > 0f)
            {
                SetState(CampfireState.Unlit);
            }
            else
            {
                Changed?.Invoke();
            }

            failureReason = string.Empty;
            return true;
        }

        public bool Ignite(out string failureReason)
        {
            if (IsBurning)
            {
                failureReason = "Campfire is already burning.";
                return false;
            }

            if (_remainingBurnSeconds < _minimumBurnSecondsToIgnite)
            {
                failureReason = "Not enough fuel to start a fire.";
                return false;
            }

            SetState(CampfireState.Burning);

            failureReason = string.Empty;
            return true;
        }

        public void Extinguish()
        {
            if (!IsBurning)
            {
                return;
            }

            SetState(CampfireState.Unlit);
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
            _lastNotifiedRemainingBurnSeconds = Mathf.CeilToInt(_remainingBurnSeconds);
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
                if (isBurning && !_fireParticles.isPlaying)
                {
                    _fireParticles.Play();
                }
                else if (!isBurning && _fireParticles.isPlaying)
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

        private void NotifyChangedIfBurnSecondsChanged()
        {
            int remainingWholeSeconds = Mathf.CeilToInt(_remainingBurnSeconds);

            if (remainingWholeSeconds == _lastNotifiedRemainingBurnSeconds)
            {
                return;
            }

            _lastNotifiedRemainingBurnSeconds = remainingWholeSeconds;
            Changed?.Invoke();
        }
    }
}
