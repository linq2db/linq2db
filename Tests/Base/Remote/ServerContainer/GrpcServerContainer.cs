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

namespace Tests.Remote.ServerContainer
{
	using Model;
	using Model.Remote.Grpc;

	public class GrpcServerContainer : IServerContainer
	{
		private const int Port = 22654;

		private readonly object _syncRoot = new ();

		//useful for async tests
		public bool KeepSamePortBetweenThreads { get; set; } = true;

		private ConcurrentDictionary<int, TestGrpcLinqService> _openHosts = new();

		private static string GetServiceUrl(int port)
		{
#if NET6_0
			// https://learn.microsoft.com/en-us/aspnet/core/grpc/troubleshoot?view=aspnetcore-7.0#unable-to-start-aspnet-core-grpc-app-on-macos
			if (OperatingSystem.IsMacOS())
			{
				return $"http://localhost:{port}";
			}
#endif
			return $"https://localhost:{port}";
		}

		private Func<string, MappingSchema?, DataConnection> _connectionFactory = null!;

		ITestDataContext IServerContainer.CreateContext(
			MappingSchema? ms,
			string configuration,
			Func<DataOptions,DataOptions>? optionBuilder,
			Func<string, MappingSchema?, DataConnection> connectionFactory)
		{
			_connectionFactory = connectionFactory;
			var service = OpenHost(ms);

			var url = GetServiceUrl(GetPort());

			var dx = new TestGrpcDataContext(
				url,
				o => optionBuilder == null
					? o.UseConfiguration(configuration)
					: optionBuilder(o.UseConfiguration(configuration)))
			{ ConfigurationString = configuration };

			Debug.WriteLine(((IDataContext) dx).ConfigurationID, "Provider ");

			if (ms != null)
				dx.MappingSchema = MappingSchema.CombineSchemas(ms, dx.MappingSchema);

			return dx;
		}

		private TestGrpcLinqService OpenHost(MappingSchema? ms)
		{
			var port = GetPort();

			if (_openHosts.TryGetValue(port, out var service))
			{
				service.MappingSchema = ms;
				return service;
			}

			lock (_syncRoot)
			{
				if (_openHosts.TryGetValue(port, out service))
				{
					service.MappingSchema = ms;
					return service;
				}

				service = new TestGrpcLinqService(
					new TestLinqService((c, ms) => _connectionFactory(c, ms))
					{
						AllowUpdates     = true,
						RemoteClientTag = "Grpc",
					});

				if (ms != null)
					service.MappingSchema = ms;

				Startup.GrpcLinqService = service;

				var hb = Host.CreateDefaultBuilder();
				var host = hb.ConfigureWebHostDefaults(
				webBuilder =>
				{
					webBuilder.UseStartup<Startup>();

#if NET6_0
					// https://learn.microsoft.com/en-us/aspnet/core/grpc/troubleshoot?view=aspnetcore-7.0#unable-to-start-aspnet-core-grpc-app-on-macos
					if (OperatingSystem.IsMacOS())
					{
						webBuilder.ConfigureKestrel(o =>
						{
							o.ListenLocalhost(port, o => o.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http2);
						});
					}
#endif

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
