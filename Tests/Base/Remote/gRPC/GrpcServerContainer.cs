#if !NETFRAMEWORK
using System;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using ProtoBuf.Grpc.Server;

using Tests.Model;
using Tests.Model.Remote.Grpc;

namespace Tests.Remote.ServerContainer
{
	internal sealed class GrpcServerContainer : ServerContainerBase<TestGrpcLinqService>
	{
		private static string GetServiceUrl(int port) => $"https://localhost:{port}";

		protected override TestGrpcLinqService StartHost(int port, Func<string?, MappingSchema?, DataConnection> connectionFactory)
		{
			var service = new TestGrpcLinqService(
				new TestLinqService((c, ms) => connectionFactory(c, ms))
				{
					AllowUpdates    = true,
					RemoteClientTag = "Grpc",
				});

			Startup.GrpcLinqService = service;

			var host = Host.CreateDefaultBuilder().ConfigureWebHostDefaults(
				webBuilder =>
				{
					webBuilder.UseStartup<Startup>();
					webBuilder.UseUrls(GetServiceUrl(port));
				}).Build();

			host.Start();

			TestExternals.Log("gRCP host opened");

			return service;
		}

		protected override ITestDataContext CreateClientContext(TestGrpcLinqService service, int port, Func<ITestLinqService, DataOptions, DataOptions> optionBuilder)
		{
			return new TestGrpcDataContext(GetServiceUrl(port), o => optionBuilder(service, o));
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
