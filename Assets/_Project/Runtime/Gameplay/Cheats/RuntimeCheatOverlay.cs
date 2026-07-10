using System;
using System.Collections.Generic;
using TLN.Application.Feedback;
using TLN.Gameplay.Inventory;
using TLN.Gameplay.Items;
using TLN.Gameplay.Player;
using TLN.Gameplay.Survival;
using TLN.Gameplay.Time;
using UnityEngine;
using UnityInput = UnityEngine.Input;
using UnityTime = UnityEngine.Time;

namespace TLN.Gameplay.Cheats
{
	public sealed class RuntimeCheatOverlay : MonoBehaviour
	{
		private const float ConditionDamageAmount = 25f;
		private const int DefaultItemAmount = 1;
		private const int WindowPadding = 18;
		private const int MaxVisibleItems = 12;

		private CheatSettings _settings;
		private IGameTimeService _timeService;
		private ISurvivalService _fallbackSurvivalService;
		private IInventoryService _fallbackInventoryService;
		private ItemCatalog _itemCatalog;
		private LocalPlayerService _localPlayerService;
		private IFeedbackService _feedbackService;

		private readonly List<ItemDefinition> _filteredItems = new();
		private bool _visible;
		private bool _cheatsEnabled;
		private string _status = "Ready.";
		private string _itemFilter = string.Empty;
		private int _selectedItemIndex;
		private int _itemAmount = DefaultItemAmount;
		private Vector2 _scroll;
		private CursorLockMode _previousCursorLockMode;
		private bool _previousCursorVisible;

		public void Initialize(
			CheatSettings settings,
			IGameTimeService timeService,
			ISurvivalService fallbackSurvivalService,
			IInventoryService fallbackInventoryService,
			ItemCatalog itemCatalog,
			LocalPlayerService localPlayerService,
			IFeedbackService feedbackService
		)
		{
			_settings = settings;
			_timeService = timeService;
			_fallbackSurvivalService = fallbackSurvivalService;
			_fallbackInventoryService = fallbackInventoryService;
			_itemCatalog = itemCatalog;
			_localPlayerService = localPlayerService;
			_feedbackService = feedbackService;
			_cheatsEnabled = settings.CanUseCheats;

			RebuildItemList();
		}

		private void Update()
		{
			if (_settings == null || !_settings.CanOpenRuntimeOverlay)
			{
				SetVisible(false);
				return;
			}

			if (UnityInput.GetKeyDown(_settings.RuntimeOverlayToggleKey))
			{
				SetVisible(!_visible);
			}
		}

		private void OnGUI()
		{
			if (!_visible)
			{
				return;
			}

			Rect screen = new Rect(0f, 0f, Screen.width, Screen.height);
			GUI.Box(screen, GUIContent.none);

			GUILayout.BeginArea(new Rect(
				WindowPadding,
				WindowPadding,
				Screen.width - WindowPadding * 2f,
				Screen.height - WindowPadding * 2f
			));

			DrawHeader();
			_scroll = GUILayout.BeginScrollView(_scroll);
			DrawTime();
			DrawSurvival();
			DrawInventory();
			DrawFeedback();
			GUILayout.EndScrollView();

			GUILayout.EndArea();
		}

		private void DrawHeader()
		{
			GUILayout.BeginHorizontal();
			GUILayout.Label("The Long Night Cheats", CreateHeaderStyle());
			GUILayout.FlexibleSpace();
			_cheatsEnabled = GUILayout.Toggle(_cheatsEnabled, "Cheats Enabled", GUILayout.Width(160f));

			if (GUILayout.Button("Close", GUILayout.Width(120f), GUILayout.Height(32f)))
			{
				SetVisible(false);
			}

			GUILayout.EndHorizontal();
			GUILayout.Label($"Toggle: {_settings.RuntimeOverlayToggleKey} | {(_cheatsEnabled ? "Enabled" : "Disabled")} | {_status}");
			GUILayout.Space(8f);
		}

		private void DrawTime()
		{
			GUILayout.Label(
				_timeService == null
					? "Time: service missing"
					: $"Time: {_timeService.CurrentTime} | Scale: {UnityTime.timeScale:0.##}x",
				CreateSectionStyle()
			);

			GUILayout.BeginHorizontal();
			CheatButton("+1h", () => Run(CheatActions.AdvanceHours(_timeService, 1), "Advanced time by 1h."));
			CheatButton("+6h", () => Run(CheatActions.AdvanceHours(_timeService, 6), "Advanced time by 6h."));
			CheatButton("06:00", () => Run(CheatActions.SetCurrentDayTime(_timeService, 6, 0), "Time set to 06:00."));
			CheatButton("18:00", () => Run(CheatActions.SetCurrentDayTime(_timeService, 18, 0), "Time set to 18:00."));
			CheatButton("0x", () => SetTimeScale(0f));
			CheatButton("1x", () => SetTimeScale(1f));
			CheatButton("3x", () => SetTimeScale(3f));
			GUILayout.EndHorizontal();
		}

		private void DrawSurvival()
		{
			ISurvivalService survival = GetSurvivalService();
			GUILayout.Label(GetSurvivalText(survival), CreateSectionStyle());

			GUILayout.BeginHorizontal();
			CheatButton("Restore Needs", () => Run(CheatActions.StabilizeSurvival(survival), "Survival restored."));
			CheatButton("Set Cold 100", () => Run(CheatActions.FreezePlayer(survival), "Cold set to 100."));
			CheatButton("Damage Condition", () => Run(
				CheatActions.DamageCondition(survival, ConditionDamageAmount),
				$"Condition damaged by {ConditionDamageAmount:0}."
			));
			GUILayout.EndHorizontal();
		}

		private void DrawInventory()
		{
			IInventoryService inventory = GetInventoryService();
			GUILayout.Label(GetInventoryText(inventory), CreateSectionStyle());

			GUILayout.BeginHorizontal();
			GUILayout.Label("Filter", GUILayout.Width(50f));
			string nextFilter = GUILayout.TextField(_itemFilter, GUILayout.MinWidth(160f));
			if (!string.Equals(_itemFilter, nextFilter, StringComparison.Ordinal))
			{
				_itemFilter = nextFilter;
				RebuildItemList();
			}

			_itemAmount = Mathf.Max(1, IntField(_itemAmount, GUILayout.Width(70f)));
			CheatButton("Starter Kit", () => Run(
				CheatActions.AddStarterKit(inventory, _itemCatalog?.Items) > 0,
				"Starter kit added."
			), 120f);
			CheatButton("Clear", () =>
			{
				if (!CanRunCheat())
				{
					return;
				}

				if (inventory == null)
				{
					SetStatus("Inventory service missing.");
					return;
				}

				inventory.ReplaceItems(Array.Empty<ItemStack>());
				SetStatus("Inventory cleared.");
			}, 90f);
			GUILayout.EndHorizontal();

			DrawItemList(inventory);
		}

		private void DrawItemList(IInventoryService inventory)
		{
			if (_filteredItems.Count == 0)
			{
				GUILayout.Label("No matching catalog items.");
				return;
			}

			int count = Mathf.Min(MaxVisibleItems, _filteredItems.Count);
			for (int i = 0; i < count; i++)
			{
				ItemDefinition item = _filteredItems[i];
				bool selected = i == _selectedItemIndex;

				GUILayout.BeginHorizontal();
				if (GUILayout.Toggle(selected, GUIContent.none, GUILayout.Width(24f)))
				{
					_selectedItemIndex = i;
				}

				GUILayout.Label($"{item.name} [{item.Id}]", GUILayout.MinWidth(180f));
				CheatButton("Add", () => AddItem(inventory, item), 90f);

				GUILayout.EndHorizontal();
			}
		}

		private void DrawFeedback()
		{
			GUILayout.Label("Feedback", CreateSectionStyle());
			CheatButton("Play Item Pickup", () =>
			{
				if (!CanRunCheat())
				{
					return;
				}

				_feedbackService?.Play(FeedbackEventId.ItemPickedUp);
				SetStatus("Played ItemPickedUp feedback.");
			}, 180f);
		}

		private void AddItem(IInventoryService inventory, ItemDefinition item)
		{
			if (!CanRunCheat())
			{
				return;
			}

			if (inventory == null || item == null)
			{
				SetStatus("Inventory service or item missing.");
				return;
			}

			InventoryAddResult result = inventory.AddItem(item, _itemAmount);
			SetStatus(result.IsSuccess
				? $"Added {_itemAmount} x {item.name}."
				: result.FailureReason);
		}

		private void RebuildItemList()
		{
			_filteredItems.Clear();

			IReadOnlyList<ItemDefinition> items = _itemCatalog?.Items;
			if (items == null)
			{
				return;
			}

			for (int i = 0; i < items.Count; i++)
			{
				ItemDefinition item = items[i];
				if (item == null || string.IsNullOrWhiteSpace(item.Id))
				{
					continue;
				}

				if (!string.IsNullOrWhiteSpace(_itemFilter) &&
				    item.name.IndexOf(_itemFilter, StringComparison.OrdinalIgnoreCase) < 0 &&
				    item.Id.IndexOf(_itemFilter, StringComparison.OrdinalIgnoreCase) < 0)
				{
					continue;
				}

				_filteredItems.Add(item);
			}

			_selectedItemIndex = Mathf.Clamp(_selectedItemIndex, 0, Mathf.Max(0, _filteredItems.Count - 1));
		}

		private ISurvivalService GetSurvivalService()
		{
			return _localPlayerService is { HasLocalPlayer: true }
				? _localPlayerService.SurvivalService
				: _fallbackSurvivalService;
		}

		private IInventoryService GetInventoryService()
		{
			return _localPlayerService is { HasLocalPlayer: true }
				? _localPlayerService.InventoryService
				: _fallbackInventoryService;
		}

		private static string GetSurvivalText(ISurvivalService survival)
		{
			if (survival == null)
			{
				return "Survival: service missing";
			}

			return $"Survival: H {survival.Hunger.Value:0} | T {survival.Thirst.Value:0} | F {survival.Fatigue.Value:0} | Cold {survival.Cold.Value:0} | Cond {survival.Condition.Value:0}";
		}

		private static string GetInventoryText(IInventoryService inventory)
		{
			if (inventory == null)
			{
				return "Inventory: service missing";
			}

			return $"Inventory: {inventory.Items.Count} stacks | {inventory.CurrentWeight:0.##}/{inventory.MaxCarryWeight:0.##}";
		}

		private void SetVisible(bool visible)
		{
			if (_visible == visible)
			{
				return;
			}

			_visible = visible;

			if (_visible)
			{
				_previousCursorLockMode = Cursor.lockState;
				_previousCursorVisible = Cursor.visible;
				Cursor.lockState = CursorLockMode.None;
				Cursor.visible = true;
				return;
			}

			Cursor.lockState = _previousCursorLockMode;
			Cursor.visible = _previousCursorVisible;
		}

		private void SetTimeScale(float timeScale)
		{
			if (!CanRunCheat())
			{
				return;
			}

			UnityTime.timeScale = Mathf.Max(0f, timeScale);
			SetStatus($"Time scale set to {UnityTime.timeScale:0.##}x.");
		}

		private void Run(bool success, string successStatus)
		{
			if (!CanRunCheat())
			{
				return;
			}

			SetStatus(success ? successStatus : "Cheat failed.");
		}

		private bool CanRunCheat()
		{
			if (_cheatsEnabled)
			{
				return true;
			}

			SetStatus("Cheats are disabled.");
			return false;
		}

		private void SetStatus(string status)
		{
			_status = status;
		}

		private static void Button(string label, Action clicked, float width = 100f)
		{
			if (GUILayout.Button(label, GUILayout.Width(width), GUILayout.Height(30f)))
			{
				clicked?.Invoke();
			}
		}

		private void CheatButton(string label, Action clicked, float width = 100f)
		{
			bool wasEnabled = GUI.enabled;
			GUI.enabled = wasEnabled && _cheatsEnabled;
			Button(label, clicked, width);
			GUI.enabled = wasEnabled;
		}

		private static int IntField(int value, params GUILayoutOption[] options)
		{
			string input = GUILayout.TextField(value.ToString(), options);
			return int.TryParse(input, out int parsed) ? parsed : value;
		}

		private static GUIStyle CreateHeaderStyle()
		{
			return new GUIStyle(GUI.skin.label)
			{
				fontSize = 24,
				fontStyle = FontStyle.Bold
			};
		}

		private static GUIStyle CreateSectionStyle()
		{
			return new GUIStyle(GUI.skin.label)
			{
				fontSize = 16,
				fontStyle = FontStyle.Bold
			};
		}
	}
}
