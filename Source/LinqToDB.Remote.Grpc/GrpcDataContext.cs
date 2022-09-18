using System;
using Grpc.Net.Client;

namespace LinqToDB.Remote.Grpc
{
	/// <summary>
	/// Remote data context implementation over GRPC.
	/// </summary>
	public class GrpcDataContext : RemoteDataContextBase
	{
		/// <summary>
		/// Gets erver address.
		/// </summary>
		protected string              Address { get; }
		/// <summary>
		/// Gets GRPC client channel options.
		/// </summary>
		protected GrpcChannelOptions? Options { get; }

		#region Init

		/// <summary>
		/// Creates instance of grpc-based remote data context.
		/// </summary>
		/// <param name="address">Server address.</param>
		public GrpcDataContext(string address)
		{
			if (string.IsNullOrWhiteSpace(address))
				throw new ArgumentException($"'{nameof(address)}' cannot be null or whitespace.", nameof(address));

			Address = address;
		}

		/// <summary>
		/// Creates instance of grpc-based remote data context.
		/// </summary>
		/// <param name="address">Server address.</param>
		/// <param name="options">Optional client channel settings.</param>
		public GrpcDataContext(string address, GrpcChannelOptions? options)
			: this(address)
		{
			Options = options;
		}

		#endregion

		#region Overrides

		protected override ILinqService GetClient()
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
