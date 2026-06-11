#if !NETFRAMEWORK
using System;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;
using LinqToDB.Remote;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Tests.Model;
using Tests.Model.Remote.HttpContext;

namespace Tests.Remote.ServerContainer
{
	internal sealed class HttpServerContainer : ServerContainerBase<ITestLinqService>
	{
		private static string GetServiceUrl(int port) => $"http://localhost:{port}";

		protected override ITestLinqService StartHost(int port, Func<string?, MappingSchema?, DataConnection> connectionFactory)
		{
			var service = new TestLinqService((c, ms) => connectionFactory(c, ms))
			{
				AllowUpdates    = true,
				RemoteClientTag = "HttpClient",
			};

			Startup.Service = service;

			var builder = Host.CreateDefaultBuilder();

			var host = builder.ConfigureWebHostDefaults(
				webBuilder =>
				{
					webBuilder
						.UseStartup<Startup>()
						.UseUrls(GetServiceUrl(port));
				}).Build();

			host.Start();

			TestExternals.Log("Http host opened");

			return service;
		}

		protected override ITestDataContext CreateClientContext(ITestLinqService service, int port, Func<ITestLinqService, DataOptions, DataOptions> optionBuilder)
		{
			return new TestHttpContextDataContext(new Uri(GetServiceUrl(port)), o => optionBuilder(service, o));
		}

		private sealed class Startup
		{
			internal static ILinqService? Service;

			public void ConfigureServices(IServiceCollection services)
			{
				if (Service == null)
					throw new InvalidOperationException("Http service should be created first");

				services.AddSingleton(Service);
				services.AddControllers();
				services.AddTransient<TestHttpLinqServiceController>();
			}

			public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
			{
				app.UsePathBase("/remote/linq2db");

				app.UseRouting();

				app.UseEndpoints(endpoints =>
				{
					endpoints.MapControllers();
				});
			}
		}
	}
}
#endif
