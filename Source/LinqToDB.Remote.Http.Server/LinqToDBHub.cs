using System;
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
			Console.WriteLine("GetInfoAsync before");
			var result = _linqService.GetInfoAsync(configuration, default);
			Console.WriteLine("GetInfoAsync after");
			return result;
		}

		public virtual Task<int> ExecuteNonQueryAsync(string? configuration, string queryData)
		{
			Console.WriteLine("ExecuteNonQueryAsync before");
			var result = _linqService.ExecuteNonQueryAsync(configuration, queryData, default);
			Console.WriteLine("ExecuteNonQueryAsync after");
			return result;
		}

		public virtual Task<string?> ExecuteScalarAsync(string? configuration, string queryData)
		{
			Console.WriteLine("ExecuteScalarAsync before");
			var result = _linqService.ExecuteScalarAsync(configuration, queryData, default);
			Console.WriteLine("ExecuteScalarAsync after");
			return result;
		}

		public virtual Task<string> ExecuteReaderAsync(string? configuration, string queryData)
		{
			Console.WriteLine("ExecuteReaderAsync before");
			var result = _linqService.ExecuteReaderAsync(configuration, queryData, default);
			Console.WriteLine("ExecuteReaderAsync after");
			return result;
		}

		public virtual Task<int> ExecuteBatchAsync(string? configuration, string queryData)
		{
			Console.WriteLine("ExecuteBatchAsync before");
			var result = _linqService.ExecuteBatchAsync(configuration, queryData, default);
			Console.WriteLine("ExecuteBatchAsync after");
			return result;
		}
	}
}
