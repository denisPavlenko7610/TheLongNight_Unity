using TLN.Application.GameStates;
using TLN.Gameplay.Player.Input;
using TLN.Gameplay.Survival;
using UnityEngine;
using VContainer;

namespace TLN.Gameplay.Player
{
	/// <summary>
	/// Sprinting burns energy: while moving at a sprint the player fatigues
	/// and consumes hunger/thirst noticeably faster than the passive rates.
	/// </summary>
	public sealed class PlayerExertionController : MonoBehaviour
	{
		private const float MinMoveInput = 0.1f;
		private const float SecondsPerMinute = 60f;

		[SerializeField] private PlayerInputReader _inputReader;

		private ISurvivalService _survivalService;
		private SurvivalConfig _survivalConfig;
		private IGameStateMachine _gameStateMachine;

		private void Awake()
		{
			if (_inputReader == null)
			{
				_inputReader = GetComponent<PlayerInputReader>();
			}
		}

		[Inject]
		public void Construct(
			ISurvivalService survivalService,
			SurvivalConfig survivalConfig,
			IGameStateMachine gameStateMachine
		)
		{
			_survivalService = survivalService;
			_survivalConfig = survivalConfig;
			_gameStateMachine = gameStateMachine;
		}

		private void Update()
		{
			if (_survivalService == null || _survivalConfig == null)
			{
				return;
			}

			if (_gameStateMachine != null &&
			    !_gameStateMachine.IsCurrent(GameStateId.Playing))
			{
				return;
			}

			if (_inputReader == null ||
			    !_inputReader.IsSprintHeld ||
			    _inputReader.Move.magnitude < MinMoveInput)
			{
				return;
			}

			float dt = UnityEngine.Time.deltaTime;
			float minutes = dt / SecondsPerMinute;

			_survivalService.AddFatigue(_survivalConfig.SprintFatiguePerMinute * minutes);
			_survivalService.AddHunger(_survivalConfig.SprintHungerPerMinute * minutes);
			_survivalService.AddThirst(_survivalConfig.SprintThirstPerMinute * minutes);
		}
	}
}
