using System;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Net.Client;
using LinqToDB.Remote.Grpc.Dto;
using ProtoBuf.Grpc.Client;

namespace LinqToDB.Remote.Grpc
{
	/// <summary>
	/// grpc-base remote data context client.
	/// </summary>
	public class GrpcLinqServiceClient : ILinqService, IDisposable
	{
		private readonly GrpcChannel      _channel;
		private readonly IGrpcLinqService _client;

		public GrpcLinqServiceClient(GrpcChannel channel)
		{
			_channel = channel;
			_client  = channel.CreateGrpcService<IGrpcLinqService>();
		}

		LinqServiceInfo ILinqService.GetInfo(string? configuration)
		{
			return _client.GetInfo(
				new GrpcConfiguration()
				{
					Configuration = configuration
				});
		}

		int ILinqService.ExecuteNonQuery(string? configuration, string queryData)
		{
			return _client.ExecuteNonQuery(
				new GrpcConfigurationQuery()
				{
					Configuration = configuration,
					QueryData     = queryData
				});
		}

		string? ILinqService.ExecuteScalar(string? configuration, string queryData)
		{
			return _client.ExecuteScalar(
				new GrpcConfigurationQuery()
				{
					Configuration = configuration,
					QueryData     = queryData
				});
		}

		string ILinqService.ExecuteReader(string? configuration, string queryData)
		{
			var ret = _client.ExecuteReader(
				new GrpcConfigurationQuery()
				{
					Configuration = configuration,
					QueryData     = queryData
				}).Value;

			return ret ?? throw new LinqToDBException("Return value is not allowed to be null");
		}

		int ILinqService.ExecuteBatch(string? configuration, string queryData)
		{
			return _client.ExecuteBatch(
				new GrpcConfigurationQuery()
				{
					Configuration = configuration,
					QueryData     = queryData
				}).Value;
		}

		Task<LinqServiceInfo> ILinqService.GetInfoAsync(string? configuration, CancellationToken cancellationToken)
		{
			return _client.GetInfoAsync(
				new GrpcConfiguration()
				{
					Configuration = configuration
				}, cancellationToken);
		}

		async Task<int> ILinqService.ExecuteNonQueryAsync(string? configuration, string queryData, CancellationToken cancellationToken)
		{
			return await _client.ExecuteNonQueryAsync(
				new GrpcConfigurationQuery()
				{
					Configuration = configuration,
					QueryData     = queryData
				}, cancellationToken)
				.ConfigureAwait(false);
		}

		async Task<string?> ILinqService.ExecuteScalarAsync(string? configuration, string queryData, CancellationToken cancellationToken)
		{
			return await _client.ExecuteScalarAsync(
				new GrpcConfigurationQuery()
				{
					Configuration = configuration,
					QueryData     = queryData
				}, cancellationToken)
				.ConfigureAwait(false);
		}

		async Task<string> ILinqService.ExecuteReaderAsync(string? configuration, string queryData, CancellationToken cancellationToken)
		{
			var result = await _client.ExecuteReaderAsync(
				new GrpcConfigurationQuery()
				{
					Configuration = configuration,
					QueryData     = queryData
				}, cancellationToken)
				.ConfigureAwait(false);

			return result.Value ?? throw new LinqToDBException("Return value is not allowed to be null");
		}

		async Task<int> ILinqService.ExecuteBatchAsync(string? configuration, string queryData, CancellationToken cancellationToken)
		{
			return await _client.ExecuteBatchAsync(
				new GrpcConfigurationQuery()
				{
					Configuration = configuration,
					QueryData = queryData
				})
				.ConfigureAwait(false);
		}

		string? ILinqService.RemoteClientTag { get; set; } = "Grpc";

		void IDisposable.Dispose()
		{
			_channel.Dispose();
		}
	}
}
