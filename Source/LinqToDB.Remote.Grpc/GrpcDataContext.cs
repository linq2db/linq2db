using System;
using Grpc.Net.Client;

namespace LinqToDB.Remote.Grpc
{
	public class GrpcDataContext : RemoteDataContextBase
	{
		protected string              Address { get; }
		protected GrpcChannelOptions? Options { get; }

		#region Init

		public GrpcDataContext(string address)
		{
			if (string.IsNullOrWhiteSpace(address))
			{
				throw new ArgumentException($"'{nameof(address)}' cannot be null or whitespace.", nameof(address));
			}

			Address = address;
		}

		public GrpcDataContext(string address, GrpcChannelOptions? options)
			: this(address)
		{
			Options = options;
		}

		#endregion

		#region Overrides

		protected override ILinqClient GetClient()
		{
			var channel = Options == null ? GrpcChannel.ForAddress(Address) : GrpcChannel.ForAddress(Address, Options);

			return new GrpcLinqServiceClient(channel);
		}

		protected override IDataContext Clone()
		{
			return new GrpcDataContext(Address, Options)
			{
				MappingSchema = MappingSchema,
				Configuration = Configuration
			};
		}

		protected override string ContextIDPrefix => "GrpcRemoteLinqService";

		#endregion
	}
}
