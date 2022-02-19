#if NETFRAMEWORK

using System;
using LinqToDB.Interceptors;
using LinqToDB.Mapping;
using LinqToDB.Remote;
using System.ServiceModel;
using System.ServiceModel.Description;
using LinqToDB.Remote.WCF;
using System.Diagnostics;
using LinqToDB;
using Tests.Model.Remote.WCF;
using Tests.Model;

namespace Tests.Remote.ServerContainer
{

	public class WcfServerContainer : IServerContainer
	{
		private const int Port = 22654;

		private readonly object _syncRoot = new ();

		//useful for async tests
		public bool KeepSamePortBetweenThreads
		{
			get;
			set;
		} = true;

		private TestWcfLinqService? _service;
		private bool _isHostOpen;

		public WcfServerContainer(
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
			OpenHost(ms);

			_service!.SuppressSequentialAccess = suppressSequentialAccess;
			if (interceptor != null)
			{
				_service!.AddInterceptor(interceptor);
			}

			var dx = new TestWcfDataContext(
				GetPort(),
				() =>
				{
					_service!.SuppressSequentialAccess = false;
					if (interceptor != null)
						_service!.RemoveInterceptor();
				})
			{ Configuration = configuration };

			Debug.WriteLine(((IDataContext)dx).ContextID, "Provider ");

			if (ms != null)
				dx.MappingSchema = MappingSchema.CombineSchemas(dx.MappingSchema, ms);

			return dx;
		}

		private void OpenHost(MappingSchema? ms)
		{
			if (_isHostOpen)
			{
				_service!.MappingSchema = ms;
				return;
			}

			ServiceHost host;

			lock (_syncRoot)
			{
				if (_isHostOpen)
				{
					_service!.MappingSchema = ms;
					return;
				}

				host = new ServiceHost(_service = new TestWcfLinqService(new LinqService(ms), null, false) { AllowUpdates = true }, new Uri($"net.tcp://localhost:{GetPort()}"));
				_isHostOpen = true;
			}

			host.Description.Behaviors.Add(new ServiceMetadataBehavior());
			host.Description.Behaviors.Find<ServiceDebugBehavior>().IncludeExceptionDetailInFaults = true;
			host.AddServiceEndpoint(typeof(IMetadataExchange), MetadataExchangeBindings.CreateMexTcpBinding(), "mex");
			host.AddServiceEndpoint(
				typeof(IWcfLinqService),
				new NetTcpBinding(SecurityMode.None)
				{
					MaxReceivedMessageSize = 10000000,
					MaxBufferPoolSize = 10000000,
					MaxBufferSize = 10000000,
					CloseTimeout = new TimeSpan(00, 01, 00),
					OpenTimeout = new TimeSpan(00, 01, 00),
					ReceiveTimeout = new TimeSpan(00, 10, 00),
					SendTimeout = new TimeSpan(00, 10, 00),
				},
				"LinqOverWCF");

			host.Open();

			TestExternals.Log($"WCF host opened, Address : {host.BaseAddresses[0]}");
		}


		//Environment.CurrentManagedThreadId need for a parallel test like <see cref= "DataConnectionTests.MultipleConnectionsTest" />
		public int GetPort()
		{
			return Port + TestExternals.RunID;
		}

	}
}

#endif
