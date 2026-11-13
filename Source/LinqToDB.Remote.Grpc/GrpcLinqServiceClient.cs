using System;
using System.Threading;
using System.Threading.Tasks;

using Grpc.Core;
using Grpc.Net.Client;

using LinqToDB.Remote.Grpc.Dto;

namespace LinqToDB.Remote.Grpc
{
	/// <summary>
	/// grpc-base remote data context client.
	/// </summary>
	public class GrpcLinqServiceClient : ILinqService, IDisposable
	{
		private readonly GrpcChannel      _channel;
		private readonly IGrpcLinqService _client;
		private readonly bool             _ownsChannel;

		/// <summary>
		/// Creates a client that owns <paramref name="channel"/> and disposes it when the client is disposed.
		/// </summary>
		/// <param name="channel">gRPC channel to the server.</param>
		public GrpcLinqServiceClient(GrpcChannel channel)
			: this(channel, ownsChannel: true)
		{
		}

		// A client over a channel whose lifetime is managed elsewhere leaves it open when disposed.
		internal GrpcLinqServiceClient(GrpcChannel channel, bool ownsChannel)
		{
			_channel     = channel;
			_ownsChannel = ownsChannel;
			// through the generated factory rather than CreateGrpcService(), whose proxy is reflection-built
			_client  = GrpcLinqServiceProxies.Instance.CreateClient<IGrpcLinqService>(channel.CreateCallInvoker());
		}

		async Task<LinqServiceInfo> ILinqService.GetInfoAsync(string? configuration, CancellationToken cancellationToken)
		{
			try
			{
				return await _client.GetInfoAsync(
					new GrpcConfiguration()
					{
						Configuration = configuration,
					}, cancellationToken)
					.ConfigureAwait(false);
			}
			catch (RpcException exception) when (IsCallerCancellation(exception, cancellationToken))
			{
				throw CallerCancellation(exception, cancellationToken);
			}
		}

		async Task<int> ILinqService.ExecuteNonQueryAsync(string? configuration, string queryData, CancellationToken cancellationToken)
		{
			try
			{
				return await _client.ExecuteNonQueryAsync(
					new GrpcConfigurationQuery()
					{
						Configuration = configuration,
						QueryData     = queryData,
					}, cancellationToken)
					.ConfigureAwait(false);
			}
			catch (RpcException exception) when (IsCallerCancellation(exception, cancellationToken))
			{
				throw CallerCancellation(exception, cancellationToken);
			}
		}

		async Task<string?> ILinqService.ExecuteScalarAsync(string? configuration, string queryData, CancellationToken cancellationToken)
		{
			try
			{
				return await _client.ExecuteScalarAsync(
					new GrpcConfigurationQuery()
					{
						Configuration = configuration,
						QueryData     = queryData,
					}, cancellationToken)
					.ConfigureAwait(false);
			}
			catch (RpcException exception) when (IsCallerCancellation(exception, cancellationToken))
			{
				throw CallerCancellation(exception, cancellationToken);
			}
		}

		async Task<string> ILinqService.ExecuteReaderAsync(string? configuration, string queryData, CancellationToken cancellationToken)
		{
			GrpcString result;

			try
			{
				result = await _client.ExecuteReaderAsync(
					new GrpcConfigurationQuery()
					{
						Configuration = configuration,
						QueryData     = queryData,
					}, cancellationToken)
					.ConfigureAwait(false);
			}
			catch (RpcException exception) when (IsCallerCancellation(exception, cancellationToken))
			{
				throw CallerCancellation(exception, cancellationToken);
			}

			return result.Value ?? throw new LinqToDBException("Return value is not allowed to be null");
		}

		async Task<int> ILinqService.ExecuteBatchAsync(string? configuration, string queryData, CancellationToken cancellationToken)
		{
			try
			{
				return await _client.ExecuteBatchAsync(
					new GrpcConfigurationQuery()
					{
						Configuration = configuration,
						QueryData     = queryData,
					}, cancellationToken)
					.ConfigureAwait(false);
			}
			catch (RpcException exception) when (IsCallerCancellation(exception, cancellationToken))
			{
				throw CallerCancellation(exception, cancellationToken);
			}
		}

		// grpc-dotnet reports a call cancelled through the caller's token as RpcException(Cancelled). Callers of
		// ILinqService expect the OperationCanceledException every other async API throws for their token.
		// A Cancelled status with the caller's token not cancelled (the server or HttpClient.Timeout ended the
		// call) is not the caller's cancellation and stays an RpcException.
		static bool IsCallerCancellation(RpcException exception, CancellationToken cancellationToken)
		{
			// Read, not thrown: the exception to throw must carry the RpcException as its inner exception, and an
			// uncancelled token must leave the RpcException as it is.
#pragma warning disable RS0030 // Do not use banned APIs
			return exception.StatusCode == StatusCode.Cancelled && cancellationToken.IsCancellationRequested;
#pragma warning restore RS0030 // Do not use banned APIs
		}

		static OperationCanceledException CallerCancellation(RpcException exception, CancellationToken cancellationToken)
		{
			return new OperationCanceledException(exception.Message, exception, cancellationToken);
		}

		string? ILinqService.RemoteClientTag { get; set; } = "Grpc";

		void IDisposable.Dispose()
		{
			if (_ownsChannel)
				_channel.Dispose();
		}
	}
}
