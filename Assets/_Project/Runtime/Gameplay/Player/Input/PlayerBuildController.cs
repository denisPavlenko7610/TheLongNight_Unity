using Assign;
using TLN.Application.GameStates;
using TLN.Gameplay.Building;
using UnityEngine;
using VContainer;

namespace TLN.Gameplay.Player.Input
{
	[RequireComponent(typeof(PlayerInputReader))]
	public sealed class PlayerBuildController : MonoBehaviour
	{
		[SerializeField, Assign] private PlayerInputReader _inputReader;

		private IBuildWindow _buildWindow;
		private IGameStateMachine _gameStateMachine;

		[Inject]
		public void Construct(IBuildWindow buildWindow, IGameStateMachine gameStateMachine)
		{
			_buildWindow = buildWindow;
			_gameStateMachine = gameStateMachine;
		}

		private void Awake()
		{
			if (_inputReader == null)
			{
				_inputReader = GetComponent<PlayerInputReader>();
			}
		}

		private void Update()
		{
			if (_buildWindow == null)
			{
				return;
			}

			if (_gameStateMachine != null && _gameStateMachine.CurrentState != GameStateId.Playing)
			{
				return;
			}

			if (!_inputReader.WasBuildPressedThisFrame)
			{
				return;
			}

			_buildWindow.Toggle();
		}
	}
}
