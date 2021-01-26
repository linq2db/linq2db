#if NET472
using System;
using System.Threading.Tasks;

using LinqToDB.ServiceModel;

namespace Tests.ServiceModel
{
	class TestLinqServiceClient : ILinqClient
	{
#region Init

		public TestLinqServiceClient(LinqService linqService)
		{
			_linqService = linqService;
		}

		readonly LinqService _linqService;

#endregion

#region ILinqService Members

		public LinqServiceInfo GetInfo(string? configuration)
		{
			return _linqService.GetInfo(configuration);
		}

		public int ExecuteNonQuery(string? configuration, string queryData)
		{
			return _linqService.ExecuteNonQuery(configuration, queryData);
		}

		public object? ExecuteScalar(string? configuration, string queryData)
		{
			return _linqService.ExecuteScalar(configuration, queryData);
		}

		public string ExecuteReader(string? configuration, string queryData)
		{
			return _linqService.ExecuteReader(configuration, queryData);
		}

		public int ExecuteBatch(string? configuration, string queryData)
		{
			return _linqService.ExecuteBatch(configuration, queryData);
		}

		public Task<LinqServiceInfo> GetInfoAsync(string? configuration)
		{
			return Task.Run(() => _linqService.GetInfo(configuration));
		}

		public Task<int> ExecuteNonQueryAsync(string? configuration, string queryData)
		{
			return Task.Run(() => _linqService.ExecuteNonQuery(configuration, queryData));
		}

		public Task<object?> ExecuteScalarAsync(string? configuration, string queryData)
		{
			return Task.Run(() => _linqService.ExecuteScalar(configuration, queryData));
		}

		public Task<string> ExecuteReaderAsync(string? configuration, string queryData)
		{
			return Task.Run(() => _linqService.ExecuteReader(configuration, queryData));
		}

		public Task<int> ExecuteBatchAsync(string? configuration, string queryData)
		{
			return Task.Run(() => _linqService.ExecuteBatch(configuration, queryData));
		}

#endregion
	}
}
#endif
