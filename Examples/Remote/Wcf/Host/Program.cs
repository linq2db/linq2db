using System;
using System.ServiceModel;
using System.ServiceModel.Description;


namespace Host
{
	class Program
	{
		public static string IP
		{
			get; private set;
		}

		static void Main(string[] args)
		{
			var host = new ServiceHost(
				new LinqToDB.Remote.Wcf.WcfLinqService(
					new LinqToDB.Remote.LinqService() { AllowUpdates = true }),
				new Uri("net.tcp://localhost:30304"));

			host.Description.Behaviors.Add(new ServiceMetadataBehavior());
			host.Description.Behaviors.Find<ServiceDebugBehavior>().IncludeExceptionDetailInFaults = true;
			host.AddServiceEndpoint(typeof(IMetadataExchange), MetadataExchangeBindings.CreateMexTcpBinding(), "mex");
			host.AddServiceEndpoint(
				typeof(LinqToDB.Remote.Wcf.IWcfLinqService),
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

			Console.WriteLine("Press any...");
			Console.ReadKey();

			host.Close();
		}
	}
}
