using Grpc.Core;
using LinqToDB.Remote.Grpc.Dto;
using ProtoBuf.Grpc;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace LinqToDB.Remote.Grpc
{
	public class GrpcLinqService : IGrpcLinqService
	{
		private readonly ILinqService _linqService;
		private readonly bool _transferInternalExceptionToClient;

		public GrpcLinqService(
			ILinqService linqService,
			bool transferInternalExceptionToClient
			)
		{
			if (linqService is null)
			{
				throw new ArgumentNullException(nameof(linqService));
			}

			_linqService = linqService;
			_transferInternalExceptionToClient = transferInternalExceptionToClient;
		}


		public LinqServiceInfo GetInfo(GrpcConfiguration configuration, CallContext context = default)
		{
			try
			{
				var result = _linqService.GetInfo(configuration.Configuration);
				return result;
			}
			catch (Exception exception)
			when (_transferInternalExceptionToClient)
			{
				throw new RpcException(new Status(StatusCode.Unknown, exception.Message));
			}
		}

		public GrpcInt ExecuteBatch(GrpcConfigurationQuery caq, CallContext context = default)
		{
			try
			{
				var result = _linqService.ExecuteBatch(caq.Configuration, caq.QueryData);
				return result;
			}
			catch (Exception exception)
			when (_transferInternalExceptionToClient)
			{
				throw new RpcException(new Status(StatusCode.Unknown, exception.Message));
			}
		}

		public GrpcInt ExecuteNonQuery(GrpcConfigurationQuery caq, CallContext context = default)
		{
			try
			{
				var result = _linqService.ExecuteNonQuery(caq.Configuration, caq.QueryData);
				return result;
			}
			catch (Exception exception)
			when (_transferInternalExceptionToClient)
			{
				throw new RpcException(new Status(StatusCode.Unknown, exception.Message));
			}
		}

		public GrpcString ExecuteReader(GrpcConfigurationQuery caq, CallContext context = default)
		{
			try
			{
				var result = _linqService.ExecuteReader(caq.Configuration, caq.QueryData);
				return result;
			}
			catch (Exception exception)
			when(_transferInternalExceptionToClient)
			{
				throw new RpcException(new Status(StatusCode.Unknown, exception.Message));
			}
		}

		public GrpcString ExecuteScalar(GrpcConfigurationQuery caq, CallContext context = default)
		{
			try
			{
				var result = _linqService.ExecuteScalar(caq.Configuration, caq.QueryData);
				return result;
			}
			catch (Exception exception)
			when (_transferInternalExceptionToClient)
			{
				throw new RpcException(new Status(StatusCode.Unknown, exception.Message));
			}
		}



		//public Task<GrpcLinqServiceInfo> GetInfoAsync(GrpcConfiguration configuration, CallContext context = default)
		//{
		//	//here is nothing to do asynchronously; leave it as is now

		//	var result = _linqService.GetInfo(
		//		configuration.Configuration
		//		);

		//	return Task.FromResult<GrpcLinqServiceInfo>(result);
		//}

		public Task<GrpcInt> ExecuteBatchAsync(GrpcConfigurationQuery caq, CallContext context = default)
		{
			try
			{
				//TODO: modify LinqService to support async
				var result = _linqService.ExecuteBatch(caq.Configuration, caq.QueryData);
				return Task.FromResult<GrpcInt>(result);
			}
			catch (Exception exception)
			when (_transferInternalExceptionToClient)
			{
				throw new RpcException(new Status(StatusCode.Unknown, exception.Message));
			}
		}

		public async Task<GrpcInt> ExecuteNonQueryAsync(GrpcConfigurationQuery caq, CallContext context = default)
		{
			try
			{
				var result = await _linqService.ExecuteNonQueryAsync(
					caq.Configuration,
					caq.QueryData,
					context.ServerCallContext?.CancellationToken ?? CancellationToken.None
					).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);

				return result;
			}
			catch (Exception exception)
			when (_transferInternalExceptionToClient)
			{
				throw new RpcException(new Status(StatusCode.Unknown, exception.Message));
			}
		}

		public async Task<GrpcString> ExecuteReaderAsync(GrpcConfigurationQuery caq, CallContext context = default)
		{
			try
			{
				var result = await _linqService.ExecuteReaderAsync(
					caq.Configuration,
					caq.QueryData,
					context.ServerCallContext?.CancellationToken ?? CancellationToken.None
					).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);

				return result;
			}
			catch (Exception exception)
			when (_transferInternalExceptionToClient)
			{
				throw new RpcException(new Status(StatusCode.Unknown, exception.Message));
			}
		}

		public async Task<GrpcString> ExecuteScalarAsync(GrpcConfigurationQuery caq, CallContext context = default)
		{
			try
			{
				var result = await _linqService.ExecuteScalarAsync(
					caq.Configuration,
					caq.QueryData,
					context.ServerCallContext?.CancellationToken ?? CancellationToken.None
					).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);

				return result;
			}
			catch (Exception exception)
			when (_transferInternalExceptionToClient)
			{
				throw new RpcException(new Status(StatusCode.Unknown, exception.Message));
			}
		}

	}
}
