using TLN.Application.Multiplayer;
using TLN.Gameplay.Player;
using TLN.Gameplay.Time;
using UnityEngine;
using VContainer;

namespace TLN.UI
{
	public sealed class WorldLocalPlayerUiBinder : MonoBehaviour
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

			TryBindLocalPlayerUi();
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
			TryBindLocalPlayerUi();
		}

		private void TryBindLocalPlayerUi()
		{
			if (_multiplayerSessionService is not { IsMultiplayer: true })
			{
				return;
			}

			if (_uiRoot == null)
			{
				return;
			}

			if (_localPlayerService is not { HasLocalPlayer: true })
			{
				return;
			}

			BindHud();
			BindSurvivalMenu();
		}

		private void BindHud()
		{
			if (_uiRoot.HUD == null ||
				_localPlayerService.SurvivalService == null)
			{
				return;
			}

			_uiRoot.HUD.Construct(
				_localPlayerService.SurvivalService,
				_gameTimeService
			);
		}

		private void BindSurvivalMenu()
		{
			if (_uiRoot.SurvivalMenu == null ||
				_localPlayerService.InventoryService == null ||
				_localPlayerService.ItemUseService == null)
			{
				return;
			}

			_uiRoot.SurvivalMenu.BindInventory(
				_localPlayerService.InventoryService,
				_localPlayerService.ItemUseService
			);
		}
	}
}
