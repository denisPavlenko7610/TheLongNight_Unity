using TLN.Application.GameStates;
using TLN.Application.Multiplayer;
using UnityEngine;
using VContainer;

namespace TLN.Gameplay.Time
{
	public sealed class WorldTimeController : MonoBehaviour
	{
		private IGameTimeService _gameTimeService;
		private IGameStateMachine _gameStateMachine;
		private IMultiplayerSessionService _multiplayerSessionService;

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

		private void Update()
		{
			if (_gameTimeService == null)
			{
				return;
			}

			if (_gameStateMachine != null &&
				_gameStateMachine.CurrentState != GameStateId.Playing)
			{
				return;
			}

			if (!ShouldSimulateWorld())
			{
				return;
			}

			#if UNITY_EDITOR
			HandleEditorCheats();
			#endif

			_gameTimeService.Tick(UnityEngine.Time.deltaTime);
		}

		private bool ShouldSimulateWorld()
		{
			return _multiplayerSessionService is not { IsMultiplayer: true } ||
			       _multiplayerSessionService.IsServer;
		}

		#if UNITY_EDITOR
		private void HandleEditorCheats()
		{
			if (!UnityEngine.Input.GetKeyDown(KeyCode.F4))
			{
				return;
			}

			_gameTimeService.AdvanceHours(1);
			Debug.Log($"[TLN Editor Cheat] Advanced time to {_gameTimeService.CurrentTime}.");
		}
		#endif
	}
}
