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
		protected GrpcChannelOptions? ChannelOptions { get; }

		#region Init

		/// <summary>
		/// Creates instance of grpc-based remote data context.
		/// </summary>
		/// <param name="address">Server address.</param>
		public GrpcDataContext(string address, Func<DataOptions,DataOptions>? optionBuilder = null)
			: base(optionBuilder == null ? new() : optionBuilder(new()))
		{
			if (string.IsNullOrWhiteSpace(address))
				throw new ArgumentException($"'{nameof(address)}' cannot be null or whitespace.", nameof(address));

			Address = address;
		}

		/// <summary>
		/// Creates instance of grpc-based remote data context.
		/// </summary>
		/// <param name="address">Server address.</param>
		/// <param name="channelOptions">Optional client channel settings.</param>
		public GrpcDataContext(string address, GrpcChannelOptions? channelOptions, Func<DataOptions,DataOptions>? optionBuilder = null)
			: this(address, optionBuilder)
		{
			ChannelOptions = channelOptions;
		}

		#endregion

		#region Overrides

		protected override ILinqService GetClient()
		{
			var channel = ChannelOptions == null ? GrpcChannel.ForAddress(Address) : GrpcChannel.ForAddress(Address, ChannelOptions);

			return new GrpcLinqServiceClient(channel);
		}

		protected override string ContextIDPrefix => "GrpcRemoteLinqService";

		#endregion
	}
}
