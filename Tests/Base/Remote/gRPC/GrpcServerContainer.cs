#if !NETFRAMEWORK
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Security.Authentication;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Interceptors;
using LinqToDB.Mapping;
using LinqToDB.Remote;
using LinqToDB.Remote.Grpc;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using NUnit.Framework;

using ProtoBuf.Grpc.Server;

using Tests.Model;
using Tests.Model.Remote.Grpc;

namespace Tests.Remote.ServerContainer
{
	public class GrpcServerContainer : IServerContainer
	{
		private const int Port = 22654;

		private readonly Lock _syncRoot = new ();

		//useful for async tests
		public bool KeepSamePortBetweenThreads { get; set; } = true;

		private ConcurrentDictionary<int, TestGrpcLinqService> _openHosts = new();

		private static string GetServiceUrl(int port)
		{
			return $"https://localhost:{port}";
		}

		private Func<string?, MappingSchema?, DataConnection> _connectionFactory = null!;

		ITestDataContext IServerContainer.CreateContext(Func<ITestLinqService,DataOptions, DataOptions> optionBuilder, Func<string?, MappingSchema?, DataConnection> connectionFactory)
		{
			_connectionFactory = connectionFactory;
			var service = OpenHost();

			var url = GetServiceUrl(GetPort());

			var dx = new TestGrpcDataContext(url, o => optionBuilder(service, o));

			return dx;
		}

		private TestGrpcLinqService OpenHost()
		{
			var port = GetPort();

			if (_openHosts.TryGetValue(port, out var service))
				return service;

			lock (_syncRoot)
			{
				if (_openHosts.TryGetValue(port, out service))
					return service;

				service = new TestGrpcLinqService(
					new TestLinqService((c, ms) => _connectionFactory(c, ms))
					{
						AllowUpdates     = true,
						RemoteClientTag = "Grpc",
					});

				Startup.GrpcLinqService = service;

				var hb = Host.CreateDefaultBuilder();
				var host = hb.ConfigureWebHostDefaults(
				webBuilder =>
				{
					webBuilder.UseStartup<Startup>();

					var url = GetServiceUrl(port);
					webBuilder.UseUrls(url);
				}).Build();

				host.Start();

				_openHosts[port] = service;
			}

			TestExternals.Log("gRCP host opened");

			return service;
		}

		//Environment.CurrentManagedThreadId need for a parallel test like DataConnectionTests.MultipleConnectionsTest
		public int GetPort()
		{
			if(KeepSamePortBetweenThreads)
			{
				return Port;
			}

			return Port + (Environment.CurrentManagedThreadId % 1000) + TestExternals.RunID;
		}

		public class Startup
		{
			internal static TestGrpcLinqService? GrpcLinqService;

			public void ConfigureServices(IServiceCollection services)
			{
				if (GrpcLinqService == null)
				{
					throw new InvalidOperationException("Grpc service should be created first");
				}

				services.AddGrpc();
				services.AddCodeFirstGrpc();
				services.AddSingleton(p => GrpcLinqService);

			}

			public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
			{
				app.UseDeveloperExceptionPage();

				app.UseRouting();

				app.UseEndpoints(endpoints =>
				{
					endpoints.MapGrpcService<TestGrpcLinqService>();
				});
			}
		}
	}
}
#endif
