using System.Net.Http;
using Grpc.Net.Client;
using LinqToDB;
using LinqToDB.Remote;
using LinqToDB.Remote.Grpc;

namespace DataModels
{
	public partial class ExampleDataContext
	{
		public ExampleDataContext()
			: base(
				  "https://localhost:15001",
				  new GrpcChannelOptions()
				  {
					  HttpClient = new HttpClient(
						  new HttpClientHandler
						  {
							  ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
						  })
				  })
		{
		}
	}
}
