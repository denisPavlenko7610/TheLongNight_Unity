using Assign;
using TLN.Application.Notifications;
using TLN.Application.Saves;
using TLN.Core.Logging;
using TLN.Core.Validation;
using TLN.Gameplay.Building;
using TLN.Gameplay.Campfire;
using TLN.Gameplay.DayNight;
using TLN.Gameplay.Equipment;
using TLN.Gameplay.Inventory;
using TLN.Gameplay.Items;
using TLN.Gameplay.Placement;
using TLN.Gameplay.Player;
using TLN.Gameplay.Player.Networking;
using TLN.Gameplay.Saves;
using TLN.Gameplay.Sleep;
using TLN.Gameplay.Survival;
using TLN.Gameplay.Time;
using TLN.Gameplay.Time.Networking;
using TLN.Gameplay.Wildlife;
using TLN.Gameplay.World;
using TLN.Infrastructure.World;
using TLN.UI;
using UnityEngine;
using VContainer;
using VContainer.Unity;

public sealed class WorldLifetimeScope : LifetimeScope
{
	[Header("UI")]
	[SerializeField] [Required] private WorldUIRoot _uiRoot;
	[SerializeField] [Assign(Mode.Scene)] [Required] private WorldLocalPlayerUiBinder _worldLocalPlayerUiBinder;

	[Header("Configs")]
	[SerializeField] [Required] private InventoryConfig _inventoryConfig;
	[SerializeField] [Required] private GameTimeConfig _gameTimeConfig;
	[SerializeField] [Required] private DayNightConfig _dayNightConfig;
	[SerializeField] [Required] private SurvivalConfig _survivalConfig;
	[SerializeField] [Required] private SleepConfig _sleepConfig;
	[SerializeField] [Required] private BuildRecipeCatalog _buildRecipeCatalog;
	[SerializeField] [Required] private ItemCatalog _itemCatalog;
	[SerializeField] [Required] private WorldPrefabCatalog _worldPrefabCatalog;

	[Header("World Controllers")]
	[SerializeField] [Assign(Mode.Scene)] [Required] private WorldEntryPoint _worldEntryPoint;
	[SerializeField] [Assign(Mode.Scene)] [Required] private WorldTimeController _worldTimeController;
	[SerializeField] [Assign(Mode.Scene)] [Required] private DayNightController _dayNightController;
	[SerializeField] [Assign(Mode.Scene)] [Required] private WorldSurvivalController _worldSurvivalController;

	[Header("Multiplayer")]
	[SerializeField] [Assign(Mode.Scene)] [Required] private NetworkPlayerSpawner _networkPlayerSpawner;
	[SerializeField] [Assign(Mode.Scene)] [Required] private NetworkLocalPlayerBinder _networkLocalPlayerBinder;
	[SerializeField] [Assign(Mode.Scene)] [Required] private NetworkWorldTimeSynchronizer _networkWorldTimeSynchronizer;

	[Header("Spawning")]
	[SerializeField] [Assign(Mode.Scene)] private RandomWorldSpawner[] _randomWorldSpawners;

	[Header("Tuning")]
	[SerializeField] private float _survivalWarningCooldownSeconds = 30f;

	protected override void Configure(IContainerBuilder builder)
	{
		ResolveSceneReferences();

		RegisterConfigs(builder);
		RegisterGameplayServices(builder);
		RegisterWorldComponents(builder);
		RegisterRandomSpawners(builder);
		RegisterUI(builder);
	}

	private void RegisterConfigs(IContainerBuilder builder)
	{
		builder.RegisterInstance(_inventoryConfig);
		builder.RegisterInstance(_gameTimeConfig);
		builder.RegisterInstance(_dayNightConfig);
		builder.RegisterInstance(_survivalConfig);
		builder.RegisterInstance(_sleepConfig);
		builder.RegisterInstance(_buildRecipeCatalog);
		builder.RegisterInstance(_itemCatalog);
		builder.RegisterInstance(_worldPrefabCatalog);
	}

	private void RegisterGameplayServices(IContainerBuilder builder)
	{
		builder.Register<WorldSaveRegistry>(Lifetime.Scoped);
		builder.Register<GameSaveService>(Lifetime.Scoped).As<IGameSaveService>();

		builder.Register<GameTimeService>(Lifetime.Scoped).As<IGameTimeService>();
		builder.Register<DayNightService>(Lifetime.Scoped).As<IDayNightService>();
		builder.Register<SurvivalService>(Lifetime.Scoped).As<ISurvivalService>();
		builder.Register<SleepService>(Lifetime.Scoped);

		builder.Register<InventoryService>(Lifetime.Scoped).As<IInventoryService>();
		builder.Register<PlayerEquipmentService>(Lifetime.Scoped).As<IPlayerEquipmentService>();
		builder.Register<ItemUseService>(Lifetime.Scoped).As<IItemUseService>();

		builder.Register<IWorldObjectFactory, VContainerWorldObjectFactory>(Lifetime.Scoped);
		builder.Register<PlacementService>(Lifetime.Scoped);
		builder.Register<IPlayerFactory, PlayerFactory>(Lifetime.Scoped);
		builder.Register<IBuildService, BuildService>(Lifetime.Scoped);
		builder.Register<LocalPlayerService>(Lifetime.Scoped);

		builder.Register<WarmthService>(Lifetime.Scoped).As<IWarmthService>();
		builder.Register<WildlifeTargetService>(Lifetime.Scoped);

		builder.Register(
			container => new SurvivalWarningService(
				container.Resolve<ISurvivalService>(),
				container.Resolve<INotificationService>(),
				_survivalWarningCooldownSeconds
			),
			Lifetime.Scoped
		);
	}

	private void RegisterWorldComponents(IContainerBuilder builder)
	{
		builder.RegisterComponent(_worldEntryPoint);
		builder.RegisterComponent(_worldTimeController);
		builder.RegisterComponent(_dayNightController);
		builder.RegisterComponent(_worldSurvivalController);

		RegisterSceneComponentIfPresent(builder, _networkPlayerSpawner, nameof(_networkPlayerSpawner));
		RegisterSceneComponentIfPresent(builder, _networkLocalPlayerBinder, nameof(_networkLocalPlayerBinder));
		RegisterSceneComponentIfPresent(builder, _networkWorldTimeSynchronizer, nameof(_networkWorldTimeSynchronizer));
	}

	private void RegisterRandomSpawners(IContainerBuilder builder)
	{
		builder.RegisterInstance(new RandomWorldSpawnerSet(_randomWorldSpawners));

		if (_randomWorldSpawners == null)
		{
			return;
		}

		builder.RegisterBuildCallback(resolver =>
		{
			for (int i = 0; i < _randomWorldSpawners.Length; i++)
			{
				RandomWorldSpawner spawner = _randomWorldSpawners[i];

				if (spawner != null)
				{
					resolver.Inject(spawner);
				}
			}
		});
	}

	private void RegisterUI(IContainerBuilder builder)
	{
		builder.RegisterComponent(_uiRoot);

		builder.RegisterComponent(_uiRoot.HUD).AsImplementedInterfaces();
		builder.RegisterComponent(_uiRoot.SurvivalMenu).AsImplementedInterfaces();
		builder.RegisterComponent(_uiRoot.SleepWindow).AsImplementedInterfaces();
		builder.RegisterComponent(_uiRoot.CampfireWindow).AsImplementedInterfaces();

		builder.RegisterComponent(_uiRoot.PauseMenu);
		RegisterSceneComponentIfPresent(builder, _worldLocalPlayerUiBinder, nameof(_worldLocalPlayerUiBinder));
	}

	private void ResolveSceneReferences()
	{
		_worldLocalPlayerUiBinder ??= FindSceneComponent<WorldLocalPlayerUiBinder>();
		_networkPlayerSpawner ??= FindSceneComponent<NetworkPlayerSpawner>();
		_networkLocalPlayerBinder ??= FindSceneComponent<NetworkLocalPlayerBinder>();
		_networkWorldTimeSynchronizer ??= FindSceneComponent<NetworkWorldTimeSynchronizer>();
	}

	private static T FindSceneComponent<T>() where T : Component
	{
		return UnityEngine.Object.FindAnyObjectByType<T>(FindObjectsInactive.Include);
	}

	private static void RegisterSceneComponentIfPresent<T>(
		IContainerBuilder builder,
		T component,
		string componentName
	) where T : Component
	{
		if (component == null)
		{
			TLNLogger.LogWarning($"{componentName} is missing in WorldLifetimeScope.");
			return;
		}

		builder.RegisterComponent(component);
	}
}
