using System.Net.Http;

using Grpc.Net.Client;

namespace DataModels
{
	public partial class ExampleDataContext
	{
		// One channel for the whole application. A GrpcChannel is thread-safe and meant to be long-lived: every
		// context created over it shares its connections instead of opening its own, and none of them disposes it.
		static readonly GrpcChannel _channel = GrpcChannel.ForAddress(
			"https://localhost:15001",
			new GrpcChannelOptions()
			{
#pragma warning disable CA2000 // Dispose objects before losing scope
#pragma warning disable MA0039 // Do not write your own certificate validation method
				// Lives as long as the channel. Accepts the example server's development certificate:
				// do not do this in production.
				HttpHandler = new HttpClientHandler()
				{
					ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
				}
#pragma warning restore MA0039 // Do not write your own certificate validation method
#pragma warning restore CA2000 // Dispose objects before losing scope
			});

		public ExampleDataContext()
			: base(_channel)
		{
		}
	}
}
