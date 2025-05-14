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

		private Func<string?, MappingSchema?, DataConnection> _connectionFactory = null!;

		ITestDataContext IServerContainer.CreateContext(Func<ITestLinqService,DataOptions, DataOptions> optionBuilder, Func<string?, MappingSchema?, DataConnection> connectionFactory)
		{
			_connectionFactory = connectionFactory;

			var service = OpenHost();

			var dx = new TestWcfDataContext(GetPort(), o => optionBuilder(service, o));

			return dx;
		}

		private TestWcfLinqService OpenHost()
		{
			var port = GetPort();

			if (_openHosts.TryGetValue(port, out var service))
				return service;

			lock (_syncRoot)
			{
				if (_openHosts.TryGetValue(port, out service))
					return service;

#pragma warning disable CA2000 // Dispose objects before losing scope
				var host = new ServiceHost(
					service = new TestWcfLinqService(
						new TestLinqService((c, ms) => _connectionFactory(c, ms))
						{
							RemoteClientTag = "Wcf",
						})
						{
							AllowUpdates = true,
						},
					new Uri($"net.tcp://localhost:{GetPort()}"));
#pragma warning restore CA2000 // Dispose objects before losing scope

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
