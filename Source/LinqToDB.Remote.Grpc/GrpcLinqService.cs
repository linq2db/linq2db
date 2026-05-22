using System;
using System.Threading;
using System.Threading.Tasks;

using Grpc.Core;

using LinqToDB.Remote.Grpc.Dto;

using ProtoBuf.Grpc;

namespace LinqToDB.Remote.Grpc
{
	/// <summary>
	/// grpc-based remote data context server implementation.
	/// </summary>
	public class GrpcLinqService : IGrpcLinqService
	{
		private readonly ILinqService _linqService;
		private readonly bool         _transferInternalExceptionToClient;

		/// <summary>
		/// Create instance of grpc-based remote data context server.
		/// </summary>
		/// <param name="linqService">Remote data context server services.</param>
		/// <param name="transferInternalExceptionToClient">when <see langword="true"/>, exception from server will contain exception details; otherwise generic grpc exception will be provided to client.</param>
		/// <exception cref="ArgumentNullException"></exception>
		public GrpcLinqService(ILinqService linqService, bool transferInternalExceptionToClient)
		{
			_linqService = linqService ?? throw new ArgumentNullException(nameof(linqService));
			_transferInternalExceptionToClient = transferInternalExceptionToClient;
		}

		async Task<LinqServiceInfo> IGrpcLinqService.GetInfoAsync(GrpcConfiguration configuration, CallContext context)
		{
			try
			{
				return await _linqService.GetInfoAsync(
					configuration.Configuration,
					context.ServerCallContext?.CancellationToken ?? CancellationToken.None)
					.ConfigureAwait(false);
			}
			catch (Exception exception) when (_transferInternalExceptionToClient)
			{
				throw new RpcException(new Status(StatusCode.Unknown, exception.ToString()));
			}
		}

		async Task<GrpcInt> IGrpcLinqService.ExecuteNonQueryAsync(GrpcConfigurationQuery caq, CallContext context)
		{
			try
			{
				return await _linqService.ExecuteNonQueryAsync(
					caq.Configuration,
					caq.QueryData,
					context.ServerCallContext?.CancellationToken ?? CancellationToken.None
					).ConfigureAwait(false);
			}
			catch (Exception exception) when (_transferInternalExceptionToClient)
			{
				throw new RpcException(new Status(StatusCode.Unknown, exception.ToString()));
			}
		}

		async Task<GrpcString> IGrpcLinqService.ExecuteReaderAsync(GrpcConfigurationQuery caq, CallContext context)
		{
			try
			{
				return await _linqService.ExecuteReaderAsync(
					caq.Configuration,
					caq.QueryData,
					context.ServerCallContext?.CancellationToken ?? CancellationToken.None
					).ConfigureAwait(false);
			}
			catch (Exception exception) when (_transferInternalExceptionToClient)
			{
				throw new RpcException(new Status(StatusCode.Unknown, exception.ToString()));
			}
		}

		async Task<GrpcString> IGrpcLinqService.ExecuteScalarAsync(GrpcConfigurationQuery caq, CallContext context)
		{
			try
			{
				return await _linqService.ExecuteScalarAsync(
					caq.Configuration,
					caq.QueryData,
					context.ServerCallContext?.CancellationToken ?? CancellationToken.None
					).ConfigureAwait(false);
			}
			catch (Exception exception) when (_transferInternalExceptionToClient)
			{
				throw new RpcException(new Status(StatusCode.Unknown, exception.ToString()));
			}
		}

		async Task<GrpcInt> IGrpcLinqService.ExecuteBatchAsync(GrpcConfigurationQuery caq, CallContext context)
		{
			try
			{
				return await _linqService.ExecuteBatchAsync(caq.Configuration, caq.QueryData)
					.ConfigureAwait(false);
			}
			catch (Exception exception) when (_transferInternalExceptionToClient)
			{
				throw new RpcException(new Status(StatusCode.Unknown, exception.ToString()));
			}
		}
	}
}
