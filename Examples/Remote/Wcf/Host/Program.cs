using System;
using System.ServiceModel;
using System.ServiceModel.Description;

namespace Host
{
	using LinqToDB.Remote;
	using LinqToDB.Remote.Wcf;

	sealed class Program
	{
		static void Main(string[] args)
		{
			using var host = new ServiceHost(
				new WcfLinqService(new LinqService() { AllowUpdates = true }, true),
				new Uri("net.tcp://localhost:30304"));

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

			host.Open();

			Console.WriteLine("Press any key to exit");
			Console.ReadKey();

			host.Close();
		}
	}
}
