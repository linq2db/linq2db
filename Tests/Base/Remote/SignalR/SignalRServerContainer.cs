using System;
using System.Collections.Concurrent;
using System.Threading;

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
	public class SignalRServerContainer : IServerContainer
	{
		private const string HUB_PATH = "/remote/linq2db";
		private const int Port = 22656;

		private readonly Lock _syncRoot = new ();

		//useful for async tests
		public bool KeepSamePortBetweenThreads { get; set; } = true;

		private ConcurrentDictionary<int, ITestLinqService> _openHosts = new();

		private static string GetServiceUrl(int port) => $"http://localhost:{port}";

		private Func<string?, MappingSchema?, DataConnection> _connectionFactory = null!;

		ITestDataContext IServerContainer.CreateContext(Func<ITestLinqService,DataOptions, DataOptions> optionBuilder, Func<string?, MappingSchema?, DataConnection> connectionFactory)
		{
			_connectionFactory = connectionFactory;
			var service = OpenHost();

			var url = GetServiceUrl(GetPort());

			var hubConnection = new HubConnectionBuilder().WithUrl(url + HUB_PATH).Build();
			hubConnection.StartAsync().GetAwaiter().GetResult();
			var dx = new TestSignalRDataContext(hubConnection, o => optionBuilder(service, o));

			return dx;
		}

		private ITestLinqService OpenHost()
		{
			var port = GetPort();

			if (_openHosts.TryGetValue(port, out var service))
				return service;

			lock (_syncRoot)
			{
				if (_openHosts.TryGetValue(port, out service))
					return service;

				service = new TestLinqService((c, ms) => _connectionFactory(c, ms))
				{
					AllowUpdates    = true,
					RemoteClientTag = "SignalR",
				};

				Startup.Service = (TestLinqService)service;

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

				_openHosts[port] = service;
			}

			TestExternals.Log("SignalR host opened");

			return service;
		}

		//Environment.CurrentManagedThreadId need for a parallel test like DataConnectionTests.MultipleConnectionsTest
		private int GetPort()
		{
			if(KeepSamePortBetweenThreads)
			{
				return Port;
			}

			return Port + (Environment.CurrentManagedThreadId % 1000) + TestExternals.RunID;
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
