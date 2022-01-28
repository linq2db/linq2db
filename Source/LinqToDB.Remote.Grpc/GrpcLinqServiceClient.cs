using System;
using System.Threading.Tasks;
using Grpc.Net.Client;
using LinqToDB.Remote.Grpc.Dto;
using ProtoBuf.Grpc.Client;

namespace LinqToDB.Remote.Grpc
{
	public class GrpcLinqServiceClient : ILinqClient, IDisposable
	{
		private readonly GrpcChannel _channel;
		private readonly IGrpcLinqService _client;

		#region Init

		public GrpcLinqServiceClient(
			string address
			)
		{
			_channel = GrpcChannel.ForAddress(address);
			_client = _channel.CreateGrpcService<IGrpcLinqService>();
		}

		#endregion

		#region ILinqService Members

		public LinqServiceInfo GetInfo(string? configuration)
		{
			var result = _client.GetInfo(
				new GrpcConfiguration
				{
					Configuration = configuration
				}
				);

			return result;
		}

		public int ExecuteNonQuery(string? configuration, string queryData)
		{
			return _client.ExecuteNonQuery(
				new GrpcConfigurationQuery
				{
					Configuration = configuration,
					QueryData = queryData
				});
		}

		public string? ExecuteScalar(string? configuration, string queryData)
		{
			return _client.ExecuteScalar(
				new GrpcConfigurationQuery
				{
					Configuration = configuration,
					QueryData = queryData
				});

		}

		public string ExecuteReader(string? configuration, string queryData)
		{
			var ret = _client.ExecuteReader(
				new GrpcConfigurationQuery
				{
					Configuration = configuration,
					QueryData = queryData
				}).Value;

			if(ret == null)
			{
				throw new LinqToDBException("Return value is not allowed to be null");
			}

			return ret;
		}

		public int ExecuteBatch(string? configuration, string queryData)
		{
			return _client.ExecuteBatch(
				new GrpcConfigurationQuery
				{
					Configuration = configuration,
					QueryData = queryData
				});
		}



		public async Task<LinqServiceInfo> GetInfoAsync(string? configuration)
		{
			var result = await _client.GetInfoAsync(
				new GrpcConfiguration
				{
					Configuration = configuration
				}).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext); ;

			return result;
		}

		public async Task<int> ExecuteNonQueryAsync(string? configuration, string queryData)
		{
			var result = await _client.ExecuteNonQueryAsync(
				new GrpcConfigurationQuery
				{
					Configuration = configuration,
					QueryData = queryData
				}).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext); ;

			return result;
		}

		public async Task<string?> ExecuteScalarAsync(string? configuration, string queryData)
		{
			var result = await _client.ExecuteScalarAsync(
				new GrpcConfigurationQuery
				{
					Configuration = configuration,
					QueryData = queryData
				}).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);

			var ret = result.Value;

			return ret;
		}

		public async Task<string> ExecuteReaderAsync(string? configuration, string queryData)
		{
			var result = await _client.ExecuteReaderAsync(
				new GrpcConfigurationQuery
				{
					Configuration = configuration,
					QueryData = queryData
				}).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);

			var ret = result.Value;

			if (ret == null)
			{
				throw new LinqToDBException("Return value is not allowed to be null");
			}

			return ret;
		}

		public async Task<int> ExecuteBatchAsync(string? configuration, string queryData)
		{
			var result = await _client.ExecuteBatchAsync(
				new GrpcConfigurationQuery
				{
					Configuration = configuration,
					QueryData = queryData
				}).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);

			return result;
		}

		#endregion

		#region IDisposable Members

		void IDisposable.Dispose()
		{
			_channel.Dispose();
		}

		#endregion

	}
}
