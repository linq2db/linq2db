using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.SignalR.Client;

#pragma warning disable CA2007
#pragma warning disable IL2026
#pragma warning disable IL3050

namespace LinqToDB.Remote.Http.Client
{
	public class Container<T>(T @object)
	{
		public T Object { get; } = @object;
	}

	/// <summary>
	/// Signal/R-base remote data context client.
	/// </summary>
	public class SignalRLinqServiceClient : ILinqService, IAsyncDisposable
	{
		HubConnection _hubConnection;

		/// <summary>
		/// Signal/R-base remote data context client.
		/// </summary>
		public SignalRLinqServiceClient(HubConnection hubConnection)
		{
			_hubConnection = hubConnection;
		}

		LinqServiceInfo ILinqService.GetInfo(string? configuration)
		{
			throw new NotImplementedException();
		}

		int ILinqService.ExecuteNonQuery(string? configuration, string queryData)
		{
			throw new NotImplementedException();
		}

		string ILinqService.ExecuteScalar(string? configuration, string queryData)
		{
			throw new NotImplementedException();
		}

		string ILinqService.ExecuteReader(string? configuration, string queryData)
		{
			throw new NotImplementedException();
		}

		int ILinqService.ExecuteBatch(string? configuration, string queryData)
		{
			throw new NotImplementedException();
		}

		Task<LinqServiceInfo> ILinqService.GetInfoAsync(string? configuration, CancellationToken cancellationToken)
		{
			return _hubConnection.InvokeAsync<LinqServiceInfo>(nameof(ILinqService.GetInfoAsync), configuration, cancellationToken : cancellationToken);
		}

		Task<int> ILinqService.ExecuteNonQueryAsync(string? configuration, string queryData, CancellationToken cancellationToken)
		{
			return _hubConnection.InvokeAsync<int>(nameof(ILinqService.ExecuteNonQueryAsync), configuration, queryData, cancellationToken : cancellationToken);
		}

		Task<string?> ILinqService.ExecuteScalarAsync(string? configuration, string queryData, CancellationToken cancellationToken)
		{
			return _hubConnection.InvokeAsync<string?>(nameof(ILinqService.ExecuteScalarAsync), configuration, queryData, cancellationToken : cancellationToken);
		}

		Task<string> ILinqService.ExecuteReaderAsync(string? configuration, string queryData, CancellationToken cancellationToken)
		{
			Console.WriteLine("ExecuteReaderAsync before");
			var r  = _hubConnection.InvokeAsync<string>(nameof(ILinqService.ExecuteReaderAsync), configuration, queryData, cancellationToken: cancellationToken);
			Console.WriteLine("ExecuteReaderAsync after");
			return r;
		}

		Task<int> ILinqService.ExecuteBatchAsync(string? configuration, string queryData, CancellationToken cancellationToken)
		{
			return _hubConnection.InvokeAsync<int>(nameof(ILinqService.ExecuteBatchAsync), configuration, queryData, cancellationToken: cancellationToken);
		}

		public async ValueTask DisposeAsync()
		{
			await Task.CompletedTask;
		}
	}
}
