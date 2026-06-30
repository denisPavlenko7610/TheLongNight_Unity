using System;
using TLN.Application.GameStates;
using TLN.Gameplay.Campfire;
using TLN.Gameplay.Items;
using TLN.Gameplay.Sleep;
using TLN.Gameplay.Time;
using Unity.Netcode;
using UnityEngine;
using VContainer;

namespace TLN.Gameplay.Survival
{
	[RequireComponent(typeof(NetworkObject))]
	public sealed class NetworkPlayerSurvival : NetworkBehaviour, ISurvivalService
	{
		private const float SecondsPerMinute = 60f;

		private readonly NetworkVariable<float> _networkHunger = new(
			0f,
			NetworkVariableReadPermission.Everyone,
			NetworkVariableWritePermission.Server
		);

		private readonly NetworkVariable<float> _networkThirst = new(
			0f,
			NetworkVariableReadPermission.Everyone,
			NetworkVariableWritePermission.Server
		);

		private readonly NetworkVariable<float> _networkFatigue = new(
			0f,
			NetworkVariableReadPermission.Everyone,
			NetworkVariableWritePermission.Server
		);

		private readonly NetworkVariable<float> _networkCold = new(
			0f,
			NetworkVariableReadPermission.Everyone,
			NetworkVariableWritePermission.Server
		);

		private readonly NetworkVariable<float> _networkCondition = new(
			100f,
			NetworkVariableReadPermission.Everyone,
			NetworkVariableWritePermission.Server
		);

		private SurvivalStat _hunger;
		private SurvivalStat _thirst;
		private SurvivalStat _fatigue;
		private SurvivalStat _cold;
		private SurvivalStat _condition;

		private SurvivalConfig _config;
		private SleepConfig _sleepConfig;
		private ItemCatalog _itemCatalog;
		private IGameStateMachine _gameStateMachine;
		private IGameTimeService _gameTimeService;
		private IWarmthService _warmthService;

		private bool _isConstructed;
		private bool _hasInitializedStats;
		private bool _hasPendingNetworkValues;

		private float _survivalTickAccumulator;

		public SurvivalStat Hunger
		{
			get
			{
				EnsureInitializedStats();
				return _hunger;
			}
		}

		public SurvivalStat Thirst
		{
			get
			{
				EnsureInitializedStats();
				return _thirst;
			}
		}

		public SurvivalStat Fatigue
		{
			get
			{
				EnsureInitializedStats();
				return _fatigue;
			}
		}

		public SurvivalStat Cold
		{
			get
			{
				EnsureInitializedStats();
				return _cold;
			}
		}

		public SurvivalStat Condition
		{
			get
			{
				EnsureInitializedStats();
				return _condition;
			}
		}

		public event Action Changed;

		[Inject]
		public void Construct(
			SurvivalConfig config,
			SleepConfig sleepConfig,
			ItemCatalog itemCatalog,
			IGameStateMachine gameStateMachine,
			IGameTimeService gameTimeService,
			IWarmthService warmthService
		)
		{
			_config = config ?? throw new ArgumentNullException(nameof(config));
			_sleepConfig = sleepConfig ?? throw new ArgumentNullException(nameof(sleepConfig));
			_itemCatalog = itemCatalog ?? throw new ArgumentNullException(nameof(itemCatalog));
			_gameStateMachine = gameStateMachine ?? throw new ArgumentNullException(nameof(gameStateMachine));
			_gameTimeService = gameTimeService ?? throw new ArgumentNullException(nameof(gameTimeService));
			_warmthService = warmthService ?? throw new ArgumentNullException(nameof(warmthService));

			_isConstructed = true;

			EnsureInitializedStats();
			InitializeNetworkStateIfNeeded();
		}

		public override void OnNetworkSpawn()
		{
			_networkHunger.OnValueChanged += OnNetworkSurvivalChanged;
			_networkThirst.OnValueChanged += OnNetworkSurvivalChanged;
			_networkFatigue.OnValueChanged += OnNetworkSurvivalChanged;
			_networkCold.OnValueChanged += OnNetworkSurvivalChanged;
			_networkCondition.OnValueChanged += OnNetworkSurvivalChanged;

			InitializeNetworkStateIfNeeded();
		}

		public override void OnNetworkDespawn()
		{
			_networkHunger.OnValueChanged -= OnNetworkSurvivalChanged;
			_networkThirst.OnValueChanged -= OnNetworkSurvivalChanged;
			_networkFatigue.OnValueChanged -= OnNetworkSurvivalChanged;
			_networkCold.OnValueChanged -= OnNetworkSurvivalChanged;
			_networkCondition.OnValueChanged -= OnNetworkSurvivalChanged;
		}

		private void Update()
		{
			if (!IsServer)
			{
				return;
			}

			if (!_isConstructed)
			{
				return;
			}

			if (_gameStateMachine.CurrentState != GameStateId.Playing)
			{
				return;
			}

			_survivalTickAccumulator += UnityEngine.Time.deltaTime;

			if (_survivalTickAccumulator < _config.SurvivalTickIntervalSeconds)
			{
				return;
			}

			Tick(_survivalTickAccumulator);
			_survivalTickAccumulator = 0f;
		}

		public void Tick(float deltaTime)
		{
			if (!CanMutateSurvival())
			{
				return;
			}

			if (deltaTime <= 0f)
			{
				return;
			}

			EnsureInitializedStats();

			float gameHours = ConvertRealDeltaTimeToGameHours(deltaTime);

			AddToStat(ref _hunger, _config.HungerPerHour * gameHours);
			AddToStat(ref _thirst, _config.ThirstPerHour * gameHours);
			AddToStat(ref _fatigue, _config.FatiguePerHour * gameHours);

			ApplyColdExposure(gameHours);
			ApplyConditionDamage(gameHours);

			PublishServerChange();
		}

		public void SetValues(float hunger, float thirst, float fatigue, float cold, float condition)
		{
			if (!CanMutateSurvival())
			{
				return;
			}

			SetLocalValues(hunger, thirst, fatigue, cold, condition, true);
			WriteNetworkValues(false);
		}

		public void ApplyConsumable(ConsumableItemDefinition consumable)
		{
			if (!CanMutateSurvival())
			{
				RequestApplyConsumable(consumable);
				return;
			}

			ApplyConsumableLocally(consumable);
		}

		public void ReduceCold(float amount)
		{
			if (!CanMutateSurvival())
			{
				RequestStatDelta(SurvivalStatId.Cold, -amount);
				return;
			}

			EnsureInitializedStats();
			SubtractFromStat(ref _cold, amount);
			PublishServerChange();
		}

		public void AddFatigue(float amount)
		{
			if (!CanMutateSurvival())
			{
				RequestStatDelta(SurvivalStatId.Fatigue, amount);
				return;
			}

			EnsureInitializedStats();
			AddToStat(ref _fatigue, amount);
			PublishServerChange();
		}

		public void ReduceFatigue(float amount)
		{
			if (!CanMutateSurvival())
			{
				RequestStatDelta(SurvivalStatId.Fatigue, -amount);
				return;
			}

			EnsureInitializedStats();
			SubtractFromStat(ref _fatigue, amount);
			PublishServerChange();
		}

		public void AddHunger(float amount)
		{
			if (!CanMutateSurvival())
			{
				RequestStatDelta(SurvivalStatId.Hunger, amount);
				return;
			}

			EnsureInitializedStats();
			AddToStat(ref _hunger, amount);
			PublishServerChange();
		}

		public void AddThirst(float amount)
		{
			if (!CanMutateSurvival())
			{
				RequestStatDelta(SurvivalStatId.Thirst, amount);
				return;
			}

			EnsureInitializedStats();
			AddToStat(ref _thirst, amount);
			PublishServerChange();
		}

		public void AddCold(float amount)
		{
			if (!CanMutateSurvival())
			{
				RequestStatDelta(SurvivalStatId.Cold, amount);
				return;
			}

			EnsureInitializedStats();
			AddToStat(ref _cold, amount);
			PublishServerChange();
		}

		public void RestoreCondition(float amount)
		{
			if (!CanMutateSurvival())
			{
				RequestStatDelta(SurvivalStatId.Condition, amount);
				return;
			}

			EnsureInitializedStats();
			AddToStat(ref _condition, amount);
			PublishServerChange();
		}

		public void DamageCondition(float amount)
		{
			if (!CanMutateSurvival())
			{
				RequestStatDelta(SurvivalStatId.Condition, -amount);
				return;
			}

			EnsureInitializedStats();
			SubtractFromStat(ref _condition, amount);
			PublishServerChange();
		}

		public bool RequestSleep(int hours)
		{
			if (!_isConstructed)
			{
				return false;
			}

			if (!IsSpawned || IsServer)
			{
				return ApplySleepLocally(hours);
			}

			if (!CanSendOwnerServerRequest())
			{
				return false;
			}

			RequestSleepServerRpc(hours);
			return true;
		}

		private void RequestApplyConsumable(ConsumableItemDefinition consumable)
		{
			if (!CanSendOwnerServerRequest())
			{
				return;
			}

			if (consumable == null || string.IsNullOrWhiteSpace(consumable.Id))
			{
				return;
			}

			ApplyConsumableServerRpc(consumable.Id);
		}

		private void RequestStatDelta(SurvivalStatId statId, float delta)
		{
			if (!CanSendOwnerServerRequest())
			{
				return;
			}

			if (!IsValidStatDelta(delta))
			{
				return;
			}

			ApplyStatDeltaServerRpc(statId, delta);
		}

		[Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
		private void ApplyConsumableServerRpc(string itemId)
		{
			if (!CanProcessServerPlayerRequest())
			{
				return;
			}

			if (string.IsNullOrWhiteSpace(itemId))
			{
				return;
			}

			if (!_itemCatalog.TryGetItem(itemId, out ItemDefinition item) ||
			    item is not ConsumableItemDefinition consumable)
			{
				return;
			}

			ApplyConsumableLocally(consumable);
		}

		[Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
		private void ApplyStatDeltaServerRpc(SurvivalStatId statId, float delta)
		{
			if (!CanProcessServerPlayerRequest())
			{
				return;
			}

			if (!IsValidStatDelta(delta))
			{
				return;
			}

			if (ApplyStatDelta(statId, delta))
			{
				PublishServerChange();
			}
		}

		[Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
		private void RequestSleepServerRpc(int hours)
		{
			if (!CanProcessServerPlayerRequest())
			{
				return;
			}

			ApplySleepLocally(hours);
		}

		private void InitializeNetworkStateIfNeeded()
		{
			if (!_isConstructed)
			{
				return;
			}

			if (!IsSpawned)
			{
				return;
			}

			if (IsServer)
			{
				WriteNetworkValues(true);
				return;
			}

			if (_hasPendingNetworkValues)
			{
				_hasPendingNetworkValues = false;
			}

			ApplyNetworkValuesToLocalStats();
		}

		private void EnsureInitializedStats()
		{
			if (_hasInitializedStats)
			{
				return;
			}

			if (!_isConstructed)
			{
				throw new InvalidOperationException(
					"NetworkPlayerSurvival must be constructed before survival stats are accessed."
				);
			}

			_hunger = new SurvivalStat(
				SurvivalStatId.Hunger,
				_config.InitialHunger,
				SurvivalService.MinStat,
				SurvivalService.MaxStat
			);

			_thirst = new SurvivalStat(
				SurvivalStatId.Thirst,
				_config.InitialThirst,
				SurvivalService.MinStat,
				SurvivalService.MaxStat
			);

			_fatigue = new SurvivalStat(
				SurvivalStatId.Fatigue,
				_config.InitialFatigue,
				SurvivalService.MinStat,
				SurvivalService.MaxStat
			);

			_cold = new SurvivalStat(
				SurvivalStatId.Cold,
				_config.InitialCold,
				SurvivalService.MinStat,
				SurvivalService.MaxStat
			);

			_condition = new SurvivalStat(
				SurvivalStatId.Condition,
				_config.InitialCondition,
				SurvivalService.MinStat,
				SurvivalService.MaxStat
			);

			_hasInitializedStats = true;
		}

		private bool CanMutateSurvival()
		{
			if (!_isConstructed)
			{
				return false;
			}

			return !IsSpawned || IsServer;
		}

		private bool CanSendOwnerServerRequest()
		{
			return _isConstructed && IsSpawned && IsClient && IsOwner;
		}

		private bool CanProcessServerPlayerRequest()
		{
			return _isConstructed &&
			       IsServer &&
			       _gameStateMachine.CurrentState == GameStateId.Playing;
		}

		private void ApplyConsumableLocally(ConsumableItemDefinition consumable)
		{
			if (consumable == null)
			{
				return;
			}

			EnsureInitializedStats();

			SubtractFromStat(ref _hunger, consumable.HungerRestore);
			SubtractFromStat(ref _thirst, consumable.ThirstRestore);
			SubtractFromStat(ref _fatigue, consumable.FatigueRestore);
			SubtractFromStat(ref _cold, consumable.ColdRestore);
			AddToStat(ref _condition, consumable.ConditionRestore);
			SubtractFromStat(ref _condition, consumable.ConditionDamage);

			PublishServerChange();
		}

		private bool ApplySleepLocally(int hours)
		{
			if (!IsValidSleepHours(hours))
			{
				return false;
			}

			if (_gameStateMachine.CurrentState != GameStateId.Playing)
			{
				return false;
			}

			EnsureInitializedStats();

			SubtractFromStat(ref _fatigue, _sleepConfig.FatigueRecoveryPerHour * hours);
			AddToStat(ref _hunger, _sleepConfig.HungerIncreasePerHour * hours);
			AddToStat(ref _thirst, _sleepConfig.ThirstIncreasePerHour * hours);
			AddToStat(ref _cold, _sleepConfig.ColdIncreasePerHour * hours);
			AddToStat(ref _condition, _sleepConfig.ConditionRecoveryPerHour * hours);

			PublishServerChange();
			_gameTimeService.AdvanceHours(hours);

			return true;
		}

		private bool ApplyStatDelta(SurvivalStatId statId, float delta)
		{
			if (!IsValidStatDelta(delta))
			{
				return false;
			}

			EnsureInitializedStats();

			switch (statId)
			{
				case SurvivalStatId.Hunger:
					ApplyDelta(ref _hunger, delta);
					return true;
				case SurvivalStatId.Thirst:
					ApplyDelta(ref _thirst, delta);
					return true;
				case SurvivalStatId.Fatigue:
					ApplyDelta(ref _fatigue, delta);
					return true;
				case SurvivalStatId.Cold:
					ApplyDelta(ref _cold, delta);
					return true;
				case SurvivalStatId.Condition:
					ApplyDelta(ref _condition, delta);
					return true;
				default:
					return false;
			}
		}

		private bool IsValidSleepHours(int hours)
		{
			return hours >= _sleepConfig.MinSleepHours &&
			       hours <= _sleepConfig.MaxSleepHours;
		}

		private static bool IsValidStatDelta(float delta)
		{
			return delta != 0f &&
			       !float.IsNaN(delta) &&
			       !float.IsInfinity(delta);
		}

		private void ApplyColdExposure(float gameHours)
		{
			float baseColdPerGameHour = Mathf.Max(0f, _config.ColdPerHour);
			float fireWarmthPerGameHour = GetFireWarmthPerGameHour();

			float coldChangePerGameHour = baseColdPerGameHour - fireWarmthPerGameHour;

			if (coldChangePerGameHour > 0f)
			{
				AddToStat(ref _cold, coldChangePerGameHour * gameHours);
				return;
			}

			if (coldChangePerGameHour < 0f)
			{
				SubtractFromStat(ref _cold, -coldChangePerGameHour * gameHours);
			}
		}

		private float GetFireWarmthPerGameHour()
		{
			return _warmthService.GetWarmthAt(transform.position);
		}

		private void ApplyConditionDamage(float gameHours)
		{
			float damage = 0f;

			if (_hunger.Value >= SurvivalService.MaxStat)
			{
				damage += _config.HungerConditionDamagePerHour * gameHours;
			}

			if (_thirst.Value >= SurvivalService.MaxStat)
			{
				damage += _config.ThirstConditionDamagePerHour * gameHours;
			}

			if (_fatigue.Value >= SurvivalService.MaxStat)
			{
				damage += _config.FatigueConditionDamagePerHour * gameHours;
			}

			if (_cold.Value >= SurvivalService.MaxStat)
			{
				damage += _config.ColdConditionDamagePerHour * gameHours;
			}

			if (damage > 0f)
			{
				SubtractFromStat(ref _condition, damage);
			}
		}

		private void PublishServerChange()
		{
			Changed?.Invoke();
			WriteNetworkValues(false);
		}

		private void WriteNetworkValues(bool force)
		{
			if (!IsServer)
			{
				return;
			}

			SetNetworkValue(_networkHunger, _hunger.Value, force);
			SetNetworkValue(_networkThirst, _thirst.Value, force);
			SetNetworkValue(_networkFatigue, _fatigue.Value, force);
			SetNetworkValue(_networkCold, _cold.Value, force);
			SetNetworkValue(_networkCondition, _condition.Value, force);
		}

		private void SetNetworkValue(
			NetworkVariable<float> networkVariable,
			float value,
			bool force
		)
		{
			if (!force &&
			    Mathf.Abs(networkVariable.Value - value) <= _config.NetworkSyncEpsilon)
			{
				return;
			}

			networkVariable.Value = value;
		}

		private void OnNetworkSurvivalChanged(float previousValue, float nextValue)
		{
			if (IsServer)
			{
				return;
			}

			if (!_isConstructed)
			{
				_hasPendingNetworkValues = true;
				return;
			}

			ApplyNetworkValuesToLocalStats();
		}

		private void ApplyNetworkValuesToLocalStats()
		{
			SetLocalValues(
				_networkHunger.Value,
				_networkThirst.Value,
				_networkFatigue.Value,
				_networkCold.Value,
				_networkCondition.Value,
				true
			);
		}

		private void SetLocalValues(
			float hunger,
			float thirst,
			float fatigue,
			float cold,
			float condition,
			bool notify
		)
		{
			EnsureInitializedStats();

			if (IsSameAsLocalValues(hunger, thirst, fatigue, cold, condition))
			{
				return;
			}

			_hunger.Set(hunger);
			_thirst.Set(thirst);
			_fatigue.Set(fatigue);
			_cold.Set(cold);
			_condition.Set(condition);

			if (notify)
			{
				Changed?.Invoke();
			}
		}

		private bool IsSameAsLocalValues(
			float hunger,
			float thirst,
			float fatigue,
			float cold,
			float condition
		)
		{
			float epsilon = _config.NetworkSyncEpsilon;

			return Mathf.Abs(_hunger.Value - hunger) <= epsilon &&
			       Mathf.Abs(_thirst.Value - thirst) <= epsilon &&
			       Mathf.Abs(_fatigue.Value - fatigue) <= epsilon &&
			       Mathf.Abs(_cold.Value - cold) <= epsilon &&
			       Mathf.Abs(_condition.Value - condition) <= epsilon;
		}

		private float ConvertRealDeltaTimeToGameHours(float deltaTime)
		{
			float realMinutes = deltaTime / SecondsPerMinute;
			return realMinutes * _config.GameHoursPerRealMinute;
		}

		private static void AddToStat(ref SurvivalStat stat, float amount)
		{
			if (amount <= 0f)
			{
				return;
			}

			stat.Add(amount);
		}

		private static void ApplyDelta(ref SurvivalStat stat, float delta)
		{
			if (delta > 0f)
			{
				AddToStat(ref stat, delta);
				return;
			}

			if (delta < 0f)
			{
				SubtractFromStat(ref stat, -delta);
			}
		}

		private static void SubtractFromStat(ref SurvivalStat stat, float amount)
		{
			if (amount <= 0f)
			{
				return;
			}

			stat.Subtract(amount);
		}
	}
}
