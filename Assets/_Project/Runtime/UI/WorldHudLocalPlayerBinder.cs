using TLN.Application.Multiplayer;
using TLN.Gameplay.Player;
using TLN.Gameplay.Time;
using UnityEngine;
using VContainer;

namespace TLN.UI
{
	public sealed class WorldHudLocalPlayerBinder : MonoBehaviour
	{
		private WorldUIRoot _uiRoot;
		private IGameTimeService _gameTimeService;
		private IMultiplayerSessionService _multiplayerSessionService;
		private LocalPlayerService _localPlayerService;

		[Inject]
		public void Construct(
			WorldUIRoot uiRoot,
			IGameTimeService gameTimeService,
			IMultiplayerSessionService multiplayerSessionService,
			LocalPlayerService localPlayerService
		)
		{
			_uiRoot = uiRoot;
			_gameTimeService = gameTimeService;
			_multiplayerSessionService = multiplayerSessionService;
			_localPlayerService = localPlayerService;

			_localPlayerService.Changed += OnLocalPlayerChanged;

			TryBindHudToLocalPlayer();
		}

		private void OnDestroy()
		{
			if (_localPlayerService != null)
			{
				_localPlayerService.Changed -= OnLocalPlayerChanged;
			}
		}

		private void OnLocalPlayerChanged()
		{
			TryBindHudToLocalPlayer();
		}

		private void TryBindHudToLocalPlayer()
		{
			if (_multiplayerSessionService is not { IsMultiplayer: true })
			{
				return;
			}

			if (_uiRoot == null || _uiRoot.HUD == null)
			{
				return;
			}

			if (_localPlayerService is not { HasLocalPlayer: true } ||
				_localPlayerService.SurvivalService == null)
			{
				return;
			}

			_uiRoot.HUD.Construct(
				_localPlayerService.SurvivalService,
				_gameTimeService
			);
		}
	}
}
