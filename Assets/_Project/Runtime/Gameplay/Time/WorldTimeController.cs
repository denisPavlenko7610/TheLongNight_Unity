using TLN.Application.GameStates;
using UnityEngine;
using VContainer;

namespace TLN.Gameplay.Time
{
	public sealed class WorldTimeController : MonoBehaviour
	{
		private IGameTimeService _gameTimeService;
		private IGameStateMachine _gameStateMachine;

		[Inject]
		public void Construct(IGameTimeService gameTimeService, IGameStateMachine gameStateMachine)
		{
			_gameTimeService = gameTimeService;
			_gameStateMachine = gameStateMachine;
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

#if UNITY_EDITOR
			HandleEditorCheats();
#endif

			_gameTimeService.Tick(UnityEngine.Time.deltaTime);
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
