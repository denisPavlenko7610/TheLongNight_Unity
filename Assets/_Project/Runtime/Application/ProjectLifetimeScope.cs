using TLN.Application.App;
using TLN.Application.Assets;
using TLN.Application.GameStates;
using TLN.Application.Input;
using TLN.Application.Localization;
using TLN.Application.Notifications;
using TLN.Application.Scenes;
using TLN.Application.Time;
using VContainer;
using VContainer.Unity;

public sealed class ProjectLifetimeScope : LifetimeScope
{
	protected override void Configure(IContainerBuilder builder)
	{
		builder.Register<IGameStateMachine, GameStateMachine>(Lifetime.Singleton);
		builder.Register<ISceneLoader>(
			container => new SceneLoaderService(container.Resolve<IGameStateMachine>(), this),
			Lifetime.Singleton);
		builder.Register<ICursorService, CursorService>(Lifetime.Singleton);
		builder.Register<IInputModeService, InputModeService>(Lifetime.Singleton);
		builder.Register<IGameTimeScaleService, GameTimeScaleService>(Lifetime.Singleton);

		builder.Register<NotificationService>(Lifetime.Singleton).As<INotificationService>();
		builder.Register<AddressableAssetService>(Lifetime.Singleton).As<IAddressableAssetService>();
		builder.Register<UnityLocalizationService>(Lifetime.Singleton).As<ILocalizationService>();
		builder.Register<BootStartupService>(Lifetime.Singleton);

		builder.RegisterEntryPoint<GameStateInputModeController>();
		builder.RegisterEntryPoint<GameStateTimeScaleController>();
		builder.RegisterEntryPoint<ProjectStartupEntryPoint>();
	}
}
