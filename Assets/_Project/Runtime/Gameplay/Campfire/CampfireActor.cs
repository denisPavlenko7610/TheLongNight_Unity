using System;
using Newtonsoft.Json;
using TLN.Application.Localization;
using TLN.Core.Logging;
using TLN.Gameplay.Interaction;
using TLN.Gameplay.Saves;
using TLN.Gameplay.Time;
using UnityEngine;
using VContainer;

namespace TLN.Gameplay.Campfire
{
	[RequireComponent(typeof(PersistentWorldEntity))]
	public sealed class CampfireActor : MonoBehaviour, IInteractable, IWarmthProvider, IWorldSaveable
	{
		[Header("Interaction")]
		[SerializeField] private string _interactionText = "Use campfire";

		[Header("Burning")]
		[SerializeField] private int _startBurnMinutes;
		[SerializeField] private int _maxBurnMinutes = 12 * 60;
		[SerializeField] private int _minimumBurnMinutesToIgnite = 1;

		[Header("Warmth")]
		[SerializeField] private float _warmthBonus = 20f;
		[SerializeField] private float _warmthRadius = 4f;

		[Header("Visuals")]
		[SerializeField] private GameObject _burningRoot;
		[SerializeField] private Light _fireLight;
		[SerializeField] private ParticleSystem _fireParticles;
		[SerializeField] private AudioSource _fireLoopAudio;

		private ICampfireWindow _campfireWindow;
		private IGameTimeService _gameTimeService;
		private ILocalizationService _localizationService;
		private int _lastKnownTotalMinutes;
		private int _remainingBurnMinutes;
		private IWarmthService _warmthService;
		private CampfireState _state = CampfireState.Unlit;

		public string InteractionText => _interactionText;
		public CampfireState State => _state;
		public bool IsBurning => _state == CampfireState.Burning;
		public int RemainingBurnMinutes => _remainingBurnMinutes;
		public int MaxBurnMinutes => _maxBurnMinutes;
		public float WarmthBonus => IsBurning ? _warmthBonus : 0f;
		public bool IsWarmthActive => IsBurning;
		public float WarmthRadius => _warmthRadius;
		public Vector3 Position => transform.position;

		public float FuelNormalized =>
			_maxBurnMinutes <= 0 ? 0f : Mathf.Clamp01((float)_remainingBurnMinutes / _maxBurnMinutes);

		public event Action Changed;
		public event Action BurnedOut;

		[Inject]
		public void Construct(ICampfireWindow campfireWindow, IGameTimeService gameTimeService, IWarmthService warmthService, ILocalizationService localizationService)
		{
			_campfireWindow = campfireWindow;
			_gameTimeService = gameTimeService;
			_warmthService = warmthService;
			_localizationService = localizationService;

			_warmthService?.Register(this);

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

			_warmthService?.Unregister(this);
		}

		#if UNITY_EDITOR
		private void OnDrawGizmosSelected()
		{
			Gizmos.DrawWireSphere(transform.position, _warmthRadius);
		}
		#endif

		public bool CanInteract(InteractionContext context)
		{
			return true;
		}

		public void Interact(InteractionContext context)
		{
			if (_campfireWindow == null)
			{
				TLNLogger.LogWarning("Cannot open campfire window because CampfireActor was not constructed.", this);
				return;
			}

			_campfireWindow.Show(this);
		}

		public bool CanAddFuel(FuelItemDefinition fuelDefinition, int amount, out string failureReason)
		{
			if (fuelDefinition == null)
			{
				failureReason = _localizationService.Get(LocalizationKeys.Campfire.FuelMissing);
				return false;
			}

			if (amount <= 0)
			{
				failureReason = _localizationService.Get(LocalizationKeys.Campfire.FuelAmountZero);
				return false;
			}

			if (fuelDefinition.BurnMinutes <= 0)
			{
				failureReason = _localizationService.Get(LocalizationKeys.Campfire.CannotBurn);
				return false;
			}

			if (_remainingBurnMinutes >= _maxBurnMinutes)
			{
				failureReason = _localizationService.Get(LocalizationKeys.Campfire.Full);
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
				_maxBurnMinutes
			);

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
				failureReason = _localizationService.Get(LocalizationKeys.Campfire.AlreadyBurning);
				return false;
			}

			if (_remainingBurnMinutes < _minimumBurnMinutesToIgnite)
			{
				failureReason = _localizationService.Get(LocalizationKeys.Campfire.NotEnoughFuel);
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
				failureReason = _localizationService.Get(LocalizationKeys.Campfire.NotBurning);
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

		public string SaveTypeId => "campfire";

		public string CaptureStateJson()
		{
			CampfireSaveData data = new CampfireSaveData
			{
				state = _state.ToString(),
				remainingBurnMinutes = _remainingBurnMinutes,
				lastKnownTotalMinutes = _lastKnownTotalMinutes
			};

			return JsonConvert.SerializeObject(data);
		}

		public void RestoreStateJson(string json)
		{
			if (string.IsNullOrWhiteSpace(json))
			{
				return;
			}

			CampfireSaveData data = JsonConvert.DeserializeObject<CampfireSaveData>(json);

			if (data == null)
			{
				return;
			}

			_remainingBurnMinutes = Mathf.Clamp(data.remainingBurnMinutes, 0, _maxBurnMinutes);

			_lastKnownTotalMinutes = data.lastKnownTotalMinutes;

			if (!Enum.TryParse(data.state, out CampfireState restoredState))
			{
				restoredState = _remainingBurnMinutes > 0
					? CampfireState.Unlit
					: CampfireState.BurnedOut;
			}

			_state = restoredState;

			ApplyVisualState();
			Changed?.Invoke();
		}

		private sealed class CampfireSaveData
		{
			public string state;
			public int remainingBurnMinutes;
			public int lastKnownTotalMinutes;
		}
	}
}
