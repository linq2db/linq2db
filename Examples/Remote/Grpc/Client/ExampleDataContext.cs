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
					  // HttpClient instantiated manually for simplicilty of example
					  // in real code concider use of dependency injection and IHttpClientFactory
					  // https://docs.microsoft.com/en-us/dotnet/core/extensions/http-client
					  HttpClient = new HttpClient(
						  new HttpClientHandler()
						  {
							  ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
						  })
				  })
		{
		}
	}
}
