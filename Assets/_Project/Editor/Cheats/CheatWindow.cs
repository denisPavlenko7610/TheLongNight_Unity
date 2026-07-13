using System;
using System.Collections.Generic;
using System.IO;
using TLN.Application.Feedback;
using TLN.Application.Multiplayer;
using TLN.Editor.Feedback;
using TLN.Gameplay.Cheats;
using TLN.Gameplay.Feedback;
using TLN.Gameplay.Inventory;
using TLN.Gameplay.Items;
using TLN.Gameplay.Player;
using TLN.Gameplay.Survival;
using TLN.Gameplay.Time;
using TLN.Gameplay.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;
using UnityApplication = UnityEngine.Application;
using UnityTime = UnityEngine.Time;

namespace TLN.Editor.Cheats
{
	public sealed class CheatWindow : EditorWindow
	{
		private static class Paths
		{
			public const string ProjectRoot = "Assets/_Project";
			public const string SettingsAsset = "Assets/_Project/Resources/Cheats/CheatSettings.asset";
		}

		private static class Labels
		{
			public const string MenuPath = "Tools/TLN/Cheat Panel";
			public const string WindowTitle = "TLN Cheats";
			public const string NoItems = "No catalog items";
			public const string NoFeedback = "No feedback entries";
		}

		private static class VContainerApi
		{
			public const string LifetimeScopeType = "VContainer.Unity.LifetimeScope, VContainer";
			public const string ContainerProperty = "Container";
			public const string TryResolveMethod = "TryResolve";
		}

		private const double RefreshIntervalSeconds = 0.5d;
		private const float ConditionDamageAmount = 25f;

		private readonly List<ItemDefinition> _items = new();
		private readonly List<FeedbackDefinition> _feedbackDefinitions = new();

		private CheatSettings _settings;
		private CheatContext _context = new();
		private Vector2 _scrollPosition;
		private double _nextRefreshTime;
		private string _status = "Ready.";
		private MessageType _statusType = MessageType.Info;
		private string[] _itemChoices = Array.Empty<string>();
		private string[] _feedbackChoices = Array.Empty<string>();
		private int _selectedItemIndex;
		private int _selectedFeedbackIndex;
		private int _itemAmount = 1;
		private string _itemFilter = string.Empty;

		[MenuItem(Labels.MenuPath)]
		public static void ShowWindow()
		{
			CheatWindow window = GetWindow<CheatWindow>();
			window.titleContent = new GUIContent(Labels.WindowTitle);
			window.minSize = new Vector2(380f, 520f);
			window.Show();
		}

		private void OnEnable()
		{
			titleContent = new GUIContent(Labels.WindowTitle);
			EditorApplication.update += RefreshOnInterval;
			EditorApplication.playModeStateChanged += OnPlayModeChanged;
		}

		private void OnDisable()
		{
			EditorApplication.update -= RefreshOnInterval;
			EditorApplication.playModeStateChanged -= OnPlayModeChanged;
		}

		public void CreateGUI()
		{
			rootVisualElement.Clear();
			rootVisualElement.Add(new IMGUIContainer(DrawPanel));
			RefreshAll();
		}

		private void RefreshOnInterval()
		{
			if (EditorApplication.timeSinceStartup < _nextRefreshTime)
			{
				return;
			}

			_nextRefreshTime = EditorApplication.timeSinceStartup + RefreshIntervalSeconds;
			RefreshContext();
			Repaint();
		}

		private void OnPlayModeChanged(PlayModeStateChange state)
		{
			RefreshAll();
		}

		private void RefreshAll()
		{
			_settings = LoadOrCreateSettings();
			RefreshContext();
			ReloadItems();
			ReloadFeedback();
			Repaint();
		}

		private void RefreshContext()
		{
			_context = CheatContext.Find();
		}

		private void DrawPanel()
		{
			_scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

			DrawHeader();
			DrawSettings();
			EditorGUILayout.HelpBox(
				_context.IsClientOnlyMultiplayer
					? "Multiplayer client: authoritative cheats must be run on host/server."
					: _status,
				_context.IsClientOnlyMultiplayer ? MessageType.Warning : _statusType);

			using (new EditorGUI.DisabledScope(!CheatActions.CanRun(_settings) || _context.IsClientOnlyMultiplayer))
			{
				DrawPlayerCheats();
				DrawTimeCheats();
				DrawSurvivalCheats();
				DrawInventoryCheats();
				DrawFeedbackCheats();
			}

			EditorGUILayout.EndScrollView();
		}

		private void DrawHeader()
		{
			EditorGUILayout.Space(6f);
			EditorGUILayout.LabelField("The Long Night", EditorStyles.boldLabel);
			EditorGUILayout.LabelField(UnityApplication.isPlaying ? "Play Mode" : "Edit Mode", EditorStyles.miniLabel);
			EditorGUILayout.Space(6f);
		}

		private void DrawSettings()
		{
			EditorGUILayout.LabelField("Cheats", EditorStyles.boldLabel);

			EditorGUI.BeginChangeCheck();
			bool enabled = EditorGUILayout.Toggle("Enabled", _settings != null && _settings.Enabled);
			bool allowInPlayerBuilds = EditorGUILayout.Toggle("Allow In Player Builds", _settings != null && _settings.AllowInPlayerBuilds);
			bool showRuntimeOverlay = EditorGUILayout.Toggle("Runtime Overlay", _settings != null && _settings.ShowRuntimeOverlay);
			KeyCode runtimeOverlayToggleKey = (KeyCode)EditorGUILayout.EnumPopup(
				"Runtime Toggle Key",
				_settings == null ? KeyCode.None : _settings.RuntimeOverlayToggleKey
			);
			if (EditorGUI.EndChangeCheck() && _settings != null)
			{
				_settings.EditorSetEnabled(enabled);
				_settings.EditorSetAllowInPlayerBuilds(allowInPlayerBuilds);
				_settings.EditorSetShowRuntimeOverlay(showRuntimeOverlay);
				_settings.EditorSetRuntimeOverlayToggleKey(runtimeOverlayToggleKey);
				AssetDatabase.SaveAssets();
			}

			using (new EditorGUILayout.HorizontalScope())
			{
				if (GUILayout.Button("Refresh"))
				{
					RefreshAll();
				}

				if (GUILayout.Button("Select Settings"))
				{
					Selection.activeObject = _settings;
					EditorGUIUtility.PingObject(_settings);
				}
			}
		}

		private void DrawPlayerCheats()
		{
			DrawSectionTitle("Player");
			EditorGUILayout.LabelField(_context.PlayerRoot == null
				? "PlayerRoot: not found"
				: $"{_context.PlayerRoot.name} at {FormatVector(_context.PlayerRoot.transform.position)}");

			using (new EditorGUILayout.HorizontalScope())
			{
				if (GUILayout.Button("Select"))
				{
					SelectPlayer();
				}

				if (GUILayout.Button("Teleport To View"))
				{
					TeleportPlayerToSceneView();
				}

				if (GUILayout.Button("Spawn Point"))
				{
					TeleportPlayerToSpawnPoint();
				}
			}
		}

		private void DrawTimeCheats()
		{
			DrawSectionTitle("Time");
			EditorGUILayout.LabelField(_context.Time == null
				? $"Game time: not found | scale {UnityTime.timeScale:0.##}x"
				: $"{_context.Time.CurrentTime} | scale {UnityTime.timeScale:0.##}x");

			using (new EditorGUILayout.HorizontalScope())
			{
				if (GUILayout.Button("+1h"))
				{
					AdvanceHours(1);
				}

				if (GUILayout.Button("+6h"))
				{
					AdvanceHours(6);
				}

				if (GUILayout.Button("06:00"))
				{
					SetCurrentDayTime(6, 0);
				}

				if (GUILayout.Button("18:00"))
				{
					SetCurrentDayTime(18, 0);
				}
			}

			using (new EditorGUILayout.HorizontalScope())
			{
				if (GUILayout.Button("0x"))
				{
					SetTimeScale(0f);
				}

				if (GUILayout.Button("1x"))
				{
					SetTimeScale(1f);
				}

				if (GUILayout.Button("3x"))
				{
					SetTimeScale(3f);
				}
			}
		}

		private void DrawSurvivalCheats()
		{
			DrawSectionTitle("Survival");
			EditorGUILayout.LabelField(TryReadSurvival(out SurvivalValues values)
				? $"H {values.Hunger:0} | T {values.Thirst:0} | F {values.Fatigue:0} | Cold {values.Cold:0} | Cond {values.Condition:0}"
				: "Survival: not found");

			using (new EditorGUILayout.HorizontalScope())
			{
				if (GUILayout.Button("Restore Needs"))
				{
					StabilizeSurvival();
				}

				if (GUILayout.Button("Set Cold 100"))
				{
					FreezePlayer();
				}

				if (GUILayout.Button("Damage Condition"))
				{
					DamageCondition();
				}
			}
		}

		private void DrawInventoryCheats()
		{
			DrawSectionTitle("Inventory");
			EditorGUILayout.LabelField(_context.Inventory == null
				? $"Inventory: not found | catalog items {_items.Count}"
				: $"Stacks {_context.Inventory.Items.Count} | weight {_context.Inventory.CurrentWeight:0.##}/{_context.Inventory.MaxCarryWeight:0.##}" +
				$" | catalog items {_items.Count}");

			EditorGUI.BeginChangeCheck();
			_itemFilter = EditorGUILayout.TextField("Filter", _itemFilter);
			if (EditorGUI.EndChangeCheck())
			{
				ReloadItems();
			}

			_selectedItemIndex = EditorGUILayout.Popup("Item", _selectedItemIndex, _itemChoices);
			_itemAmount = Mathf.Max(1, EditorGUILayout.IntField("Amount", _itemAmount));

			using (new EditorGUILayout.HorizontalScope())
			{
				if (GUILayout.Button("Add"))
				{
					AddSelectedItem();
				}

				if (GUILayout.Button("Starter Kit"))
				{
					AddStarterKit();
				}

				if (GUILayout.Button("Clear"))
				{
					ClearInventory();
				}
			}
		}

		private void DrawFeedbackCheats()
		{
			DrawSectionTitle("Feedback Preview");
			EditorGUILayout.LabelField(_context.FeedbackCatalog == null
				? "FeedbackCatalog: not found"
				: $"{_context.FeedbackCatalog.name} | entries {_feedbackDefinitions.Count}");

			_selectedFeedbackIndex = EditorGUILayout.Popup("Event", _selectedFeedbackIndex, _feedbackChoices);

			using (new EditorGUILayout.HorizontalScope())
			{
				if (GUILayout.Button("Play 2D"))
				{
					PlayFeedback(false);
				}

				if (GUILayout.Button("At Player"))
				{
					PlayFeedback(true);
				}

				if (GUILayout.Button("Rebuild"))
				{
					FeedbackCatalogTools.RebuildFeedbackCatalogs();
					RefreshAll();
					SetStatus("Feedback catalogs rebuilt.");
				}
			}
		}

		private void ReloadItems()
		{
			_items.Clear();

			IReadOnlyList<ItemDefinition> catalogItems = _context.ItemCatalog?.Items;
			if (catalogItems != null)
			{
				for (int i = 0; i < catalogItems.Count; i++)
				{
					ItemDefinition item = catalogItems[i];
					if (IsMatchingItem(item))
					{
						_items.Add(item);
					}
				}
			}

			_items.Sort((left, right) =>
				string.Compare(GetItemLabel(left), GetItemLabel(right), StringComparison.OrdinalIgnoreCase));

			_itemChoices = _items.Count == 0
				? new[] { Labels.NoItems }
				: BuildItemChoices();
			_selectedItemIndex = Mathf.Clamp(_selectedItemIndex, 0, _itemChoices.Length - 1);
		}

		private void ReloadFeedback()
		{
			_feedbackDefinitions.Clear();

			IReadOnlyList<FeedbackDefinition> definitions = _context.FeedbackCatalog?.Definitions;
			if (definitions != null)
			{
				for (int i = 0; i < definitions.Count; i++)
				{
					FeedbackDefinition definition = definitions[i];
					if (definition != null && definition.EventId != FeedbackEventId.None)
					{
						_feedbackDefinitions.Add(definition);
					}
				}
			}

			_feedbackDefinitions.Sort((left, right) => left.EventId.CompareTo(right.EventId));
			_feedbackChoices = _feedbackDefinitions.Count == 0
				? new[] { Labels.NoFeedback }
				: BuildFeedbackChoices();
			_selectedFeedbackIndex = Mathf.Clamp(_selectedFeedbackIndex, 0, _feedbackChoices.Length - 1);
		}

		private string[] BuildItemChoices()
		{
			string[] choices = new string[_items.Count];
			for (int i = 0; i < _items.Count; i++)
			{
				choices[i] = $"{GetItemLabel(_items[i])} [{_items[i].Id}]";
			}

			return choices;
		}

		private string[] BuildFeedbackChoices()
		{
			string[] choices = new string[_feedbackDefinitions.Count];
			for (int i = 0; i < _feedbackDefinitions.Count; i++)
			{
				FeedbackDefinition definition = _feedbackDefinitions[i];
				choices[i] = $"{definition.EventId} [{definition.name}]";
			}

			return choices;
		}

		private void SelectPlayer()
		{
			if (!RequirePlayer(out PlayerRoot playerRoot))
			{
				return;
			}

			Selection.activeGameObject = playerRoot.gameObject;
			EditorGUIUtility.PingObject(playerRoot);
		}

		private void TeleportPlayerToSceneView()
		{
			if (!RequirePlayer(out PlayerRoot playerRoot))
			{
				return;
			}

			Camera sceneCamera = SceneView.lastActiveSceneView?.camera;
			if (sceneCamera == null)
			{
				SetStatus("Scene View camera not found.", MessageType.Warning);
				return;
			}

			TeleportPlayer(playerRoot, sceneCamera.transform.position + sceneCamera.transform.forward * 2f, sceneCamera.transform.rotation);
			SetStatus("Player teleported to Scene View.");
		}

		private void TeleportPlayerToSpawnPoint()
		{
			if (!RequirePlayer(out PlayerRoot playerRoot))
			{
				return;
			}

			PlayerSpawnPoint spawnPoint = Object.FindAnyObjectByType<PlayerSpawnPoint>(FindObjectsInactive.Exclude);
			if (spawnPoint == null)
			{
				SetStatus("PlayerSpawnPoint not found.", MessageType.Warning);
				return;
			}

			TeleportPlayer(playerRoot, spawnPoint.transform.position, spawnPoint.transform.rotation);
			SetStatus("Player teleported to spawn point.");
		}

		private static void TeleportPlayer(PlayerRoot playerRoot, Vector3 position, Quaternion rotation)
		{
			CharacterController controller = playerRoot.GetComponent<CharacterController>();
			bool wasEnabled = controller != null && controller.enabled;

			if (wasEnabled)
			{
				controller.enabled = false;
			}

			playerRoot.transform.SetPositionAndRotation(position, rotation);

			if (wasEnabled)
			{
				controller.enabled = true;
			}
		}

		private void AdvanceHours(int hours)
		{
			SetStatus(CheatActions.AdvanceHours(_context.Time, hours)
				? $"Advanced time by {hours}h."
				: "GameTimeService not found.");
		}

		private void SetCurrentDayTime(int hour, int minute)
		{
			SetStatus(CheatActions.SetCurrentDayTime(_context.Time, hour, minute)
				? $"Time set to {hour:00}:{minute:00}."
				: "GameTimeService not found.");
		}

		private void SetTimeScale(float scale)
		{
			UnityTime.timeScale = Mathf.Max(0f, scale);
			SetStatus($"Time scale set to {UnityTime.timeScale:0.##}x.");
		}

		private void StabilizeSurvival()
		{
			SetStatus(CheatActions.StabilizeSurvival(_context.Survival)
				? "Survival stabilized."
				: "SurvivalService not found.");
		}

		private void FreezePlayer()
		{
			SetStatus(CheatActions.FreezePlayer(_context.Survival)
				? "Cold set to 100."
				: "SurvivalService not found.");
		}

		private void DamageCondition()
		{
			SetStatus(CheatActions.DamageCondition(_context.Survival, ConditionDamageAmount)
				? $"Condition damaged by {ConditionDamageAmount:0}."
				: "SurvivalService not found.");
		}

		private void AddSelectedItem()
		{
			if (_context.Inventory == null || _items.Count == 0)
			{
				SetStatus("InventoryService or item is missing.", MessageType.Warning);
				return;
			}

			ItemDefinition item = _items[_selectedItemIndex];
			InventoryAddResult result = _context.Inventory.AddItem(item, _itemAmount);
			SetStatus(result.IsSuccess
				? $"Added {_itemAmount} x {GetItemLabel(item)}."
				: result.FailureReason,
				result.IsSuccess ? MessageType.Info : MessageType.Warning);
		}

		private void AddStarterKit()
		{
			int count = CheatActions.AddStarterKit(_context.Inventory, _items);
			SetStatus(count > 0
				? $"Starter kit added: {count} stacks."
				: "Starter kit could not add any item.",
				count > 0 ? MessageType.Info : MessageType.Warning);
		}

		private void ClearInventory()
		{
			if (_context.Inventory == null)
			{
				SetStatus("InventoryService not found.", MessageType.Warning);
				return;
			}

			_context.Inventory.ReplaceItems(Array.Empty<ItemStack>());
			SetStatus("Inventory cleared.");
		}

		private void PlayFeedback(bool atPlayer)
		{
			if (_context.Feedback == null || _feedbackDefinitions.Count == 0)
			{
				SetStatus("FeedbackService or event is missing.", MessageType.Warning);
				return;
			}

			FeedbackEventId eventId = _feedbackDefinitions[_selectedFeedbackIndex].EventId;
			if (atPlayer)
			{
				if (!RequirePlayer(out PlayerRoot playerRoot))
				{
					return;
				}

				_context.Feedback.PlayAt(eventId, playerRoot.transform.position);
				SetStatus($"Played feedback at player: {eventId}.");
				return;
			}

			_context.Feedback.Play(eventId);
			SetStatus($"Played feedback: {eventId}.");
		}

		private bool RequirePlayer(out PlayerRoot playerRoot)
		{
			playerRoot = _context.PlayerRoot;
			if (playerRoot != null)
			{
				return true;
			}

			SetStatus("PlayerRoot not found.", MessageType.Warning);
			return false;
		}

		private bool TryReadSurvival(out SurvivalValues values)
		{
			values = default;

			try
			{
				if (_context.Survival == null)
				{
					return false;
				}

				values = new SurvivalValues(
					_context.Survival.Hunger.Value,
					_context.Survival.Thirst.Value,
					_context.Survival.Fatigue.Value,
					_context.Survival.Cold.Value,
					_context.Survival.Condition.Value
				);
				return true;
			}
			catch
			{
				return false;
			}
		}

		private bool IsMatchingItem(ItemDefinition item)
		{
			if (item == null || string.IsNullOrWhiteSpace(item.Id))
			{
				return false;
			}

			if (string.IsNullOrWhiteSpace(_itemFilter))
			{
				return true;
			}

			return GetItemLabel(item).IndexOf(_itemFilter, StringComparison.OrdinalIgnoreCase) >= 0 ||
			       item.Id.IndexOf(_itemFilter, StringComparison.OrdinalIgnoreCase) >= 0 ||
			       item.Category.ToString().IndexOf(_itemFilter, StringComparison.OrdinalIgnoreCase) >= 0;
		}

		private void SetStatus(string message, MessageType type = MessageType.Info)
		{
			_status = message;
			_statusType = type;
			Repaint();
		}

		private static void DrawSectionTitle(string title)
		{
			EditorGUILayout.Space(8f);
			EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
		}

		private static CheatSettings LoadOrCreateSettings()
		{
			CheatSettings settings = AssetDatabase.LoadAssetAtPath<CheatSettings>(Paths.SettingsAsset);
			if (settings != null)
			{
				return settings;
			}

			Directory.CreateDirectory(Path.GetDirectoryName(Paths.SettingsAsset));
			settings = CreateInstance<CheatSettings>();
			AssetDatabase.CreateAsset(settings, Paths.SettingsAsset);
			AssetDatabase.SaveAssets();
			return settings;
		}

		private static T LoadFirstAsset<T>() where T : Object
		{
			string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { Paths.ProjectRoot });

			for (int i = 0; i < guids.Length; i++)
			{
				T asset = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guids[i]));
				if (asset != null)
				{
					return asset;
				}
			}

			return null;
		}

		private static string GetItemLabel(ItemDefinition item)
		{
			return string.IsNullOrWhiteSpace(item.name) ? item.Id : item.name;
		}

		private static string FormatVector(Vector3 value)
		{
			return $"{value.x:0.0}, {value.y:0.0}, {value.z:0.0}";
		}

		private sealed class CheatContext
		{
			public PlayerRoot PlayerRoot { get; private set; }
			public IGameTimeService Time { get; private set; }
			public ISurvivalService Survival { get; private set; }
			public IInventoryService Inventory { get; private set; }
			public ItemCatalog ItemCatalog { get; private set; }
			public IFeedbackService Feedback { get; private set; }
			public FeedbackCatalog FeedbackCatalog { get; private set; }
			public IMultiplayerSessionService MultiplayerSession { get; private set; }
			public bool IsMultiplayer => MultiplayerSession is { IsMultiplayer: true };
			public bool IsClientOnlyMultiplayer => MultiplayerSession is { IsMultiplayer: true, IsServer: false };

			public static CheatContext Find()
			{
				CheatContext context = new();

				if (UnityApplication.isPlaying)
				{
					FillFromLifetimeScopes(context);
				}

				if (!context.IsMultiplayer)
				{
					context.PlayerRoot ??= Object.FindAnyObjectByType<PlayerRoot>(FindObjectsInactive.Exclude);
					if (context.PlayerRoot != null)
					{
						context.Survival ??= context.PlayerRoot.GetComponent<ISurvivalService>();
						context.Inventory ??= context.PlayerRoot.GetComponent<IInventoryService>();
					}
				}

				context.ItemCatalog ??= LoadFirstAsset<ItemCatalog>();
				context.FeedbackCatalog ??= LoadFirstAsset<FeedbackCatalog>();

				return context;
			}

			private static void FillFromLifetimeScopes(CheatContext context)
			{
				Type lifetimeScopeType = Type.GetType(VContainerApi.LifetimeScopeType);
				if (lifetimeScopeType == null)
				{
					return;
				}

				Object[] scopes = Resources.FindObjectsOfTypeAll(lifetimeScopeType);
				for (int i = 0; i < scopes.Length; i++)
				{
					if (scopes[i] is not Component scope || EditorUtility.IsPersistent(scope))
					{
						continue;
					}

					object container = lifetimeScopeType.GetProperty(VContainerApi.ContainerProperty)?.GetValue(scope);
					if (container == null)
					{
						continue;
					}

					context.Time ??= ResolveOrNull<IGameTimeService>(container);
					context.Survival ??= ResolveOrNull<ISurvivalService>(container);
					context.Inventory ??= ResolveOrNull<IInventoryService>(container);
					context.ItemCatalog ??= ResolveOrNull<ItemCatalog>(container);
					context.Feedback ??= ResolveOrNull<IFeedbackService>(container);
					context.FeedbackCatalog ??= ResolveOrNull<FeedbackCatalog>(container);
					context.MultiplayerSession ??= ResolveOrNull<IMultiplayerSessionService>(container);

					LocalPlayerService localPlayer = ResolveOrNull<LocalPlayerService>(container);
					if (localPlayer is not { HasLocalPlayer: true })
					{
						continue;
					}

					context.PlayerRoot ??= localPlayer.PlayerRoot;
					context.Survival = localPlayer.SurvivalService ?? context.Survival;
					context.Inventory = localPlayer.InventoryService ?? context.Inventory;
				}
			}

			private static T ResolveOrNull<T>(object resolver) where T : class
			{
				try
				{
					System.Reflection.MethodInfo method = resolver.GetType().GetMethod(
						VContainerApi.TryResolveMethod,
						new[]
						{
							typeof(Type),
							typeof(object).MakeByRefType(),
							typeof(object)
						}
					);

					if (method == null)
					{
						return null;
					}

					object[] parameters =
					{
						typeof(T),
						null,
						null
					};

					bool resolved = (bool)method.Invoke(resolver, parameters);
					return resolved ? parameters[1] as T : null;
				}
				catch
				{
					return null;
				}
			}
		}

		private readonly struct SurvivalValues
		{
			public readonly float Hunger;
			public readonly float Thirst;
			public readonly float Fatigue;
			public readonly float Cold;
			public readonly float Condition;

			public SurvivalValues(float hunger, float thirst, float fatigue, float cold, float condition)
			{
				Hunger = hunger;
				Thirst = thirst;
				Fatigue = fatigue;
				Cold = cold;
				Condition = condition;
			}
		}
	}
}
