using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

using JetBrains.Annotations;

using Microsoft.Extensions.DependencyInjection;

namespace LinqToDB.Remote.Http.Client
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
		///               services.AddLinqToDBHttpDataContext&lt;IMyContext, MyContext&gt;(
		///                   builder.HostEnvironment.BaseAddress,
		///                   "api/linq2db",
		///                   (service,options) => options.UseSqlServer(connectionString));
		///           }
		///       </code>
		/// </example>
		/// <typeparam name="TContext">
		/// 	The class or interface that will be used to resolve the context from the container.
		/// </typeparam>
		/// <param name="serviceCollection"> The <see cref="IServiceCollection" /> to add services to. </param>
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
		public static IServiceCollection AddLinqToDBHttpDataContext<TContext>(
			this IServiceCollection              serviceCollection,
			string                               baseAddress,
			string                               serviceName,
			Func<HttpLinqServiceClient,TContext> getContext)
			where TContext: class, IDataContext
		{
			_serviceNames.Add(serviceName);

			serviceCollection.AddHttpClient(serviceName, client => client.BaseAddress = new Uri(baseAddress));

			serviceCollection.AddKeyedScoped(serviceName, (provider, _) =>
			{
				var http = provider.GetRequiredService<IHttpClientFactory>().CreateClient(serviceName);
				return new HttpLinqServiceClient(http, serviceName);
			});

			serviceCollection.AddTransient(provider =>
			{
				var client = provider.GetRequiredKeyedService<HttpLinqServiceClient>(serviceName);
				return getContext(client);
			});

			return serviceCollection;
		}

		static readonly HashSet<string> _serviceNames = [];

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
		///               services.AddLinqToDBHttpDataContext&lt;IMyContext, MyContext&gt;(
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
		public static IServiceCollection AddLinqToDBHttpDataContext<TContext>(
			this IServiceCollection              serviceCollection,
			string                               baseAddress,
			Func<HttpLinqServiceClient,TContext> getContext)
			where TContext: class, IDataContext
		{
			return serviceCollection.AddLinqToDBHttpDataContext(baseAddress, "api/linq2db", getContext);
		}

		/// <summary>
		/// Configures LinqToDB Http DataContext.
		/// </summary>
		/// <param name="provider"></param>
		/// <returns></returns>
		public static async Task ConfigureLinqToDBHttpDataContext(this IServiceProvider provider)
		{
			foreach (var serviceName in _serviceNames)
			{
				var client = provider.GetRequiredKeyedService<HttpLinqServiceClient>(serviceName);

				await using var db = new HttpDataContext(client.HttpClient, serviceName);
				await db.ConfigureAsync(default);
			}
		}
	}
}
