using System;
using System.Threading;
using System.Threading.Tasks;

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

		// Built from Address and ChannelOptions on first use, shared by every call of this context and disposed with
		// it. Guarded by _channelLock, which also keeps a call racing Dispose from building one after it.
		GrpcChannel? _ownChannel;
		readonly Lock _channelLock = new();

		#region Init

		/// <summary>
		/// Creates instance of grpc-based remote data context.
		/// The context creates a channel to <paramref name="address"/> on first use, uses it for all of its calls and
		/// disposes it when the context is disposed. To share connections between contexts, create one long-lived
		/// <see cref="GrpcChannel"/> and use the <see cref="GrpcDataContext(GrpcChannel, Func{DataOptions, DataOptions})"/>
		/// constructor.
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
		/// The context creates a channel to <paramref name="address"/> with <paramref name="channelOptions"/> on first
		/// use, uses it for all of its calls and disposes it when the context is disposed. To share connections between
		/// contexts, create one long-lived <see cref="GrpcChannel"/> and use the
		/// <see cref="GrpcDataContext(GrpcChannel, Func{DataOptions, DataOptions})"/> constructor.
		/// </summary>
		/// <param name="address">Server address.</param>
		/// <param name="channelOptions">
		/// Optional client channel settings. When they carry an <see cref="GrpcChannelOptions.HttpClient"/> shared with
		/// other contexts, leave <see cref="GrpcChannelOptions.DisposeHttpClient"/> unset: the context's channel would
		/// dispose that client when the context is disposed.
		/// </param>
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
			return new GrpcLinqServiceClient(_callerChannel ?? GetOwnChannel(), ownsChannel: false);
		}

		GrpcChannel GetOwnChannel()
		{
			lock (_channelLock)
			{
				ThrowOnDisposed();

				return _ownChannel ??= ChannelOptions == null ? GrpcChannel.ForAddress(Address) : GrpcChannel.ForAddress(Address, ChannelOptions);
			}
		}

		void DisposeOwnChannel()
		{
			GrpcChannel? channel;

			lock (_channelLock)
			{
				channel     = _ownChannel;
				_ownChannel = null;
			}

			channel?.Dispose();
		}

		public override void Dispose()
		{
			try
			{
				base.Dispose();
			}
			finally
			{
				DisposeOwnChannel();
			}
		}

		public override async ValueTask DisposeAsync()
		{
			try
			{
				await base.DisposeAsync().ConfigureAwait(false);
			}
			finally
			{
				DisposeOwnChannel();
			}
		}

		protected override string ContextIDPrefix => "GrpcRemoteLinqService";

		#endregion
	}
}
