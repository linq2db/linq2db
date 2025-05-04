#if !NETFRAMEWORK
using System;
using System.Collections.Concurrent;

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
	public class HttpServerContainer : IServerContainer
	{
		private const int Port = 22655;

		private readonly Lock _syncRoot = new ();

		//useful for async tests
		public bool KeepSamePortBetweenThreads { get; set; } = true;

		private ConcurrentDictionary<int, ITestLinqService> _openHosts = new();

		private static string GetServiceUrl(int port) => $"https://localhost:{port}";

		private Func<string?, MappingSchema?, DataConnection> _connectionFactory = null!;

		ITestDataContext IServerContainer.CreateContext(Func<ITestLinqService,DataOptions, DataOptions> optionBuilder, Func<string?, MappingSchema?, DataConnection> connectionFactory)
		{
			_connectionFactory = connectionFactory;
			var service = OpenHost();

			var url = GetServiceUrl(GetPort());

			var dx = new TestHttpContextDataContext(new Uri(url), o => optionBuilder(service, o));

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
					AllowUpdates     = true,
					RemoteClientTag = "HttpClient",
				};

				Startup.Service = (TestLinqService)service;

				var builder = Host.CreateDefaultBuilder();

				var host = builder.ConfigureWebHostDefaults(
					webBuilder =>
					{
						webBuilder
							.UseStartup<Startup>()
							.UseUrls(GetServiceUrl(port));
					}).Build();

				host.Start();

				_openHosts[port] = service;
			}

			TestExternals.Log("Http host opened");

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

		private class Startup
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
