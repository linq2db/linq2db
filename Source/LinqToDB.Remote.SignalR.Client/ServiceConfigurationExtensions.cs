using System;
using System.Threading.Tasks;

using JetBrains.Annotations;

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
		///           services.AddLinqToDBSignalRDataContext&lt;IMyContext&gt;(
		///               builder.HostEnvironment.BaseAddress,
		///               "/hub/linq2db",
		///               client => new MyContext(client));
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
			return services.AddLinqToDBSignalRDataContext(new Uri(new Uri(baseAddress), serviceName), getContext);
		}

		/// <summary>
		///     Registers <typeparamref name="TContext"/> as a service in the <see cref="IServiceCollection" />, together
		///     with a singleton <see cref="LinqToDBSignalRConnection"/> to the hub at <paramref name="hubUrl"/> shared
		///     by all <typeparamref name="TContext"/> contexts. The connection starts with the first query, and a query
		///     after the connection was lost starts it again.
		/// </summary>
		/// <remarks>
		///     Each context type registered this way has a connection of its own, so contexts of two types can use two
		///     hubs. <see cref="LinqToDBSignalRConnection"/> and <see cref="SignalRLinqServiceClient"/> resolved from the
		///     container directly are those of the last registration.
		/// </remarks>
		/// <example>
		///     <code>
		///           services.AddLinqToDBSignalRDataContext&lt;IMyContext&gt;(
		///               new Uri(new Uri(builder.HostEnvironment.BaseAddress), "/hub/linq2db"),
		///               client => new MyContext(client),
		///               options => options.ConfigureHttpConnection = http => http.AccessTokenProvider = GetTokenAsync);
		///       </code>
		/// </example>
		/// <typeparam name="TContext">
		/// 	The class or interface that will be used to resolve the context from the container.
		/// </typeparam>
		/// <param name="services"> The <see cref="IServiceCollection" /> to add services to. </param>
		/// <param name="hubUrl">LinqToDB hub URL.</param>
		/// <param name="getContext">Creates a context over the scoped client.</param>
		/// <param name="configure">Configures the connection: access token, headers, transports, protocol, logging.</param>
		/// <returns>
		///     The same service collection so that multiple calls can be chained.
		/// </returns>
		public static IServiceCollection AddLinqToDBSignalRDataContext<TContext>(
			this IServiceCollection                 services,
			Uri                                     hubUrl,
			Func<SignalRLinqServiceClient,TContext> getContext,
			Action<LinqToDBSignalRClientOptions>?   configure = null)
			where TContext: class, IDataContext
		{
			ArgumentNullException.ThrowIfNull(hubUrl);
			ArgumentNullException.ThrowIfNull(getContext);

			// The context resolves its own connection and client, so that registrations for two hubs (or two context
			// types) do not share the last registered connection.
			services.AddSingleton(provider =>
			{
				var options = new LinqToDBSignalRClientOptions();
				configure?.Invoke(options);
				return new ContextConnection<TContext>(new LinqToDBSignalRConnection(hubUrl, options));
			});

			services.AddScoped(provider => new ContextClient<TContext>(provider.GetRequiredService<ContextConnection<TContext>>().Connection));

			services.AddSingleton(provider => provider.GetRequiredService<ContextConnection<TContext>>().Connection);
			services.AddScoped   (provider => provider.GetRequiredService<ContextClient<TContext>>().Client);

			services.AddTransient(provider =>
			{
				var client = provider.GetRequiredService<ContextClient<TContext>>().Client;
				return getContext(client);
			});

			services.AddTransient<IDataContextFactory<TContext>>(provider =>
			{
				return new DataContextFactory<TContext>(_ =>
				{
					var client = provider.GetRequiredService<ContextClient<TContext>>().Client;
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
		///           services.AddLinqToDBSignalRDataContext&lt;IMyContext&gt;(
		///               builder.HostEnvironment.BaseAddress,
		///               client => new MyContext(client));
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
		/// Starts the <see cref="LinqToDBSignalRConnection"/> registered for <typeparamref name="T"/> unless it is
		/// connected already, and loads the configuration information of <typeparamref name="T"/> context. Calling it
		/// is optional: the first query does the same. Safe to call more than once.
		/// </summary>
		/// <typeparam name="T">IDataContext type.</typeparam>
		/// <param name="serviceProvider">The <see cref="IServiceProvider"/> to get services from.</param>
		/// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
		public static async Task InitSignalRAsync<T>(this IServiceProvider serviceProvider)
			where T : IDataContext
		{
			var connection = serviceProvider.GetService<ContextConnection<T>>()?.Connection ?? serviceProvider.GetRequiredService<LinqToDBSignalRConnection>();

			await connection.EnsureConnectedAsync().ConfigureAwait(false);
			await serviceProvider.GetRequiredService<T>().InitSignalRAsync().ConfigureAwait(false);
		}

		// The connection of one context type; the container disposes it with the holder.
		sealed class ContextConnection<TContext>(LinqToDBSignalRConnection connection) : IAsyncDisposable, IDisposable
		{
			public LinqToDBSignalRConnection Connection { get; } = connection;

			public ValueTask DisposeAsync()
			{
				return Connection.DisposeAsync();
			}

			public void Dispose()
			{
				Connection.Dispose();
			}
		}

		// The scoped client of one context type.
		sealed class ContextClient<TContext>(LinqToDBSignalRConnection connection)
		{
			public SignalRLinqServiceClient Client { get; } = new(connection);
		}
	}
}
