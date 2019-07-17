using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LinqToDB.AspNet
{
	using Configuration;
	using Data;
	using Extensions;

	public static class ServiceConfigurationExtensions
	{
		public static IServiceCollection AddLinqToDb(
			this IServiceCollection serviceCollection,
			Action<IServiceProvider, LinqToDbConnectionOptions> configure,
			ServiceLifetime lifetime = ServiceLifetime.Scoped)
		{
			return AddLinqToDbContext<DataConnection>(serviceCollection, configure, lifetime);
		}
		
		public static IServiceCollection AddLinqToDbContext<TContext>(
			this IServiceCollection serviceCollection,
			Action<IServiceProvider, LinqToDbConnectionOptions> configure,
			ServiceLifetime lifetime = ServiceLifetime.Scoped) where TContext : IDataContext
		{
			return AddLinqToDbContext<TContext, TContext>(serviceCollection, configure, lifetime);
		}

		public static IServiceCollection AddLinqToDbContext<TContext, TContextImplementation>(
			this IServiceCollection serviceCollection,
			Action<IServiceProvider, LinqToDbConnectionOptions> configure,
			ServiceLifetime lifetime = ServiceLifetime.Scoped) where TContextImplementation : IDataContext
		{
			CheckContextConstructor<TContextImplementation>();
			serviceCollection.TryAdd(new ServiceDescriptor(typeof(TContext), typeof(TContextImplementation), lifetime));
			serviceCollection.TryAdd(new ServiceDescriptor(typeof(LinqToDbConnectionOptions<TContextImplementation>),
				provider =>
				{
					var options = new LinqToDbConnectionOptions<TContextImplementation>();
					configure(provider, options);
					return options;
				},
				lifetime));
			serviceCollection.TryAdd(new ServiceDescriptor(typeof(LinqToDbConnectionOptions),
				provider => provider.GetService(typeof(LinqToDbConnectionOptions<TContextImplementation>)), lifetime));
			return serviceCollection;
		}

		private static void CheckContextConstructor<TContext>()
		{
			var constructorInfo = 
				typeof(TContext).GetConstructorEx(new[] {typeof(LinqToDbConnectionOptions<TContext>)}) ??
				typeof(TContext).GetConstructorEx(new[] {typeof(LinqToDbConnectionOptions)});
			if (constructorInfo == null)
			{
				throw new ArgumentException("Missing constructor accepting 'LinqToDbContextOptions' on type "
				                            + typeof(TContext).Name);
			}
		}
	}
}
