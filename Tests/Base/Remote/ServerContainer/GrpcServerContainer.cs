#if !NETFRAMEWORK
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using LinqToDB;
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

		private readonly object _syncRoot = new ();

		//useful for async tests
		public bool KeepSamePortBetweenThreads { get; set; } = true;

		private ConcurrentDictionary<int, TestGrpcLinqService> _openHosts = new();

		public GrpcServerContainer()
		{
		}

		public ITestDataContext Prepare(
			MappingSchema? ms,
			IInterceptor? interceptor,
			bool suppressSequentialAccess,
			string configuration)
		{
			var service = OpenHost(ms, interceptor, suppressSequentialAccess);

			service.SuppressSequentialAccess = suppressSequentialAccess;
			if (interceptor != null)
			{
				service.AddInterceptor(interceptor);
			}

			var url = $"https://localhost:{GetPort()}";

			var dx = new TestGrpcDataContext(
				url,
				() =>
				{
					service.SuppressSequentialAccess = false;
					if (interceptor != null)
						service.RemoveInterceptor();
				})
			{ Configuration = configuration };
		

			Debug.WriteLine(((IDataContext) dx).ContextID, "Provider ");

			if (ms != null)
				dx.MappingSchema = new MappingSchema(dx.MappingSchema, ms);

			return dx;
		}

		private TestGrpcLinqService OpenHost(MappingSchema? ms, IInterceptor? interceptor, bool suppressSequentialAccess)
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
					new LinqService()
					{
						AllowUpdates = true
					},
					interceptor,
					suppressSequentialAccess
					);
				service.MappingSchema = ms;

				Startup.GrpcLinqService = service;

				var hb = Host.CreateDefaultBuilder();
				var host = hb.ConfigureWebHostDefaults(
				webBuilder =>
				{
					webBuilder.UseStartup<Startup>();

					var url = $"https://localhost:{port}";
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
