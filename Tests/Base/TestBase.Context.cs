using System;
using System.Collections.Generic;
using System.Diagnostics;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Data.RetryPolicy;
using LinqToDB.DataProvider;
using LinqToDB.FSharp;
using LinqToDB.Interceptors;
using LinqToDB.Mapping;

using Tests.Model;
using Tests.Remote;
using Tests.Remote.ServerContainer;

namespace Tests
{
	public partial class TestBase
	{
		static readonly MappingSchema _sequentialAccessSchema = new ("SequentialAccess");

		private static readonly IReadOnlyDictionary<RemoteTransport, IServerContainer> _serverContainers = new Dictionary<RemoteTransport, IServerContainer>()
		{
#if NETFRAMEWORK
			{ RemoteTransport.WCF,     new WcfServerContainer()     },
#else
			{ RemoteTransport.gRPC,    new GrpcServerContainer()    },
			{ RemoteTransport.Http,    new HttpServerContainer()    },
#endif
			{ RemoteTransport.SignalR, new SignalRServerContainer() },
		};

		protected IServerContainer GetServerContainer(RemoteTransport transport = DefaultTransport) => _serverContainers[transport];

#if NETFRAMEWORK
		protected const RemoteTransport DefaultTransport = RemoteTransport.WCF;
#else
		protected const RemoteTransport DefaultTransport = RemoteTransport.gRPC;
#endif

		public enum RemoteTransport
		{
#if NETFRAMEWORK
			WCF     = 1,
#else
			gRPC    = 2,
			Http    = 3,
#endif
			SignalR = 4
		}

		protected ITestDataContext GetDataContext(
			string configuration,
			MappingSchema? mappingSchema  = null,
			bool testLinqService          = true,
			IInterceptor? interceptor     = null,
			bool suppressSequentialAccess = false,
			RemoteTransport transport     = DefaultTransport)
		{
			if (!configuration.IsRemote())
			{
				return GetDataConnection(configuration, mappingSchema, interceptor, suppressSequentialAccess: suppressSequentialAccess);
			}

			var str = configuration.StripRemote();
			return GetServerContainer(transport).CreateContext(
				GetRemoteContextOptionsBuilder(mappingSchema, str, opt => opt.UseFSharp()),
				(conf, ms) =>
				{
					var dc = new DataConnection(conf);

					if (conf?.IsAnyOf(TestProvName.AllSqlServerSequentialAccess) == true)
					{
						if (!suppressSequentialAccess)
							dc.AddInterceptor(SequentialAccessCommandInterceptor.Instance);

						ms = ms == null ? _sequentialAccessSchema : MappingSchema.CombineSchemas(ms, _sequentialAccessSchema);
					}

					if (interceptor != null)
						dc.AddInterceptor(interceptor);

					if (ms != null)
						dc.AddMappingSchema(ms);

					return dc;
				});
		}

		protected static string GetConnectionString(string configuration)
		{
			return DataConnection.GetConnectionString(configuration.StripRemote());
		}

		protected static IDataProvider GetDataProvider(string configuration)
		{
			return DataConnection.GetDataProvider(configuration.StripRemote());
		}

		protected ITestDataContext GetDataContext(string configuration, Func<DataOptions, DataOptions> dbOptionsBuilder, RemoteTransport transport = DefaultTransport)
		{
			if (!configuration.IsRemote())
			{
				return GetDataConnection(configuration, dbOptionsBuilder);
			}

			var str = configuration.StripRemote();
			return GetServerContainer(transport).CreateContext(
				GetRemoteContextOptionsBuilder(null, str, opt => dbOptionsBuilder(opt).UseFSharp()),
				(conf, ms) =>
				{
					var dc = new DataConnection(conf);
					if (ms != null)
						dc.AddMappingSchema(ms);
					return dc;
				});
		}

		static Func<ITestLinqService, DataOptions, DataOptions> GetRemoteContextOptionsBuilder(MappingSchema? ms, string configuration, Func<DataOptions, DataOptions>? testOptionsBuilder)
		{
			return (service, o) =>
			{
				var options = testOptionsBuilder == null
					? o.UseConfiguration(configuration)
					: testOptionsBuilder(o.UseConfiguration(configuration));

				if (ms != null)
					options = options.UseMappingSchema(
						options.ConnectionOptions.MappingSchema != null
							? MappingSchema.CombineSchemas(ms, options.ConnectionOptions.MappingSchema)
							: ms);

				service.MappingSchema = options.ConnectionOptions.MappingSchema;

				return options;
			};
		}

		protected TestDataConnection GetDataConnection(string configuration, Func<DataOptions, DataOptions> dbOptionsBuilder)
		{
			if (configuration.IsRemote())
			{
				throw new InvalidOperationException($"Call {nameof(GetDataContext)} for remote context creation");
			}

			Debug.WriteLine(configuration, "Provider ");

			var options = new DataOptions().UseConfiguration(configuration);

			if (configuration.IsAnyOf(TestProvName.AllSqlServerSequentialAccess))
			{
				//if (!suppressSequentialAccess)
				options = options.UseInterceptor(SequentialAccessCommandInterceptor.Instance);

				options = options.UseMappingSchema(options.ConnectionOptions.MappingSchema == null ? _sequentialAccessSchema : MappingSchema.CombineSchemas(options.ConnectionOptions.MappingSchema, _sequentialAccessSchema));
			}

			options = dbOptionsBuilder(options).UseFSharp();

			var res = new TestDataConnection(options);

			/*
			// add extra mapping schema to not share mappers with other sql2017/2019 providers
			// use same schema to use cache within test provider scope
			if (configuration.IsAnyOf(TestProvName.AllSqlServerSequentialAccess))
			{
				if (!suppressSequentialAccess)
					res.AddInterceptor(SequentialAccessCommandInterceptor.Instance);

				res.AddMappingSchema(_sequentialAccessSchema);
			}
			*/

			return res;
		}

		protected TestDataConnection GetDataConnection(
			string configuration,
			MappingSchema? ms = null,
			IInterceptor? interceptor = null,
			IRetryPolicy? retryPolicy = null,
			bool suppressSequentialAccess = false)
		{
			if (configuration.IsRemote())
			{
				throw new InvalidOperationException($"Call {nameof(GetDataContext)} for remote context creation");
			}

			Debug.WriteLine(configuration, "Provider ");

			var options = new DataOptions().UseConfiguration(configuration);

			if (ms != null)
				options = options.UseMappingSchema(ms);

			// add extra mapping schema to not share mappers with other sql2017/2019 providers
			// use same schema to use cache within test provider scope
			if (configuration.IsAnyOf(TestProvName.AllSqlServerSequentialAccess))
			{
				if (!suppressSequentialAccess)
					options = options.UseInterceptor(SequentialAccessCommandInterceptor.Instance);

				options = options.UseMappingSchema(ms == null ? _sequentialAccessSchema : MappingSchema.CombineSchemas(ms, _sequentialAccessSchema));
			}

			if (interceptor != null)
				options = options.UseInterceptor(interceptor);

			if (retryPolicy != null)
				options = options.UseRetryPolicy(retryPolicy);

			options = options.UseFSharp();
			return new TestDataConnection(options);
		}

		protected TestDataConnection GetDataConnection(DataOptions options)
		{
			if (options.ConnectionOptions.ConfigurationString?.IsRemote() == true)
				throw new InvalidOperationException($"Call {nameof(GetDataContext)} for remote context creation");

			Debug.WriteLine(options.ConnectionOptions.ConfigurationString, "Provider ");

			options = options.UseFSharp();
			var res = new TestDataConnection(options);

			return res;
		}
	}
}
