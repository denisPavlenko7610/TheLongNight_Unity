using TLN.Application.Assets;
using TLN.Application.GameStates;
using TLN.Application.Input;
using TLN.Application.Notifications;
using TLN.Application.Saves;
using TLN.Application.Scenes;
using TLN.Application.Settings;
using TLN.Application.Time;
using TLN.Infrastructure.Assets;
using TLN.Infrastructure.Input;
using TLN.Infrastructure.Saves;
using TLN.Infrastructure.Scenes;
using TLN.Infrastructure.Settings;
using TLN.Infrastructure.Time;
using VContainer;
using VContainer.Unity;

namespace TLN.Bootstrap
{
	public sealed class ProjectLifetimeScope : LifetimeScope
	{
		protected override void Configure(IContainerBuilder builder)
		{
			builder.Register<IGameStateMachine, GameStateMachine>(Lifetime.Singleton);
			builder.Register<ISceneLoader>(
				container =>
					new SceneLoaderService(container.Resolve<IGameStateMachine>()),
				Lifetime.Singleton
			);
			builder.Register<ICursorService, CursorService>(Lifetime.Singleton);
			builder.Register<IInputModeService, InputModeService>(Lifetime.Singleton);
			builder.Register<IGameTimeScaleService, GameTimeScaleService>(Lifetime.Singleton);

			builder.Register<NotificationService>(Lifetime.Singleton).As<INotificationService>();
			builder.Register<AddressableAssetService>(Lifetime.Singleton).As<IAddressableAssetService>();
			builder.RegisterEntryPoint<GameStateInputModeController>();
			builder.RegisterEntryPoint<GameStatePauseController>();
			builder.RegisterEntryPoint<ProjectStartupEntryPoint>();

			builder.Register<SaveSessionService>(Lifetime.Singleton);
			builder.Register<JsonSaveRepository>(Lifetime.Singleton).As<ISaveRepository>();
			builder.Register<GameSettingsService>(Lifetime.Singleton).As<IGameSettingsService>();
		}
	}
}
