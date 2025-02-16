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
			Console.WriteLine("GetInfoAsync before");
			var result = LinqService.GetInfoAsync(configuration, default);
			Console.WriteLine("GetInfoAsync after");
			return result;
		}

		public virtual Task<int> ExecuteNonQueryAsync(string? configuration, string queryData)
		{
			Console.WriteLine("ExecuteNonQueryAsync before");
			var result = LinqService.ExecuteNonQueryAsync(configuration, queryData, default);
			Console.WriteLine("ExecuteNonQueryAsync after");
			return result;
		}

		public virtual Task<string?> ExecuteScalarAsync(string? configuration, string queryData)
		{
			Console.WriteLine("ExecuteScalarAsync before");
			var result = LinqService.ExecuteScalarAsync(configuration, queryData, default);
			Console.WriteLine("ExecuteScalarAsync after");
			return result;
		}

		public virtual Task<string> ExecuteReaderAsync(string? configuration, string queryData)
		{
			Console.WriteLine("ExecuteReaderAsync before");
			var result = LinqService.ExecuteReaderAsync(configuration, queryData, default);
			Console.WriteLine("ExecuteReaderAsync after");
			return result;
		}

		public virtual Task<int> ExecuteBatchAsync(string? configuration, string queryData)
		{
			Console.WriteLine("ExecuteBatchAsync before");
			var result = LinqService.ExecuteBatchAsync(configuration, queryData, default);
			Console.WriteLine("ExecuteBatchAsync after");
			return result;
		}

		ILinqService? _linqService;

		protected virtual ILinqService LinqService => _linqService ??= CreateLinqService();

		protected virtual ILinqService CreateLinqService()
		{
			return new LinqService { AllowUpdates = false, RemoteClientTag = "Signal/R" };
		}
	}
}
