using System;
using System.ServiceModel;

namespace DataModels
{
	public partial class ExampleDataContext
	{
		public ExampleDataContext()
			 : base(
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
				   new EndpointAddress("net.tcp://localhost:30304/LinqOverWcf"))
		{
			((NetTcpBinding)Binding!).ReaderQuotas.MaxStringContentLength = 1000000;
		}
	}
}
