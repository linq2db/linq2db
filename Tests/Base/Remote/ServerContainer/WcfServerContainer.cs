﻿#if NETFRAMEWORK
using System;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.Diagnostics;
using System.Collections.Concurrent;

using LinqToDB;
using LinqToDB.Interceptors;
using LinqToDB.Mapping;
using LinqToDB.Remote;
using LinqToDB.Remote.Wcf;

namespace Tests.Remote.ServerContainer
{
	using Model;
	using Tests.Model.Remote.Wcf;

	public class WcfServerContainer : IServerContainer
	{
		private const int Port = 22654;

		private readonly object _syncRoot = new ();

		//useful for async tests
		public bool KeepSamePortBetweenThreads { get; set; } = true;

		private ConcurrentDictionary<int, TestWcfLinqService> _openHosts = new();

		public WcfServerContainer()
		{
		}

		public ITestDataContext Prepare(
			MappingSchema? ms,
			IInterceptor? interceptor,
			bool suppressSequentialAccess,
			string configuration)
		{
			var service = OpenHost(ms);

			service.SuppressSequentialAccess = suppressSequentialAccess;
			if (interceptor != null)
			{
				service.AddInterceptor(interceptor);
			}

			var dx = new TestWcfDataContext(
				GetPort(),
				() =>
				{
					service.SuppressSequentialAccess = false;
					if (interceptor != null)
						service.RemoveInterceptor();
				})
			{ Configuration = configuration };

			Debug.WriteLine(((IDataContext)dx).ContextID, "Provider ");

			if (ms != null)
				dx.MappingSchema = new MappingSchema(dx.MappingSchema, ms);

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

				var host = new ServiceHost(service = new TestWcfLinqService(new LinqService(), null, false) { AllowUpdates = true }, new Uri($"net.tcp://localhost:{GetPort()}"));
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
