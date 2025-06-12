using System;
using System.Threading.Tasks;

using Microsoft.AspNetCore.SignalR;

namespace LinqToDB.Remote.SignalR
{
	public class LinqToDBHub : Hub
	{
		public virtual LinqServiceInfo GetInfo(string? configuration)
		{
			return LinqService.GetInfo(configuration);
		}

		public virtual int ExecuteNonQuery(string? configuration, string queryData)
		{
			return LinqService.ExecuteNonQuery(configuration, queryData);
		}

		public virtual string? ExecuteScalar(string? configuration, string queryData)
		{
			return LinqService.ExecuteScalar(configuration, queryData);
		}

		public virtual string ExecuteReader(string? configuration, string queryData)
		{
			return LinqService.ExecuteReader(configuration, queryData);
		}

		public virtual int ExecuteBatch(string? configuration, string queryData)
		{
			return LinqService.ExecuteBatch(configuration, queryData);
		}

		public virtual Task<LinqServiceInfo> GetInfoAsync(string? configuration)
		{
			return LinqService.GetInfoAsync(configuration, default);
		}

		public virtual Task<int> ExecuteNonQueryAsync(string? configuration, string queryData)
		{
			return LinqService.ExecuteNonQueryAsync(configuration, queryData, default);
		}

		public virtual Task<string?> ExecuteScalarAsync(string? configuration, string queryData)
		{
			return LinqService.ExecuteScalarAsync(configuration, queryData, default);
		}

		public virtual Task<string> ExecuteReaderAsync(string? configuration, string queryData)
		{
			return LinqService.ExecuteReaderAsync(configuration, queryData, default);
		}

		public virtual Task<int> ExecuteBatchAsync(string? configuration, string queryData)
		{
			return LinqService.ExecuteBatchAsync(configuration, queryData, default);
		}

		ILinqService? _linqService;

		protected virtual ILinqService LinqService => _linqService ??= CreateLinqService();

		protected virtual ILinqService CreateLinqService()
		{
			return new LinqService { AllowUpdates = false, RemoteClientTag = "Signal/R" };
		}
	}
}
