using System;
using Newtonsoft.Json;
using TLN.Application.Audio;
using TLN.Application.Localization;
using TLN.Core.Logging;
using TLN.Gameplay.Interaction;
using TLN.Gameplay.Saves;
using TLN.Gameplay.Time;
using Unity.Netcode;
using UnityEngine;
using VContainer;
using TLN.Application.Feedback;

namespace TLN.Gameplay.Campfire
{
	[RequireComponent(typeof(PersistentWorldEntity))]
	[RequireComponent(typeof(NetworkObject))]
	public sealed class CampfireActor : NetworkBehaviour, IInteractable, IWarmthProvider, IWorldSaveable
	{
		private const string SaveType = "campfire";

		[Header("Interaction")]
		[SerializeField] private string _interactionText = "Use campfire";
		[SerializeField] private float _maxInteractionDistance = 3.5f;

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

		private readonly NetworkVariable<int> _networkRemainingBurnMinutes = new(
			0,
			NetworkVariableReadPermission.Everyone,
			NetworkVariableWritePermission.Server
		);

		private readonly NetworkVariable<CampfireState> _networkState = new(
			CampfireState.Unlit,
			NetworkVariableReadPermission.Everyone,
			NetworkVariableWritePermission.Server
		);

		private ICampfireWindow _campfireWindow;
		private IGameTimeService _gameTimeService;
		private IWarmthService _warmthService;
		private IAudioMixerService _audioMixerService;
		private IFeedbackService _feedbackService;

		private int _lastKnownTotalMinutes;
		private int _remainingBurnMinutes;
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
			_maxBurnMinutes <= 0
				? 0f
				: Mathf.Clamp01((float)_remainingBurnMinutes / _maxBurnMinutes);

		public event Action Changed;
		public event Action BurnedOut;

		public string SaveTypeId => SaveType;

		[Inject]
		public void Construct(
			ICampfireWindow campfireWindow,
			IGameTimeService gameTimeService,
			IWarmthService warmthService,
			IAudioMixerService audioMixerService,
			IFeedbackService feedbackService
		)
		{
			_campfireWindow = campfireWindow;
			_gameTimeService = gameTimeService;
			_warmthService = warmthService;
			_audioMixerService = audioMixerService;
			_feedbackService = feedbackService;

			_warmthService?.Register(this);
			RouteAudioSources();

			if (_gameTimeService != null)
			{
				_lastKnownTotalMinutes = _gameTimeService.TotalMinutes;
				_gameTimeService.Changed += OnGameTimeChanged;
			}
		}

		private void Awake()
		{
			_remainingBurnMinutes = Mathf.Clamp(
				_startBurnMinutes,
				0,
				_maxBurnMinutes
			);

			_state = _remainingBurnMinutes > 0
				? CampfireState.Unlit
				: CampfireState.Unlit;

			ApplyVisualState();
		}

		private void RouteAudioSources()
		{
			_audioMixerService?.Route(_fireLoopAudio, AudioBusId.Ambient);
		}

		public override void OnNetworkSpawn()
		{
			_networkRemainingBurnMinutes.OnValueChanged += OnNetworkRemainingBurnMinutesChanged;
			_networkState.OnValueChanged += OnNetworkStateChanged;

			if (IsServer)
			{
				WriteNetworkValues(true);
				return;
			}

			ApplyNetworkValues();
		}

		public override void OnNetworkDespawn()
		{
			_networkRemainingBurnMinutes.OnValueChanged -= OnNetworkRemainingBurnMinutesChanged;
			_networkState.OnValueChanged -= OnNetworkStateChanged;
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
			Gizmos.DrawWireSphere(transform.position, _maxInteractionDistance);
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
				TLNLogger.LogWarning(
					"Cannot open campfire window because CampfireActor was not constructed.",
					this
				);

				return;
			}

			_campfireWindow.Show(this);
		}

		public bool CanBeUsedBy(Transform playerTransform)
		{
			if (playerTransform == null)
			{
				return false;
			}

			float maxDistance = Mathf.Max(0f, _maxInteractionDistance);
			float maxSqrDistance = maxDistance * maxDistance;
			float sqrDistance =
				(playerTransform.position - transform.position).sqrMagnitude;

			return sqrDistance <= maxSqrDistance;
		}

		public bool CanAddFuel(
			FuelItemDefinition fuelDefinition,
			int amount,
			out string failureReason
		)
		{
			if (fuelDefinition == null)
			{
				failureReason = Loc.FuelMissing;
				return false;
			}

			if (amount <= 0)
			{
				failureReason = Loc.FuelAmountZero;
				return false;
			}

			if (fuelDefinition.BurnMinutes <= 0)
			{
				failureReason = Loc.CannotBurn;
				return false;
			}

			if (_remainingBurnMinutes >= _maxBurnMinutes)
			{
				failureReason = Loc.Full;
				return false;
			}

			failureReason = string.Empty;
			return true;
		}

		public bool AddFuel(
			FuelItemDefinition fuelDefinition,
			int amount,
			out string failureReason
		)
		{
			if (!CanMutateCampfire())
			{
				failureReason = Loc.CannotUse;
				return false;
			}

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
				PublishChanged();
			}

			return true;
		}

		public bool Ignite(out string failureReason)
		{
			if (!CanMutateCampfire())
			{
				failureReason = Loc.CannotUse;
				return false;
			}

			if (IsBurning)
			{
				failureReason = Loc.AlreadyBurning;
				return false;
			}

			if (_remainingBurnMinutes < _minimumBurnMinutesToIgnite)
			{
				failureReason = Loc.NotEnoughFuel;
				return false;
			}

			SetState(CampfireState.Burning);

			failureReason = string.Empty;
			return true;
		}

		public bool Extinguish(out string failureReason)
		{
			if (!CanMutateCampfire())
			{
				failureReason = Loc.CannotUse;
				return false;
			}

			if (!IsBurning)
			{
				failureReason = Loc.NotBurning;
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

			if (IsNetworkWorld() && !IsServer)
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
			_remainingBurnMinutes = Mathf.Max(
				0,
				_remainingBurnMinutes - minutes
			);

			if (_remainingBurnMinutes <= 0)
			{
				SetState(CampfireState.BurnedOut);
				BurnedOut?.Invoke();
				return;
			}

			PublishChanged();
		}

		private void SetState(CampfireState state)
		{
			if (_state == state)
			{
				return;
			}

			CampfireState previousState = _state;

			_state = state;

			ApplyVisualState();
			PlayStateFeedback(previousState, _state);
			PublishChanged();
		}

		private void PublishChanged()
		{
			WriteNetworkValues(false);
			Changed?.Invoke();
		}

		private void WriteNetworkValues(bool force)
		{
			if (!IsServer)
			{
				return;
			}

			if (force || _networkRemainingBurnMinutes.Value != _remainingBurnMinutes)
			{
				_networkRemainingBurnMinutes.Value = _remainingBurnMinutes;
			}

			if (force || _networkState.Value != _state)
			{
				_networkState.Value = _state;
			}
		}

		private void OnNetworkRemainingBurnMinutesChanged(
			int previousValue,
			int nextValue
		)
		{
			if (IsServer)
			{
				return;
			}

			_remainingBurnMinutes = nextValue;
			Changed?.Invoke();
		}

		private void OnNetworkStateChanged(
			CampfireState previousValue,
			CampfireState nextValue
		)
		{
			if (IsServer)
			{
				return;
			}

			_state = nextValue;

			ApplyVisualState();
			PlayStateFeedback(previousValue, nextValue);

			Changed?.Invoke();
		}

		private void ApplyNetworkValues()
		{
			_remainingBurnMinutes = _networkRemainingBurnMinutes.Value;
			_state = _networkState.Value;

			ApplyVisualState();
			Changed?.Invoke();
		}

		private void PlayStateFeedback(CampfireState previousState, CampfireState nextState)
		{
			if (_feedbackService == null)
			{
				return;
			}

			if (previousState == nextState)
			{
				return;
			}

			if (nextState == CampfireState.Burning)
			{
				_feedbackService.PlayAt(
					FeedbackEventId.CampfireIgnited,
					transform.position
				);

				return;
			}

			if (previousState == CampfireState.Burning)
			{
				_feedbackService.PlayAt(
					FeedbackEventId.CampfireExtinguished,
					transform.position
				);
			}
		}

		private bool CanMutateCampfire()
		{
			return !IsSpawned || IsServer;
		}

		private static bool IsNetworkWorld()
		{
			NetworkManager networkManager = NetworkManager.Singleton;

			return networkManager != null &&
			       networkManager.IsListening;
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

			CampfireSaveData data =
				JsonConvert.DeserializeObject<CampfireSaveData>(json);

			if (data == null)
			{
				return;
			}

			_remainingBurnMinutes = Mathf.Clamp(
				data.remainingBurnMinutes,
				0,
				_maxBurnMinutes
			);

			_lastKnownTotalMinutes = data.lastKnownTotalMinutes;

			if (!Enum.TryParse(data.state, out CampfireState restoredState))
			{
				restoredState = _remainingBurnMinutes > 0
					? CampfireState.Unlit
					: CampfireState.BurnedOut;
			}

			_state = restoredState;

			ApplyVisualState();
			PublishChanged();
		}

		private sealed class CampfireSaveData
		{
			public string state;
			public int remainingBurnMinutes;
			public int lastKnownTotalMinutes;
		}
	}
}
