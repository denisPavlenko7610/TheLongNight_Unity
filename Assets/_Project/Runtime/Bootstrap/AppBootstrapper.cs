using TLN.Application.App;
using TLN.Application.GameStates;
using TLN.Application.Input;
using TLN.Application.Notifications;
using TLN.Application.Scenes;
using TLN.Application.Services;
using TLN.Application.Time;
using TLN.Core.GameStates;
using UnityEngine;

namespace TLN.Bootstrap.App
{
	public sealed class AppBootstrapper : MonoBehaviour
	{
		[SerializeField] private AppRoot _appRoot;
		[SerializeField] private MonoBehaviour[] _serviceInstallerBehaviours;

		private static bool _isBootstrapped;

		private void Awake()
		{
			if (_isBootstrapped)
			{
				Destroy(gameObject);
				return;
			}

			_isBootstrapped = true;
			DontDestroyOnLoad(gameObject);

			if (_appRoot == null)
			{
				_appRoot = GetComponent<AppRoot>();
			}

			ServiceRegistry services = CreateServices();

			_appRoot.Construct(services);

			IGameStateMachine gameStateMachine = services.Resolve<IGameStateMachine>();
			gameStateMachine.Enter(GameStateId.Boot);

			BootStartupService bootStartupService = services.Resolve<BootStartupService>();
			bootStartupService.Start();
		}

		private void InstallExternalServices(ServiceRegistry services)
		{
			for (int i = 0; i < _serviceInstallerBehaviours.Length; i++)
			{
				if (_serviceInstallerBehaviours[i] is IServiceInstaller installer)
				{
					installer.Install(services);
				}
			}
		}

		private ServiceRegistry CreateServices()
		{
			ServiceRegistry services = new();

			GameStateMachine gameStateMachine = new();
			SceneLoaderService sceneLoaderService = new(gameStateMachine, _appRoot);
			BootStartupService bootStartupService = new(sceneLoaderService);
			CursorService cursorService = new();
			InputModeService inputModeService = new(cursorService);
			GameTimeScaleService gameTimeScaleService = new();
			GameStateTimeScaleController gameStateTimeScaleController = new(gameStateMachine, gameTimeScaleService);
			GameStateInputModeController gameStateInputModeController = new(gameStateMachine, inputModeService);
			NotificationService notificationService = new();

			services.Register<IGameStateMachine>(gameStateMachine);
			services.Register<ICursorService>(cursorService);
			services.Register<IInputModeService>(inputModeService);
			services.Register<IGameTimeScaleService>(gameTimeScaleService);
			services.Register<GameStateTimeScaleController>(gameStateTimeScaleController);
			services.Register<GameStateInputModeController>(gameStateInputModeController);
			services.Register<ISceneLoader>(sceneLoaderService);
			services.Register<BootStartupService>(bootStartupService);
			services.Register<INotificationService>(notificationService);
			services.Register<NotificationService>(notificationService);

			InstallExternalServices(services);
			return services;
		}
	}
}
