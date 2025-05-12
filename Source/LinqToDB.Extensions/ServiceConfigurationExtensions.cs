using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

using LinqToDB.Data;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LinqToDB.Extensions.DependencyInjection
{
	public static class ServiceConfigurationExtensions
	{
		/// <summary>
		///     Registers <see cref="DataConnection"/> as the service <see cref="IDataContext"/> in the <see cref="IServiceCollection" />.
		///     You use this method when using dependency injection in your application.
		///     For more information on setting up dependency injection, see http://go.microsoft.com/fwlink/?LinkId=526890.
		/// </summary>
		/// <example>
		///     <code>
		///           public void ConfigureServices(IServiceCollection services)
		///           {
		///               var connectionString = "connection string to database";
		///
		///               services.AddLinqToDB(options => options.UseSqlServer(connectionString));
		///           }
		///       </code>
		/// </example>
		/// <param name="serviceCollection"> The <see cref="IServiceCollection" /> to add services to. </param>
		/// <param name="configure">
		///     <para>
		///         An action to configure the <see cref="DataOptions" /> for the context.
		///     </para>
		/// </param>
		/// <param name="lifetime"> The lifetime with which to register the Context service in the container.
		/// For one connection per request use <see cref="ServiceLifetime.Scoped"/> (the default).
		/// </param>
		/// <remarks>
		/// 	<para>
		/// 		This will only work when you have 1 database connection across your whole application.
		/// 		If your application needs multiple different connections with different configurations
		/// 		then use <see cref="AddLinqToDBContext{TContext}"/> or <see cref="AddLinqToDBContext{TContext, TContextImplementation}"/>.
		/// 	</para>
		/// 	<para>
		/// 		To Resolve the connection inject <see cref="IDataContext"/> into your services.
		/// 	</para>
		/// </remarks>
		/// <returns>
		///     The same service collection so that multiple calls can be chained.
		/// </returns>
		public static IServiceCollection AddLinqToDB(
			this IServiceCollection                        serviceCollection,
			Func<IServiceProvider,DataOptions,DataOptions> configure,
			ServiceLifetime                                lifetime = ServiceLifetime.Scoped)
		{
			return AddLinqToDBContext<IDataContext,DataConnection>(serviceCollection, configure, lifetime);
		}

		/// <summary>
		///     Registers <typeparamref name="TContext"/> as a service in the <see cref="IServiceCollection" />.
		///     You use this method when using dependency injection in your application, such as with ASP.NET.
		///     For more information on setting up dependency injection, see http://go.microsoft.com/fwlink/?LinkId=526890.
		/// </summary>
		/// <example>
		///     <code>
		///           public void ConfigureServices(IServiceCollection services)
		///           {
		///               var connectionString = "connection string to database";
		///
		///               services.AddLinqToDBContext&lt;MyContext&gt;(options => options.UseSqlServer(connectionString));
		///           }
		///       </code>
		/// </example>
		/// <typeparam name="TContext">
		/// 	The type of context to be registered. Must inherit from <see cref="IDataContext"/>
		/// 	and expose a constructor that takes <see cref="DataContextOptions" /> (where T is <typeparamref name="TContext"/>)
		/// 	and passes it to the base constructor of <see cref="DataConnection" />.
		/// </typeparam>
		/// <param name="serviceCollection"> The <see cref="IServiceCollection" /> to add services to. </param>
		/// <param name="configure">
		///     <para>
		///         An action to configure the <see cref="DataOptions" /> for the context.
		///     </para>
		///     <para>
		///         In order for the options to be passed into your context, you need to expose a constructor on your context that takes
		///         <see cref="DataContextOptions" /> and passes it to the base constructor of <see cref="DataConnection" />.
		///     </para>
		/// </param>
		/// <param name="lifetime">
		/// 	The lifetime with which to register the Context service in the container.
		/// 	For one connection per request use <see cref="ServiceLifetime.Scoped"/> (the default).
		/// </param>
		/// <remarks>
		/// 	This method should be used when a custom context is required or
		/// 	when multiple contexts with different configurations are required.
		/// </remarks>
		/// <returns>
		///     The same service collection so that multiple calls can be chained.
		/// </returns>
		public static IServiceCollection AddLinqToDBContext<
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TContext>(
			this IServiceCollection                        serviceCollection,
			Func<IServiceProvider,DataOptions,DataOptions> configure,
			ServiceLifetime                                lifetime = ServiceLifetime.Scoped)
			where TContext : IDataContext
		{
			return AddLinqToDBContext<TContext,TContext>(serviceCollection, configure, lifetime);
		}

		/// <summary>
		///     Registers <typeparamref name="TContext"/> as a service in the <see cref="IServiceCollection" />.
		///     You use this method when using dependency injection in your application, such as with ASP.NET.
		///     For more information on setting up dependency injection, see http://go.microsoft.com/fwlink/?LinkId=526890.
		/// </summary>
		/// <example>
		///     <code>
		///           public void ConfigureServices(IServiceCollection services)
		///           {
		///               var connectionString = "connection string to database";
		///
		///               services.AddLinqToDBContext&lt;IMyContext, MyContext&gt;(options => options.UseSqlServer(connectionString));
		///           }
		///       </code>
		/// </example>
		/// <typeparam name="TContext">
		/// 	The class or interface that will be used to resolve the context from the container.
		/// </typeparam>
		/// <typeparam name="TContextImplementation">
		///		The concrete implementation type used to fulfill requests for <typeparamref name="TContext"/> from the container.
		/// 	Must inherit from <see cref="IDataContext"/> and <typeparamref name="TContext"/>
		/// 	and expose a constructor that takes <see cref="DataContextOptions" /> (where T is <typeparamref name="TContextImplementation"/>)
		/// 	and passes it to the base constructor of <see cref="DataConnection" />.
		/// </typeparam>
		/// <param name="serviceCollection"> The <see cref="IServiceCollection" /> to add services to. </param>
		/// <param name="configure">
		///     <para>
		///         An action to configure the <see cref="DataOptions" /> for the context.
		///     </para>
		///     <para>
		///         In order for the options to be passed into your context, you need to expose a constructor on your context that takes
		///         <see cref="DataContextOptions" /> and passes it to the base constructor of <see cref="DataConnection" />.
		///     </para>
		/// </param>
		/// <param name="lifetime">
		/// 	The lifetime with which to register the Context service in the container.
		/// 	For one connection per request use <see cref="ServiceLifetime.Scoped"/> (the default).
		/// </param>
		/// <remarks>
		/// 	This method should be used when a custom context is required or
		/// 	when multiple contexts with different configurations are required.
		/// </remarks>
		/// <returns>
		///     The same service collection so that multiple calls can be chained.
		/// </returns>
		public static IServiceCollection AddLinqToDBContext<TContext,
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TContextImplementation>(
			this IServiceCollection serviceCollection,
			Func<IServiceProvider,DataOptions,DataOptions> configure,
			ServiceLifetime                                lifetime  = ServiceLifetime.Scoped)
			where TContextImplementation : TContext, IDataContext
			where TContext: IDataContext
		{
			var constructorType = HasTypedContextConstructor<TContextImplementation, TContext>();

			serviceCollection.TryAdd(new ServiceDescriptor(typeof(TContext), typeof(TContextImplementation), lifetime));

			switch (constructorType)
			{
				case OptionsParameterType.DataOptionsTImpl:
					serviceCollection.TryAdd(new ServiceDescriptor(typeof(DataOptions<TContextImplementation>),
						provider => new DataOptions<TContextImplementation>(configure(provider, new DataOptions())),
						lifetime));
					break;
				case OptionsParameterType.DataOptionsTContext:
					serviceCollection.TryAdd(new ServiceDescriptor(typeof(DataOptions<TContext>),
						provider => new DataOptions<TContext>(configure(provider, new DataOptions())),
						lifetime));
					break;
				case OptionsParameterType.DataOptions:
					serviceCollection.TryAdd(new ServiceDescriptor(typeof(DataOptions),
						provider => configure(provider, new DataOptions()), lifetime));
					break;

			}

			return serviceCollection;
		}

		enum OptionsParameterType
		{
			DataOptionsTImpl,
			DataOptionsTContext,
			DataOptions,
		}

		static OptionsParameterType HasTypedContextConstructor<
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TContextImplementation, TContext>()
			where TContextImplementation : IDataContext
			where TContext : IDataContext
		{
			var constructors = typeof(TContextImplementation)
				.GetConstructors(BindingFlags.Public | BindingFlags.Instance);

			if (constructors.Any(c => c.GetParameters().Any(p => p.ParameterType == typeof(DataOptions<TContextImplementation>))))
				return OptionsParameterType.DataOptionsTImpl;

			if (constructors.Any(c => c.GetParameters().Any(p => p.ParameterType == typeof(DataOptions<TContext>))))
				return OptionsParameterType.DataOptionsTContext;

			if (constructors.Any(c => c.GetParameters().Any(p => p.ParameterType == typeof(DataOptions))))
				return OptionsParameterType.DataOptions;

			throw new ArgumentException($"Missing constructor accepting '{nameof(DataOptions)}' on type {typeof(TContextImplementation).Name}.");
		}
	}
}
