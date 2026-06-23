using TLN.Application.GameStates;
using UnityEngine;
using VContainer;

namespace TLN.Gameplay.Survival
{
	public sealed class WorldSurvivalController : MonoBehaviour
	{
		private ISurvivalService _survivalService;
		private IGameStateMachine _gameStateMachine;
		private SurvivalWarningService _warningService;

		[Inject]
		public void Construct(
			ISurvivalService survivalService,
			SurvivalWarningService survivalWarningService,
			IGameStateMachine gameStateMachine
		)
		{
			_survivalService = survivalService;
			_gameStateMachine = gameStateMachine;
			_warningService = survivalWarningService;
		}

		private void Update()
		{
			if (_survivalService == null)
			{
				return;
			}

			if (_gameStateMachine != null && _gameStateMachine.CurrentState != GameStateId.Playing)
			{
				return;
			}

			_survivalService.Tick(UnityEngine.Time.deltaTime);
			_warningService?.Tick(UnityEngine.Time.unscaledTime);
		}
	}
}
