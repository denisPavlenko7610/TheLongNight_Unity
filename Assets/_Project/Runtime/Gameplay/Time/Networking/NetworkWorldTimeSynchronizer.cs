using TLN.Application.GameStates;
using TLN.Application.Multiplayer;
using Unity.Netcode;
using UnityEngine;
using VContainer;

namespace TLN.Gameplay.Time.Networking
{
	[RequireComponent(typeof(NetworkObject))]
	public sealed class NetworkWorldTimeSynchronizer : NetworkBehaviour
	{
		private const float SyncIntervalSeconds = 0.25f;

		private readonly NetworkVariable<int> _serverTotalMinutes = new(
			0,
			NetworkVariableReadPermission.Everyone,
			NetworkVariableWritePermission.Server
		);

		private IGameTimeService _gameTimeService;
		private IGameStateMachine _gameStateMachine;
		private IMultiplayerSessionService _multiplayerSessionService;

		private float _syncAccumulator;
		private int _lastSentTotalMinutes = -1;

		[Inject]
		public void Construct(
			IGameTimeService gameTimeService,
			IGameStateMachine gameStateMachine,
			IMultiplayerSessionService multiplayerSessionService
		)
		{
			_gameTimeService = gameTimeService;
			_gameStateMachine = gameStateMachine;
			_multiplayerSessionService = multiplayerSessionService;
		}

		public override void OnNetworkSpawn()
		{
			_serverTotalMinutes.OnValueChanged += OnServerTotalMinutesChanged;

			if (IsServer && _gameTimeService != null)
			{
				_serverTotalMinutes.Value = _gameTimeService.TotalMinutes;
				_lastSentTotalMinutes = _gameTimeService.TotalMinutes;
			}

			if (!IsServer)
			{
				ApplyServerTime(_serverTotalMinutes.Value);
			}
		}

		public override void OnNetworkDespawn()
		{
			_serverTotalMinutes.OnValueChanged -= OnServerTotalMinutesChanged;
		}

		private void Update()
		{
			if (_multiplayerSessionService is not { IsMultiplayer: true })
			{
				return;
			}

			if (!IsServer)
			{
				return;
			}

			if (_gameTimeService == null)
			{
				return;
			}

			if (_gameStateMachine != null &&
			    _gameStateMachine.CurrentState != GameStateId.Playing)
			{
				return;
			}

			_syncAccumulator += UnityEngine.Time.unscaledDeltaTime;

			if (_syncAccumulator < SyncIntervalSeconds)
			{
				return;
			}

			_syncAccumulator = 0f;
			SendServerTimeIfChanged();
		}

		private void SendServerTimeIfChanged()
		{
			int totalMinutes = _gameTimeService.TotalMinutes;

			if (totalMinutes == _lastSentTotalMinutes)
			{
				return;
			}

			_serverTotalMinutes.Value = totalMinutes;
			_lastSentTotalMinutes = totalMinutes;
		}

		private void OnServerTotalMinutesChanged(int previousValue, int nextValue)
		{
			if (IsServer)
			{
				return;
			}

			ApplyServerTime(nextValue);
		}

		private void ApplyServerTime(int totalMinutes)
		{
			if (_gameTimeService == null)
			{
				return;
			}

			if (_gameTimeService.TotalMinutes == totalMinutes)
			{
				return;
			}

			_gameTimeService.SetTotalMinutes(totalMinutes);
		}
	}
}
