#if NETFRAMEWORK
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.ServiceModel;
using System.ServiceModel.Description;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Interceptors;
using LinqToDB.Mapping;
using LinqToDB.Remote;
using LinqToDB.Remote.Wcf;

using Tests.Model;
using Tests.Model.Remote.Wcf;

namespace Tests.Remote.ServerContainer
{
	public class WcfServerContainer : IServerContainer
	{
		private const int Port = 22654;

		private readonly Lock _syncRoot = new ();

		//useful for async tests
		public bool KeepSamePortBetweenThreads { get; set; } = true;

		private ConcurrentDictionary<int, TestWcfLinqService> _openHosts = new();

		private Func<string, MappingSchema?, DataConnection> _connectionFactory = null!;

		ITestDataContext IServerContainer.CreateContext(
			MappingSchema? ms,
			string configuration,
			Func<DataOptions, DataOptions>? optionBuilder,
			Func<string, MappingSchema?, DataConnection> connectionFactory)
		{
			_connectionFactory = connectionFactory;

			var service = OpenHost(ms);

			var dx = new TestWcfDataContext(
				GetPort(),
				o => optionBuilder == null
					? o.UseConfiguration(configuration)
					: optionBuilder(o.UseConfiguration(configuration)))
			{ ConfigurationString = configuration };

			Debug.WriteLine(((IDataContext)dx).ConfigurationID, "Provider ");

			if (ms != null)
				dx.MappingSchema = dx.MappingSchema == null ? ms : MappingSchema.CombineSchemas(ms, dx.MappingSchema);

			return dx;
		}

		private TestWcfLinqService OpenHost(MappingSchema? ms)
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

#pragma warning disable CA2000 // Dispose objects before losing scope
				var host = new ServiceHost(
					service = new TestWcfLinqService(
						new TestLinqService((c, ms) => _connectionFactory(c, ms)))
						{
							AllowUpdates = true
						},
					new Uri($"net.tcp://localhost:{GetPort()}"));
#pragma warning restore CA2000 // Dispose objects before losing scope

				if (ms != null)
					service.MappingSchema = ms;

				host.Description.Behaviors.Add(new ServiceMetadataBehavior());
				host.Description.Behaviors.Find<ServiceDebugBehavior>().IncludeExceptionDetailInFaults = true;
				host.AddServiceEndpoint(typeof(IMetadataExchange), MetadataExchangeBindings.CreateMexTcpBinding(), "mex");
				host.AddServiceEndpoint(
					typeof(IWcfLinqService),
					new NetTcpBinding(SecurityMode.None)
					{
						MaxReceivedMessageSize = 10000000,
						MaxBufferPoolSize      = 10000000,
						MaxBufferSize          = 10000000,
						CloseTimeout           = new TimeSpan(00, 01, 00),
						OpenTimeout            = new TimeSpan(00, 01, 00),
						ReceiveTimeout         = new TimeSpan(00, 10, 00),
						SendTimeout            = new TimeSpan(00, 10, 00),
					},
					"LinqOverWcf");

				host.Faulted += Host_Faulted;
				host.Open();

				_openHosts[port] = service;

				TestExternals.Log($"WCF host opened, Address : {host.BaseAddresses[0]}");
			}

			return service;
		}

		private void Host_Faulted(object sender, EventArgs e)
		{
			throw new NotImplementedException();
		}

		public int GetPort()
		{
			if (KeepSamePortBetweenThreads)
			{
				return Port;
			}

			return Port + (Environment.CurrentManagedThreadId % 1000) + TestExternals.RunID;
		}
	}
}
#endif
