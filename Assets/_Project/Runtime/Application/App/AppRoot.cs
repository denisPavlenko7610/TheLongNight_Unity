using System.Collections.Generic;
using TLN.Application.Services;
using TLN.Core.Lifetime;
using UnityEngine;

namespace TLN.Application.App
{
	public sealed class AppRoot : MonoBehaviour
	{
		private readonly List<ITickable> _tickables = new();
		private ServiceRegistry _services;

		public static AppRoot Instance { get; private set; }

		public ServiceRegistry Services => _services;

		public void Construct(ServiceRegistry services)
		{
			Instance = this;
			_services = services;

			IReadOnlyCollection<object> allServices = _services.GetAllServices();

			foreach (object service in allServices)
			{
				if (service is IInitializable initializable)
				{
					initializable.Initialize();
				}

				if (service is ITickable tickable)
				{
					_tickables.Add(tickable);
				}
			}
		}

		private void Update()
		{
			float deltaTime = UnityEngine.Time.deltaTime;

			for (int i = 0; i < _tickables.Count; i++)
			{
				_tickables[i].Tick(deltaTime);
			}
		}

		private void OnDestroy()
		{
			if (_services == null)
			{
				return;
			}

			if (Instance == this)
			{
				Instance = null;
			}

			IReadOnlyCollection<object> allServices = _services.GetAllServices();

			foreach (object service in allServices)
			{
				if (service is IDisposableService disposable)
				{
					disposable.Dispose();
				}
			}
		}
	}
}
