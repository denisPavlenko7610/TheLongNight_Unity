using System;
using System.Collections.Generic;

namespace TLN.Application.Services
{
	public sealed class ServiceRegistry
	{
		private readonly Dictionary<Type, object> _services = new();

		public void Register<TService>(TService service) where TService : class
		{
			Type type = typeof(TService);

			if (!_services.TryAdd(type, service))
			{
				throw new InvalidOperationException($"Service already registered: {type.Name}");
			}
		}

		public TService Resolve<TService>() where TService : class
		{
			Type type = typeof(TService);

			if (_services.TryGetValue(type, out object service))
			{
				return (TService)service;
			}

			throw new InvalidOperationException($"Service not registered: {type.Name}");
		}

		public bool TryResolve<TService>(out TService service) where TService : class
		{
			Type type = typeof(TService);

			if (_services.TryGetValue(type, out object rawService))
			{
				service = (TService)rawService;
				return true;
			}

			service = null;
			return false;
		}

		public IReadOnlyCollection<object> GetAllServices()
		{
			return _services.Values;
		}
	}
}
