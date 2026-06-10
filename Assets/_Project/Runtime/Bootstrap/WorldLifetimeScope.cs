using TLN.Application.Notifications;
using TLN.Bootstrap;
using TLN.Core.Validation;
using TLN.Gameplay.Building;
using TLN.Gameplay.Campfire;
using TLN.Gameplay.Equipment;
using TLN.Gameplay.Inventory;
using TLN.Gameplay.Items;
using TLN.Gameplay.Placement;
using TLN.Gameplay.Player;
using TLN.Gameplay.Sleep;
using TLN.Gameplay.Survival;
using TLN.Gameplay.Time;
using TLN.Gameplay.World;
using TLN.UI.World;
using UnityEngine;
using VContainer;
using VContainer.Unity;

public sealed class WorldLifetimeScope : LifetimeScope
{
	[SerializeField, Required] private WorldUIRoot _uiRoot;
	[SerializeField, Required] private InventoryConfig _inventoryConfig;
	[SerializeField, Required] private GameTimeConfig _gameTimeConfig;
	[SerializeField, Required] private SurvivalConfig _survivalConfig;
	[SerializeField, Required] private SleepConfig _sleepConfig;
	[SerializeField, Required] private BuildRecipeCatalog _buildRecipeCatalog;

	[SerializeField] private float _survivalWarningCooldownSeconds = 30f;

	protected override void Configure(IContainerBuilder builder)
	{

		builder.RegisterInstance(_inventoryConfig);
		builder.RegisterInstance(_gameTimeConfig);
		builder.RegisterInstance(_survivalConfig);
		builder.RegisterInstance(_sleepConfig);

		builder.Register<GameTimeService>(Lifetime.Scoped).As<IGameTimeService>();
		builder.Register<SurvivalService>(Lifetime.Scoped).As<ISurvivalService>();
		builder.Register<SleepService>(Lifetime.Scoped);

		builder.Register<InventoryService>(Lifetime.Scoped).As<IInventoryService>();
		builder.Register<PlayerEquipmentService>(Lifetime.Scoped).As<IPlayerEquipmentService>();
		builder.Register<ItemUseService>(Lifetime.Scoped).As<IItemUseService>();
		builder.Register<IWorldObjectFactory, VContainerWorldObjectFactory>(Lifetime.Scoped);
		builder.Register<PlacementService>(Lifetime.Scoped);
		builder.Register(container => new SurvivalWarningService(
			container.Resolve<ISurvivalService>(), container.Resolve<INotificationService>(),
			_survivalWarningCooldownSeconds), Lifetime.Scoped);

		builder.Register<IPlayerFactory, PlayerFactory>(Lifetime.Scoped);
		builder.Register<IBuildService, BuildService>(Lifetime.Scoped);
		builder.Register<WarmthService>(Lifetime.Scoped).As<IWarmthService>();

		builder.RegisterComponentInHierarchy<WorldEntryPoint>();
		builder.RegisterComponentInHierarchy<WorldTimeController>();
		builder.RegisterComponentInHierarchy<WorldSurvivalController>();

		builder.RegisterInstance(_buildRecipeCatalog);
		builder.RegisterComponent(_uiRoot);
		builder.RegisterComponent(_uiRoot.HUD).AsImplementedInterfaces();
		builder.RegisterComponent(_uiRoot.InventoryWindow).AsImplementedInterfaces();
		builder.RegisterComponent(_uiRoot.SleepWindow).AsImplementedInterfaces();
		builder.RegisterComponent(_uiRoot.CampfireWindow).AsImplementedInterfaces();
		builder.RegisterComponent(_uiRoot.PauseMenu).AsImplementedInterfaces();
		builder.RegisterComponent(_uiRoot.BuildWindow).AsImplementedInterfaces();

		builder.RegisterBuildCallback(InjectSceneInjectables);
	}

	private void InjectSceneInjectables(IObjectResolver resolver)
	{
		SceneInjectable[] injectables = FindObjectsByType<SceneInjectable>(
			FindObjectsInactive.Include,
			FindObjectsSortMode.None);

		foreach (SceneInjectable injectable in injectables)
		{
			if (injectable == null)
			{
				continue;
			}

			if (injectable.HasParentInjectable)
			{
				continue;
			}

			GameObject target = injectable.gameObject;

			if (!target.scene.IsValid() || !target.scene.isLoaded)
			{
				continue;
			}

			injectable.InjectHierarchy(resolver);
		}
	}

}
