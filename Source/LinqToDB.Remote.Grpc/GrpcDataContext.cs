using System;
using Grpc.Net.Client;

namespace LinqToDB.Remote.Grpc
{
	public class GrpcDataContext : RemoteDataContextBase
	{
		protected readonly string _address;

		#region Init

		public GrpcDataContext(
			string address
			)
		{
			if (string.IsNullOrWhiteSpace(address))
			{
				throw new ArgumentException($"'{nameof(address)}' cannot be null or whitespace.", nameof(address));
			}

			_address = address;
		}
		
		#endregion

		#region Overrides

		protected override ILinqClient GetClient()
		{
			var channel = GrpcChannel.ForAddress(
				_address
				);

			return new GrpcLinqServiceClient(channel);
		}

		protected override IDataContext Clone()
		{
			return new GrpcDataContext(_address)
			{
				MappingSchema = MappingSchema,
				Configuration = Configuration
			};
		}

		protected override string ContextIDPrefix => "GrpcRemoteLinqService";

		#endregion
	}
}
