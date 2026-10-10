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
		/// Gets erver address. For a context created over a caller's channel, it is the channel's <see cref="global::Grpc.Core.ChannelBase.Target"/>.
		/// </summary>
		protected string              Address { get; }
		/// <summary>
		/// Gets GRPC client channel options.
		/// </summary>
		protected GrpcChannelOptions? ChannelOptions { get; }

		// Supplied by the caller, who owns it: used for every call and never disposed by the context.
		readonly GrpcChannel? _callerChannel;

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

		/// <summary>
		/// Creates instance of grpc-based remote data context over an existing channel.
		/// </summary>
		/// <param name="channel">
		/// Channel to the server, used for every call of this context. It stays owned by the caller: the context
		/// never disposes it, so dispose it only after every context that uses it is done with it.
		/// A <see cref="GrpcChannel"/> is thread-safe and meant to be long-lived: create one per server and share it
		/// between contexts, so their queries reuse its connections instead of opening a connection per context.
		/// </param>
		/// <param name="optionBuilder">Optional data context options builder.</param>
		public GrpcDataContext(GrpcChannel channel, Func<DataOptions,DataOptions>? optionBuilder = null)
			: base(optionBuilder == null ? new() : optionBuilder(new()))
		{
			_callerChannel = channel ?? throw new ArgumentNullException(nameof(channel));

			Address = channel.Target;
		}

		#endregion

		#region Overrides

		protected override ILinqService GetClient()
		{
			if (_callerChannel != null)
				return new GrpcLinqServiceClient(_callerChannel, ownsChannel: false);

			var channel = ChannelOptions == null ? GrpcChannel.ForAddress(Address) : GrpcChannel.ForAddress(Address, ChannelOptions);

			return new GrpcLinqServiceClient(channel);
		}

		protected override string ContextIDPrefix => "GrpcRemoteLinqService";

		#endregion
	}
}
