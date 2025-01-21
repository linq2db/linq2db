using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.SignalR;

namespace LinqToDB.Remote.Http.Server
{
	public class LinqToDBHub() : Hub
	{
		readonly LinqService _linqService = new () { AllowUpdates = true, RemoteClientTag = "Signal/R" };

		public virtual LinqServiceInfo GetInfo(string? configuration)
		{
			return _linqService.GetInfo(configuration);
		}

		public virtual int ExecuteNonQuery(string? configuration, string queryData)
		{
			return _linqService.ExecuteNonQuery(configuration, queryData);
		}

		public virtual string? ExecuteScalar(string? configuration, string queryData)
		{
			return _linqService.ExecuteScalar(configuration, queryData);
		}

		public virtual string ExecuteReader(string? configuration, string queryData)
		{
			return _linqService.ExecuteReader(configuration, queryData);
		}

		public virtual int ExecuteBatch(string? configuration, string queryData)
		{
			return _linqService.ExecuteBatch(configuration, queryData);
		}

		public virtual Task<LinqServiceInfo> GetInfoAsync(string? configuration)
		{
			return _linqService.GetInfoAsync(configuration, default);
		}

		public virtual Task<int> ExecuteNonQueryAsync(string? configuration, string queryData, CancellationToken cancellationToken)
		{
			return _linqService.ExecuteNonQueryAsync(configuration, queryData, cancellationToken);
		}

		public virtual Task<string?> ExecuteScalarAsync(string? configuration, string queryData, CancellationToken cancellationToken)
		{
			return _linqService.ExecuteScalarAsync(configuration, queryData, cancellationToken);
		}

		public virtual Task<string> ExecuteReaderAsync(string? configuration, string queryData)
		{
			return _linqService.ExecuteReaderAsync(configuration, queryData, default);
		}

		public virtual Task<int> ExecuteBatchAsync(string? configuration, string queryData, CancellationToken cancellationToken)
		{
			return _linqService.ExecuteBatchAsync(configuration, queryData, cancellationToken);
		}
	}
}
