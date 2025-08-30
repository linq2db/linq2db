using System.Net.Http;

using Grpc.Net.Client;

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
#pragma warning disable CA2000 // Dispose objects before losing scope
						  new HttpClientHandler()
						  {
							  ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
						  })
#pragma warning restore CA2000 // Dispose objects before losing scope
				  })
		{
		}
	}
}
