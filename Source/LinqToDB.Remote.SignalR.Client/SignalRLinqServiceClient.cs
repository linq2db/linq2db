using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using Microsoft.AspNetCore.SignalR.Client;

namespace LinqToDB.Remote.SignalR
{
	[SuppressMessage("Design", "MA0048:File name must match type name")]
	sealed class Container<T>(T @object)
	{
		public T Object { get; } = @object;
	}

	/// <summary>
	/// Signal/R-base remote data context client.
	/// <para>
	/// Every call is a Signal/R streaming invocation of <c>LinqToDBHub</c>: cancelling the call's token cancels it on
	/// the server, and the result arrives as one or more stream items. A request larger than the hub accepts
	/// (<c>HubOptions.MaximumReceiveMessageSize</c>, reported by .NET 8+ servers) fails with a
	/// <see cref="LinqToDBException"/> before it is sent, instead of closing the connection.
	/// </para>
	/// </summary>
	public class SignalRLinqServiceClient : ILinqService, IAsyncDisposable
	{
		const string GetInfoMethod         = "GetInfoStream";
		const string ExecuteNonQueryMethod = "ExecuteNonQueryStream";
		const string ExecuteScalarMethod   = "ExecuteScalarStream";
		const string ExecuteReaderMethod   = "ExecuteReaderStream";
		const string ExecuteBatchMethod    = "ExecuteBatchStream";

		readonly HubConnection _hubConnection;

		/// <summary>
		/// Signal/R-base remote data context client over a hub connection the caller starts, stops and disposes.
		/// </summary>
		public SignalRLinqServiceClient(HubConnection hubConnection)
		{
			_hubConnection = hubConnection;
		}

		async Task<LinqServiceInfo> ILinqService.GetInfoAsync(string? configuration, CancellationToken cancellationToken)
		{
			var reader = await _hubConnection.StreamAsChannelAsync<LinqServiceInfo>(GetInfoMethod, configuration, cancellationToken).ConfigureAwait(false);

			return await ReadSingleAsync(reader, GetInfoMethod, cancellationToken).ConfigureAwait(false);
		}

		async Task<int> ILinqService.ExecuteNonQueryAsync(string? configuration, string queryData, CancellationToken cancellationToken)
		{
			var reader = await StartAsync<int>(ExecuteNonQueryMethod, configuration, queryData, cancellationToken).ConfigureAwait(false);

			return await ReadSingleAsync(reader, ExecuteNonQueryMethod, cancellationToken).ConfigureAwait(false);
		}

		async Task<string?> ILinqService.ExecuteScalarAsync(string? configuration, string queryData, CancellationToken cancellationToken)
		{
			var reader = await StartAsync<string>(ExecuteScalarMethod, configuration, queryData, cancellationToken).ConfigureAwait(false);

			return await ReadStringAsync(reader, cancellationToken).ConfigureAwait(false);
		}

		async Task<string> ILinqService.ExecuteReaderAsync(string? configuration, string queryData, CancellationToken cancellationToken)
		{
			var reader = await StartAsync<string>(ExecuteReaderMethod, configuration, queryData, cancellationToken).ConfigureAwait(false);

			return await ReadStringAsync(reader, cancellationToken).ConfigureAwait(false)
				?? throw new LinqToDBException($"The Signal/R hub returned no result for {ExecuteReaderMethod}.");
		}

		async Task<int> ILinqService.ExecuteBatchAsync(string? configuration, string queryData, CancellationToken cancellationToken)
		{
			var reader = await StartAsync<int>(ExecuteBatchMethod, configuration, queryData, cancellationToken).ConfigureAwait(false);

			return await ReadSingleAsync(reader, ExecuteBatchMethod, cancellationToken).ConfigureAwait(false);
		}

		string? ILinqService.RemoteClientTag { get; set; } = "Signal/R";

		async Task<ChannelReader<T>> StartAsync<T>(string methodName, string? configuration, string queryData, CancellationToken cancellationToken)
		{
			await HubConnectionInfo.Get(_hubConnection).CheckRequestSizeAsync(methodName, configuration, queryData, cancellationToken).ConfigureAwait(false);

			return await _hubConnection.StreamAsChannelAsync<T>(methodName, configuration, queryData, cancellationToken).ConfigureAwait(false);
		}

		static async Task<T> ReadSingleAsync<T>(ChannelReader<T> reader, string methodName, CancellationToken cancellationToken)
		{
			var hasResult = false;
			var result    = default(T)!;

			while (await reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
			{
				while (reader.TryRead(out var item))
				{
					result    = item;
					hasResult = true;
				}
			}

			if (!hasResult)
				throw new LinqToDBException($"The Signal/R hub returned no result for {methodName}.");

			return result;
		}

		// Results arrive in chunks; no chunks means null.
		static async Task<string?> ReadStringAsync(ChannelReader<string> reader, CancellationToken cancellationToken)
		{
			string?        first   = null;
			StringBuilder? builder = null;

			while (await reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
			{
				while (reader.TryRead(out var chunk))
				{
					if (first == null)
						first = chunk;
					else
						(builder ??= new StringBuilder(first)).Append(chunk);
				}
			}

			return builder?.ToString() ?? first;
		}

		// Deliberately does nothing: the hub connection is handed in, so it belongs to whoever created it -
		// SignalRDataContext disposes it only when constructed with disposeHubConnection: true. RemoteDataContextBase.OwnsClient
		// is false for SignalRDataContext, so nothing releases this instance per query either.
		public ValueTask DisposeAsync() => default;
	}
}
