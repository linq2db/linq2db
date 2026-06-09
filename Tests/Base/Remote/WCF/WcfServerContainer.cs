#if NETFRAMEWORK
using System;
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
	internal sealed class WcfServerContainer : ServerContainerBase<TestWcfLinqService>
	{
		protected override TestWcfLinqService StartHost(int port, Func<string?, MappingSchema?, DataConnection> connectionFactory)
		{
#pragma warning disable CA2000 // Dispose objects before losing scope
			var service = new TestWcfLinqService(
				new TestLinqService((c, ms) => connectionFactory(c, ms))
				{
					RemoteClientTag = "Wcf",
				})
				{
					AllowUpdates = true,
				};

			var host = new ServiceHost(service, new Uri($"net.tcp://localhost:{port}"));
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

			TestExternals.Log($"WCF host opened, Address : {host.BaseAddresses[0]}");

			return service;
		}

		protected override ITestDataContext CreateClientContext(TestWcfLinqService service, int port, Func<ITestLinqService, DataOptions, DataOptions> optionBuilder)
		{
			return new TestWcfDataContext(port, o => optionBuilder(service, o));
		}

		private void Host_Faulted(object sender, EventArgs e)
		{
			throw new NotImplementedException();
		}
	}
}
#endif
