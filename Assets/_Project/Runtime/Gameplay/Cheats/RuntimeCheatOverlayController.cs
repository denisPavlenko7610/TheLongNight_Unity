using System;
using TLN.Application.Feedback;
using TLN.Gameplay.Inventory;
using TLN.Gameplay.Items;
using TLN.Gameplay.Player;
using TLN.Gameplay.Survival;
using TLN.Gameplay.Time;
using UnityEngine;
using VContainer.Unity;
using Object = UnityEngine.Object;

namespace TLN.Gameplay.Cheats
{
	public sealed class RuntimeCheatOverlayController : IInitializable, IDisposable
	{
		private const string SettingsResourcePath = "Cheats/CheatSettings";
		private const string OverlayName = "[TLN Runtime Cheat Overlay]";

		private readonly IGameTimeService _timeService;
		private readonly ISurvivalService _fallbackSurvivalService;
		private readonly IInventoryService _fallbackInventoryService;
		private readonly ItemCatalog _itemCatalog;
		private readonly LocalPlayerService _localPlayerService;
		private readonly IFeedbackService _feedbackService;

		private RuntimeCheatOverlay _overlay;

		public RuntimeCheatOverlayController(
			IGameTimeService timeService,
			ISurvivalService fallbackSurvivalService,
			IInventoryService fallbackInventoryService,
			ItemCatalog itemCatalog,
			LocalPlayerService localPlayerService,
			IFeedbackService feedbackService
		)
		{
			_timeService = timeService;
			_fallbackSurvivalService = fallbackSurvivalService;
			_fallbackInventoryService = fallbackInventoryService;
			_itemCatalog = itemCatalog;
			_localPlayerService = localPlayerService;
			_feedbackService = feedbackService;
		}

		public void Initialize()
		{
			CheatSettings settings = Resources.Load<CheatSettings>(SettingsResourcePath);
			if (settings == null)
			{
				return;
			}

			GameObject overlayObject = new GameObject(OverlayName);
			_overlay = overlayObject.AddComponent<RuntimeCheatOverlay>();
			_overlay.Initialize(
				settings,
				_timeService,
				_fallbackSurvivalService,
				_fallbackInventoryService,
				_itemCatalog,
				_localPlayerService,
				_feedbackService
			);
		}

		public void Dispose()
		{
			if (_overlay != null)
			{
				Object.Destroy(_overlay.gameObject);
			}
		}
	}
}
