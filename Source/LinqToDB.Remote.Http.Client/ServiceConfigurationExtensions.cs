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
		public static IServiceCollection AddLinqToDBHttpDataContext<TContext>(
			this IServiceCollection              services,
			string                               baseAddress,
			string                               serviceName,
			Func<HttpLinqServiceClient,TContext> getContext)
			where TContext: class, IDataContext
		{
			services.AddHttpClient(serviceName, client => client.BaseAddress = new Uri(baseAddress));

			services.AddKeyedScoped(serviceName, (provider, _) =>
			{
				var http = provider.GetRequiredService<IHttpClientFactory>().CreateClient(serviceName);
				return new HttpLinqServiceClient(http, serviceName);
			});

			services.AddTransient(provider =>
			{
				var client = provider.GetRequiredKeyedService<HttpLinqServiceClient>(serviceName);
				return getContext(client);
			});

			services.AddTransient<IDataContextFactory<TContext>>(provider =>
			{
				var client = provider.GetRequiredKeyedService<HttpLinqServiceClient>(serviceName);
				return new DataContextFactory<TContext>(() => getContext(client));
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

		public static async Task InitAsync(this IDataContext dataContext)
		{
			if (dataContext is HttpDataContext httpDataContext)
			{
				await httpDataContext.ConfigureAsync(default).ConfigureAwait(false);
			}
		}
	}
}
