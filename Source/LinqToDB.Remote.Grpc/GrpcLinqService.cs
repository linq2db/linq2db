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

		public GrpcLinqService(
			ILinqService linqService
			)
		{
			if (linqService is null)
			{
				throw new ArgumentNullException(nameof(linqService));
			}

			_linqService = linqService;
		}


		public GrpcLinqServiceInfo GetInfo(GrpcConfiguration configuration, CallContext context = default)
		{
			var result = _linqService.GetInfo(configuration.Configuration);
			return result;
		}

		public GrpcInt ExecuteBatch(GrpcConfigurationQuery caq, CallContext context = default)
		{
			return _linqService.ExecuteBatch(caq.Configuration, caq.QueryData);
		}

		public GrpcInt ExecuteNonQuery(GrpcConfigurationQuery caq, CallContext context = default)
		{
			return _linqService.ExecuteNonQuery(caq.Configuration, caq.QueryData);
		}

		public GrpcString ExecuteReader(GrpcConfigurationQuery caq, CallContext context = default)
		{
			return _linqService.ExecuteReader(caq.Configuration, caq.QueryData);
		}

		public GrpcString ExecuteScalar(GrpcConfigurationQuery caq, CallContext context = default)
		{
			return _linqService.ExecuteScalar(caq.Configuration, caq.QueryData);
		}



		public Task<GrpcLinqServiceInfo> GetInfoAsync(GrpcConfiguration configuration, CallContext context = default)
		{
			//TODO: modify LinqService to support async
			var result = _linqService.GetInfo(
				configuration.Configuration
				);
			return Task.FromResult<GrpcLinqServiceInfo>(result);
		}

		public Task<GrpcInt> ExecuteBatchAsync(GrpcConfigurationQuery caq, CallContext context = default)
		{
			//TODO: modify LinqService to support async
			var result = _linqService.ExecuteBatch(caq.Configuration, caq.QueryData);
			return Task.FromResult<GrpcInt>(result);
		}

		public Task<GrpcInt> ExecuteNonQueryAsync(GrpcConfigurationQuery caq, CallContext context = default)
		{
			//TODO: modify LinqService to support async
			var result = _linqService.ExecuteNonQuery(caq.Configuration, caq.QueryData);
			return Task.FromResult<GrpcInt>(result);
		}

		public Task<GrpcString> ExecuteReaderAsync(GrpcConfigurationQuery caq, CallContext context = default)
		{
			//TODO: modify LinqService to support async
			var result = _linqService.ExecuteReader(caq.Configuration, caq.QueryData);
			return Task.FromResult<GrpcString>(result);
		}

		public async Task<GrpcString> ExecuteScalarAsync(GrpcConfigurationQuery caq, CallContext context = default)
		{
			var result = await _linqService.ExecuteScalarAsync(
				caq.Configuration,
				caq.QueryData,
				context.ServerCallContext?.CancellationToken ?? CancellationToken.None
				).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);

			return result;
		}

	}
}
