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
							  // the example server listens on localhost with the untrusted ASP.NET Core development certificate
#pragma warning disable MA0039 // Do not write your own certificate validation method
							  ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
#pragma warning restore MA0039 // Do not write your own certificate validation method
						  })
#pragma warning restore CA2000 // Dispose objects before losing scope
				  })
		{
		}
	}
}
