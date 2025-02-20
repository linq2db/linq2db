using System;
using System.Collections.Generic;

namespace LinqToDB.Internal.DataProvider
{
	public class SimpleServiceProvider : IServiceProvider
	{
		readonly Dictionary<Type, object> _services = new();

		public void AddService<T>(T service) where T : class
		{
			_services[typeof(T)] = service;
		}

		public object? GetService(Type serviceType)
		{
			return _services.TryGetValue(serviceType, out var service) ? service : null;
		}
	}
}
