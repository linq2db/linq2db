using System;
using System.Threading.Tasks;

using JetBrains.Annotations;

using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;

namespace LinqToDB.Remote.SignalR
{
	[PublicAPI]
	public static class ServiceConfigurationExtensions
	{
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
		///               services.AddLinqToDBSignalRDataContext&lt;IMyContext, MyContext&gt;(
		///                   builder.HostEnvironment.BaseAddress,
		///                   "api/linq2db",
		///                   (service,options) => options.UseSqlServer(connectionString));
		///           }
		///       </code>
		/// </example>
		/// <typeparam name="TContext">
		/// 	The class or interface that will be used to resolve the context from the container.
		/// </typeparam>
		/// <param name="services"> The <see cref="IServiceCollection" /> to add services to. </param>
		/// <param name="baseAddress">LinqToDB API controller base address.</param>
		/// <param name="serviceName">LinqToDB API controller address.</param>
		/// <param name="getContext"></param>
		/// <remarks>
		/// 	This method should be used when a custom context is required or
		/// 	when multiple contexts with different configurations are required.
		/// </remarks>
		/// <returns>
		///     The same service collection so that multiple calls can be chained.
		/// </returns>
		public static IServiceCollection AddLinqToDBSignalRDataContext<TContext>(
			this IServiceCollection                 services,
			string                                  baseAddress,
			string                                  serviceName,
			Func<SignalRLinqServiceClient,TContext> getContext)
			where TContext: class, IDataContext
		{
			services.AddSingleton(provider =>
				new Container<HubConnection>(new HubConnectionBuilder()
					.WithUrl(new Uri(new Uri(baseAddress), serviceName))
					.WithAutomaticReconnect()
					.Build()));

			services.AddScoped(provider =>
			{
				var client = provider.GetRequiredService<Container<HubConnection>>();
				return new SignalRLinqServiceClient(client.Object);
			});

			services.AddTransient(provider =>
			{
				var client = provider.GetRequiredService<SignalRLinqServiceClient>();
				return getContext(client);
			});

			services.AddTransient<IDataContextFactory<TContext>>(provider =>
			{
				return new DataContextFactory<TContext>(_ =>
				{
					var client = provider.GetRequiredService<SignalRLinqServiceClient>();
					return getContext(client);
				});
			});

			return services;
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
		///               services.AddLinqToDBSignalRDataContext&lt;IMyContext, MyContext&gt;(
		///                   builder.HostEnvironment.BaseAddress,
		///                   (service,options) => options.UseSqlServer(connectionString));
		///           }
		///       </code>
		/// </example>
		/// <typeparam name="TContext">
		/// 	The class or interface that will be used to resolve the context from the container.
		/// </typeparam>
		/// <param name="serviceCollection"> The <see cref="IServiceCollection" /> to add services to. </param>
		/// <param name="baseAddress">LinqToDB API controller base address.</param>
		/// <param name="getContext"></param>
		/// <remarks>
		/// 	This method should be used when a custom context is required or
		/// 	when multiple contexts with different configurations are required.
		/// </remarks>
		/// <returns>
		///     The same service collection so that multiple calls can be chained.
		/// </returns>
		public static IServiceCollection AddLinqToDBSignalRDataContext<TContext>(
			this IServiceCollection                 serviceCollection,
			string                                  baseAddress,
			Func<SignalRLinqServiceClient,TContext> getContext)
			where TContext: class, IDataContext
		{
			return serviceCollection.AddLinqToDBSignalRDataContext(baseAddress, "/hub/linq2db", getContext);
		}

		/// <summary>
		/// / Initializes SignalR connection for <see cref="IDataContext"/> context.
		/// </summary>
		/// <param name="dataContext">The <see cref="IDataContext"/> to initialize.</param>
		/// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
		public static async Task InitSignalRAsync(this IDataContext dataContext)
		{
			if (dataContext is SignalRDataContext signalRDataContext)
			{
				await signalRDataContext.ConfigureAsync(default).ConfigureAwait(false);
			}
		}

		/// <summary>
		/// Initializes SignalR connection for <typeparamref name="T"/> context.
		/// </summary>
		/// <typeparam name="T">IDataContext type.</typeparam>
		/// <param name="serviceProvider">The <see cref="IServiceProvider"/> to get services from.</param>
		/// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
		public static async Task InitSignalRAsync<T>(this IServiceProvider serviceProvider)
			where T : IDataContext
		{
			await serviceProvider.GetRequiredService<Container<HubConnection>>().Object.StartAsync().ConfigureAwait(false);
			await serviceProvider.GetRequiredService<T>().InitSignalRAsync().ConfigureAwait(false);
		}
	}
}
