using System;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.SignalR.Client;

#pragma warning disable CA2007

#pragma warning disable IL2026
#pragma warning disable IL3050

namespace LinqToDB.Remote.Http.Client
{
	/// <summary>
	/// Signal/R-base remote data context client.
	/// </summary>
	public class SignalRLinqServiceClient(Uri requestUri) : ILinqService, IAsyncDisposable
	{
		async Task<HubConnection> GetHubConnectionAsync()
		{
			if (_hubConnection is null)
			{
				_hubConnection = new HubConnectionBuilder()
					.WithUrl(requestUri)
					.WithAutomaticReconnect()
					.Build();

				await _hubConnection.StartAsync();
			}

			return _hubConnection;
		}

		HubConnection? _hubConnection;

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

		async Task<LinqServiceInfo> ILinqService.GetInfoAsync(string? configuration, CancellationToken cancellationToken)
		{
			var hc = await GetHubConnectionAsync();
			return await hc.InvokeAsync<LinqServiceInfo>(nameof(ILinqService.GetInfoAsync), configuration, cancellationToken : cancellationToken);
		}

		async Task<int> ILinqService.ExecuteNonQueryAsync(string? configuration, string queryData, CancellationToken cancellationToken)
		{
			var hc = await GetHubConnectionAsync();
			return await hc.InvokeAsync<int>(nameof(ILinqService.ExecuteNonQueryAsync), configuration, queryData, cancellationToken : cancellationToken);
		}

		async Task<string?> ILinqService.ExecuteScalarAsync(string? configuration, string queryData, CancellationToken cancellationToken)
		{
			var hc = await GetHubConnectionAsync();
			return await hc.InvokeAsync<string?>(nameof(ILinqService.ExecuteScalarAsync), configuration, queryData, cancellationToken : cancellationToken);
		}

		async Task<string> ILinqService.ExecuteReaderAsync(string? configuration, string queryData, CancellationToken cancellationToken)
		{
			var hc = await GetHubConnectionAsync();
			return await hc.InvokeAsync<string>(nameof(ILinqService.ExecuteReaderAsync), configuration, queryData, cancellationToken: cancellationToken);
		}

		async Task<int> ILinqService.ExecuteBatchAsync(string? configuration, string queryData, CancellationToken cancellationToken)
		{
			var hc = await GetHubConnectionAsync();
			return await hc.InvokeAsync<int>(nameof(ILinqService.ExecuteBatchAsync), configuration, queryData, cancellationToken: cancellationToken);
		}

		public async ValueTask DisposeAsync()
		{
			var hc = _hubConnection;

			_hubConnection = null;

			if (hc is not null)
				await hc.DisposeAsync();
		}
	}
}
