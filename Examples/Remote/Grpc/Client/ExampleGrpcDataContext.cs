using System.Net.Http;
using Grpc.Net.Client;
using LinqToDB;
using LinqToDB.Remote;
using LinqToDB.Remote.Grpc;

namespace Client
{
	public class ExampleGrpcDataContext : GrpcDataContext
	{
		public ExampleGrpcDataContext(string address)
			: base(address)
		{
		}

		protected override ILinqClient GetClient()
		{
			var channel = GrpcChannel.ForAddress(
				_address,
				new GrpcChannelOptions
				{
					HttpClient = new HttpClient(
						new HttpClientHandler
						{
							ServerCertificateCustomValidationCallback =
								(httpRequestMessage, x509Certificate2, x509Chain, sslPolicyErrors) => true
						})
				}
				);

			return new GrpcLinqServiceClient(channel);
		}

		protected override IDataContext Clone()
		{
			return new ExampleGrpcDataContext(_address)
			{
				MappingSchema = MappingSchema,
				Configuration = Configuration
			};
		}
	}
}
