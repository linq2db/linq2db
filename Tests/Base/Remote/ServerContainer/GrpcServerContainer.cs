#if NETCOREAPP3_1_OR_GREATER
using System;
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
		public bool KeepSamePortBetweenThreads
		{
			get;
			set;
		} = true;

		private TestGrpcLinqService? _service;
		private bool _isHostOpen;

		public GrpcServerContainer(
			)
		{
		}

		public ITestDataContext Prepare(
			MappingSchema? ms,
			IInterceptor? interceptor,
			bool suppressSequentialAccess,
			string configuration
			)
		{
			OpenHost(ms, interceptor, suppressSequentialAccess);

			_service!.SuppressSequentialAccess = suppressSequentialAccess;
			if (interceptor != null)
			{
				_service!.AddInterceptor(interceptor);
			}

			var dx = new TestGrpcDataContext(
				$"https://localhost:{GetPort()}",
				() =>
				{
					_service!.SuppressSequentialAccess = false;
					if (interceptor != null)
						_service!.RemoveInterceptor();
				})
			{ Configuration = configuration };
		

			Debug.WriteLine(((IDataContext) dx).ContextID, "Provider ");

			if (ms != null)
				dx.MappingSchema = MappingSchema.CombineSchemas(dx.MappingSchema, ms);

			return dx;
		}

		private void OpenHost(MappingSchema? ms, IInterceptor? interceptor, bool suppressSequentialAccess)
		{
			if (_isHostOpen)
			{
				_service!.MappingSchema = ms;
				return;
			}

			lock (_syncRoot)
			{
				if (_isHostOpen)
				{
					_service!.MappingSchema = ms;
					return;
				}

				_service = new TestGrpcLinqService(
					new LinqService()
					{
						AllowUpdates = true
					},
					interceptor,
					suppressSequentialAccess
					);

				Startup.GrpcLinqService = _service;
			}

			var hb = Host.CreateDefaultBuilder();
			var host = hb.ConfigureWebHostDefaults(
				webBuilder =>
				{
					webBuilder.UseStartup<Startup>();

					webBuilder.UseUrls($"https://localhost:{GetPort()}");
				}).Build();

			host.Start();

			//not sure does we need to wait for grpc server starts?

			_isHostOpen = true;

			TestExternals.Log($"grpc host opened");
		}


		//Environment.CurrentManagedThreadId need for a parallel test like <see cref= "DataConnectionTests.MultipleConnectionsTest" />
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
