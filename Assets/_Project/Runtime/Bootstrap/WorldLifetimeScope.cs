using System;
using TLN.Application.Notifications;
using TLN.Bootstrap;
using TLN.Gameplay.Building;
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
	[SerializeField] private WorldUIRoot _uiRoot;
	[SerializeField] private InventoryConfig _inventoryConfig;
	[SerializeField] private GameTimeConfig _gameTimeConfig;
	[SerializeField] private SurvivalConfig _survivalConfig;
	[SerializeField] private SleepConfig _sleepConfig;
	[SerializeField] private BuildRecipeCatalog _buildRecipeCatalog;

	[SerializeField] private float _survivalWarningCooldownSeconds = 30f;

	protected override void Configure(IContainerBuilder builder)
	{
		ValidateReferences();

		builder.RegisterInstance(_inventoryConfig);
		builder.RegisterInstance(_gameTimeConfig);
		builder.RegisterInstance(_survivalConfig);
		builder.RegisterInstance(_sleepConfig);

		builder.Register<GameTimeService>(Lifetime.Scoped).AsSelf().As<IGameTimeService>();
		builder.Register<SurvivalService>(Lifetime.Scoped).AsSelf().As<ISurvivalService>();
		builder.Register<SleepService>(Lifetime.Scoped);

		builder.Register<InventoryService>(Lifetime.Scoped).AsSelf().As<IInventoryService>();
		builder.Register<ItemUseService>(Lifetime.Scoped)
			.AsSelf().As<IItemUseService>();
		builder.Register<IWorldObjectFactory, VContainerWorldObjectFactory>(Lifetime.Scoped);
		builder.Register<PlacementService>(Lifetime.Scoped);
		builder.Register(container => new SurvivalWarningService(
			container.Resolve<ISurvivalService>(), container.Resolve<INotificationService>(),
			_survivalWarningCooldownSeconds), Lifetime.Scoped);

		builder.Register<IPlayerFactory, PlayerFactory>(Lifetime.Scoped);
		builder.Register<IBuildService, BuildService>(Lifetime.Scoped);

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

	private void ValidateReferences()
	{
		if (_uiRoot == null)
		{
			throw new InvalidOperationException("WorldUIRoot is not assigned in WorldLifetimeScope.");
		}

		if (_inventoryConfig == null)
		{
			throw new InvalidOperationException("InventoryConfig is not assigned in WorldLifetimeScope.");
		}

		if (_buildRecipeCatalog == null)
		{
			throw new InvalidOperationException("BuildRecipeCatalog is not assigned in WorldLifetimeScope.");
		}

		if (_gameTimeConfig == null)
		{
			throw new InvalidOperationException("GameTimeConfig is not assigned in WorldLifetimeScope.");
		}

		if (_survivalConfig == null)
		{
			throw new InvalidOperationException("SurvivalConfig is not assigned in WorldLifetimeScope.");
		}

		if (_sleepConfig == null)
		{
			throw new InvalidOperationException("SleepConfig is not assigned in WorldLifetimeScope.");
		}
	}

	private void InjectSceneInjectables(IObjectResolver resolver)
	{
		SceneInjectable[] injectables = FindObjectsByType<SceneInjectable>(
			FindObjectsInactive.Include,
			FindObjectsSortMode.None);

		for (int i = 0; i < injectables.Length; i++)
		{
			SceneInjectable injectable = injectables[i];

			if (injectable == null)
			{
				continue;
			}

			if (IsCoveredByParentInjectable(injectable))
			{
				continue;
			}

			GameObject target = injectable.gameObject;

			if (!target.scene.IsValid() || !target.scene.isLoaded)
			{
				continue;
			}

			if (injectable.InjectChildren)
			{
				resolver.InjectGameObject(target);
				continue;
			}

			InjectSingleGameObject(resolver, target);
		}
	}

	private static bool IsCoveredByParentInjectable(SceneInjectable injectable)
	{
		Transform parent = injectable.transform.parent;

		while (parent != null)
		{
			if (parent.TryGetComponent(out SceneInjectable parentInjectable) && parentInjectable.InjectChildren)
			{
				return true;
			}

			parent = parent.parent;
		}

		return false;
	}

	private static void InjectSingleGameObject(IObjectResolver resolver, GameObject target)
	{
		MonoBehaviour[] components = target.GetComponents<MonoBehaviour>();

		for (int i = 0; i < components.Length; i++)
		{
			MonoBehaviour component = components[i];

			if (component == null)
			{
				continue;
			}

			resolver.Inject(component);
		}
	}
}
