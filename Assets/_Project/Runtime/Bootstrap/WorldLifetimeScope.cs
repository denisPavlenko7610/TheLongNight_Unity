using TLN.Application.Notifications;
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
	[SerializeField] private float _survivalWarningCooldownSeconds = 30f;

	protected override void Configure(IContainerBuilder builder)
	{
		ResolveSceneReferences();

		builder.RegisterInstance(_inventoryConfig);
		builder.RegisterInstance(_gameTimeConfig);
		builder.RegisterInstance(_survivalConfig);
		builder.RegisterInstance(_sleepConfig);

		builder.Register<GameTimeService>(Lifetime.Scoped)
			.AsSelf()
			.As<IGameTimeService>();
		builder.Register<SurvivalService>(Lifetime.Scoped)
			.AsSelf()
			.As<ISurvivalService>();
		builder.Register<SleepService>(Lifetime.Scoped);

		builder.Register<InventoryService>(Lifetime.Scoped)
			.AsSelf()
			.As<IInventoryService>();
		builder.Register<ItemUseService>(Lifetime.Scoped)
			.AsSelf()
			.As<IItemUseService>();
		builder.Register<PlacementService>(Lifetime.Scoped);
		builder.Register(container => new SurvivalWarningService(
			container.Resolve<ISurvivalService>(),
			container.Resolve<INotificationService>(),
			_survivalWarningCooldownSeconds), Lifetime.Scoped);

		builder.Register<IPlayerFactory, PlayerFactory>(Lifetime.Scoped);

		builder.RegisterComponent(_uiRoot);
		builder.RegisterComponent(_uiRoot.HUD).AsImplementedInterfaces();
		builder.RegisterComponent(_uiRoot.InventoryWindow).AsImplementedInterfaces();
		builder.RegisterComponent(_uiRoot.SleepWindow).AsImplementedInterfaces();
		builder.RegisterComponent(_uiRoot.PauseMenu).AsImplementedInterfaces();
		builder.RegisterComponentInHierarchy<WorldEntryPoint>();
		builder.RegisterComponentInHierarchy<WorldTimeController>();
		builder.RegisterComponentInHierarchy<WorldSurvivalController>();
	}

	private void ResolveSceneReferences()
	{
		if (_uiRoot == null)
		{
			_uiRoot = FindFirstObjectByType<WorldUIRoot>();
		}
	}
}