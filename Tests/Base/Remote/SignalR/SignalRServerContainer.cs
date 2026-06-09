using System;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;
using LinqToDB.Remote;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Tests.Model;
using Tests.Model.Remote.SignalR;

#if NETFRAMEWORK
using Microsoft.AspNetCore;
#endif

namespace Tests.Remote.ServerContainer
{
	internal sealed class SignalRServerContainer : ServerContainerBase<ITestLinqService>
	{
		private const string HUB_PATH = "/remote/linq2db";

		private static string GetServiceUrl(int port) => $"http://localhost:{port}";

		protected override ITestLinqService StartHost(int port, Func<string?, MappingSchema?, DataConnection> connectionFactory)
		{
			var service = new TestLinqService((c, ms) => connectionFactory(c, ms))
			{
				AllowUpdates    = true,
				RemoteClientTag = "SignalR",
			};

			Startup.Service = service;

#if NETFRAMEWORK
			var host = WebHost.CreateDefaultBuilder().UseUrls(GetServiceUrl(port)).UseStartup<Startup>().Build();
#else
			var builder = Host.CreateDefaultBuilder();

			var host = builder.ConfigureWebHostDefaults(
				webBuilder =>
				{
					webBuilder
						.UseStartup<Startup>()
						.UseUrls(GetServiceUrl(port));
				}).Build();
#endif

			host.Start();

			TestExternals.Log("SignalR host opened");

			return service;
		}

		protected override ITestDataContext CreateClientContext(ITestLinqService service, int port, Func<ITestLinqService, DataOptions, DataOptions> optionBuilder)
		{
			var hubConnection = new HubConnectionBuilder().WithUrl(GetServiceUrl(port) + HUB_PATH).Build();
			hubConnection.StartAsync().GetAwaiter().GetResult();

			return new TestSignalRDataContext(hubConnection, o => optionBuilder(service, o));
		}

		private sealed class Startup
		{
			internal static ILinqService? Service;

			public void ConfigureServices(IServiceCollection services)
			{
				if (Service == null)
					throw new InvalidOperationException("SignalR service should be created first");

				services.AddSingleton(Service);
				services.AddTransient<TestSignalRLinqService>();
				services.AddSignalR();
			}

#if NETFRAMEWORK
			public void Configure(IApplicationBuilder app)
			{
				app.UseSignalR(c =>
				{
					c.MapHub<TestSignalRLinqService>(HUB_PATH);
				});
			}
#else
			public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
			{
				app.UseRouting();

				app.UseEndpoints(endpoints =>
				{
					endpoints.MapHub<TestSignalRLinqService>(HUB_PATH);
				});
			}
#endif
		}
	}
}
