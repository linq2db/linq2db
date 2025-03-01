using System;

namespace LinqToDB.Internal.Infrastructure
{
	public static class ServiceProviderExtensions
	{
		public static T? GetService<T>(this IServiceProvider provider)
			where T : class
		{
			return provider.GetService(typeof(T)) as T;
		}

		public static T GetRequiredService<T>(this IServiceProvider provider)
			where T : class
		{
			var service = provider.GetService<T>();
			if (service == null)
				throw new InvalidOperationException($"Service of type '{typeof(T)}' not found.");
			return service;
		}
	}
}
