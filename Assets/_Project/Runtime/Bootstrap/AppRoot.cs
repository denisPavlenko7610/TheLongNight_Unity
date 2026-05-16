using System.Collections.Generic;
using TLN.Application.Services;
using TLN.Core.Lifetime;
using UnityEngine;

namespace TLN.Bootstrap.App
{
	public sealed class AppRoot : MonoBehaviour
	{
		public static AppRoot Instance { get; private set; }
		private readonly List<ITickable> _tickables = new List<ITickable>();

		public ServiceRegistry Services { get; private set; }

		private void Update()
		{
			float deltaTime = Time.deltaTime;

			for (int i = 0; i < _tickables.Count; i++) {
				_tickables[i].Tick(deltaTime);
			}
		}

		private void OnDestroy()
		{
			if (Services == null) {
				return;
			}

			if (Instance == this) {
				Instance = null;
			}

			IReadOnlyCollection<object> allServices = Services.GetAllServices();

			foreach (object service in allServices) {
				if (service is IDisposableService disposable) {
					disposable.Dispose();
				}
			}
		}

		public void Construct(ServiceRegistry services)
		{
			Instance = this;
			Services = services;

			IReadOnlyCollection<object> allServices = Services.GetAllServices();

			foreach (object service in allServices) {
				if (service is IInitializable initializable) {
					initializable.Initialize();
				}

				if (service is ITickable tickable) {
					_tickables.Add(tickable);
				}
			}
		}
	}
}
