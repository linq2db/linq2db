using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.SignalR.Client;

namespace LinqToDB.Remote.SignalR
{
	sealed class Container<T>(T @object)
	{
		public T Object { get; } = @object;
	}

	/// <summary>
	/// Signal/R-base remote data context client.
	/// </summary>
	public class SignalRLinqServiceClient : ILinqService, IAsyncDisposable
	{
		readonly HubConnection _hubConnection;

		/// <summary>
		/// Signal/R-base remote data context client.
		/// </summary>
		public SignalRLinqServiceClient(HubConnection hubConnection)
		{
			_hubConnection = hubConnection;
		}

		LinqServiceInfo ILinqService.GetInfo(string? configuration)
		{
			return _hubConnection.InvokeAsync<LinqServiceInfo>(nameof(ILinqService.GetInfoAsync), configuration, cancellationToken : default).Result;
		}

		int ILinqService.ExecuteNonQuery(string? configuration, string queryData)
		{
			return _hubConnection.InvokeAsync<int>(nameof(ILinqService.ExecuteNonQueryAsync), configuration, queryData, cancellationToken : default).Result;
		}

		string? ILinqService.ExecuteScalar(string? configuration, string queryData)
		{
			return _hubConnection.InvokeAsync<string?>(nameof(ILinqService.ExecuteScalarAsync), configuration, queryData, cancellationToken : default).Result;
		}

		string ILinqService.ExecuteReader(string? configuration, string queryData)
		{
			return _hubConnection.InvokeAsync<string>(nameof(ILinqService.ExecuteReaderAsync), configuration, queryData, cancellationToken: default).Result;
		}

		int ILinqService.ExecuteBatch(string? configuration, string queryData)
		{
			return _hubConnection.InvokeAsync<int>(nameof(ILinqService.ExecuteBatchAsync), configuration, queryData, cancellationToken: default).Result;
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
			return _hubConnection.InvokeAsync<string>(nameof(ILinqService.ExecuteReaderAsync), configuration, queryData, cancellationToken: cancellationToken);
		}

		Task<int> ILinqService.ExecuteBatchAsync(string? configuration, string queryData, CancellationToken cancellationToken)
		{
			return _hubConnection.InvokeAsync<int>(nameof(ILinqService.ExecuteBatchAsync), configuration, queryData, cancellationToken: cancellationToken);
		}

		string? ILinqService.RemoteClientTag { get; set; } = "Signal/R";

		public ValueTask DisposeAsync() => default;
	}
}
