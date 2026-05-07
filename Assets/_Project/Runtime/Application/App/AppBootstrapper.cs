using TLN.Application.GameStates;
using TLN.Application.Scenes;
using TLN.Application.Services;
using TLN.Core.GameStates;
using UnityEngine;

namespace TLN.Application.App
{
	public sealed class AppBootstrapper : MonoBehaviour
	{
		[SerializeField] private AppRoot _appRoot;

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

			ISceneLoader sceneLoader = services.Resolve<ISceneLoader>();
			sceneLoader.LoadMainMenu();
		}

		private ServiceRegistry CreateServices()
		{
			ServiceRegistry services = new();

			GameStateMachine gameStateMachine = new();
			SceneLoaderService sceneLoaderService = new(gameStateMachine, _appRoot);
			GameStateDebugLogger gameStateDebugLogger = new(gameStateMachine);

			services.Register<IGameStateMachine>(gameStateMachine);
			services.Register<ISceneLoader>(sceneLoaderService);
			services.Register<GameStateDebugLogger>(gameStateDebugLogger);

			return services;
		}
	}
}
