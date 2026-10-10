using System;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using Microsoft.AspNetCore.SignalR.Client;

namespace LinqToDB.Remote.SignalR
{
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

		readonly HubConnection              _hubConnection;
		readonly LinqToDBSignalRConnection? _connection;

		/// <summary>
		/// Signal/R-base remote data context client over a hub connection the caller starts, stops and disposes.
		/// </summary>
		public SignalRLinqServiceClient(HubConnection hubConnection)
		{
			_hubConnection = hubConnection;
		}

		/// <summary>
		/// Signal/R-base remote data context client over a <see cref="LinqToDBSignalRConnection"/>, which is started
		/// (or started again) before each call when it is not connected.
		/// </summary>
		public SignalRLinqServiceClient(LinqToDBSignalRConnection connection)
		{
			ArgumentNullException.ThrowIfNull(connection);

			_connection    = connection;
			_hubConnection = connection.HubConnection;
		}

		Task<LinqServiceInfo> ILinqService.GetInfoAsync(string? configuration, CancellationToken cancellationToken)
		{
			return StreamAsync<LinqServiceInfo,LinqServiceInfo>(GetInfoMethod, configuration, null,
				static (reader, token) => ReadSingleAsync(reader, GetInfoMethod, token),
				cancellationToken);
		}

		Task<int> ILinqService.ExecuteNonQueryAsync(string? configuration, string queryData, CancellationToken cancellationToken)
		{
			return StreamAsync<int,int>(ExecuteNonQueryMethod, configuration, queryData,
				static (reader, token) => ReadSingleAsync(reader, ExecuteNonQueryMethod, token),
				cancellationToken);
		}

		Task<string?> ILinqService.ExecuteScalarAsync(string? configuration, string queryData, CancellationToken cancellationToken)
		{
			return StreamAsync<string,string?>(ExecuteScalarMethod, configuration, queryData,
				static (reader, token) => ReadStringAsync(reader, token),
				cancellationToken);
		}

		async Task<string> ILinqService.ExecuteReaderAsync(string? configuration, string queryData, CancellationToken cancellationToken)
		{
			return await StreamAsync<string,string?>(ExecuteReaderMethod, configuration, queryData,
				static (reader, token) => ReadStringAsync(reader, token),
				cancellationToken).ConfigureAwait(false)
				?? throw new LinqToDBException($"The Signal/R hub returned no result for {ExecuteReaderMethod}.");
		}

		Task<int> ILinqService.ExecuteBatchAsync(string? configuration, string queryData, CancellationToken cancellationToken)
		{
			return StreamAsync<int,int>(ExecuteBatchMethod, configuration, queryData,
				static (reader, token) => ReadSingleAsync(reader, ExecuteBatchMethod, token),
				cancellationToken);
		}

		string? ILinqService.RemoteClientTag { get; set; } = "Signal/R";

		// Signal/R registers a callback on the token of a stream and never removes it, so with the caller's token every
		// finished call would stay on it, and cancelling a long-lived token later would send a cancellation for each
		// of them. A token of the call's own, released when the call ends, leaves nothing behind.
		async Task<TResult> StreamAsync<T,TResult>(
			string                                                 methodName,
			string?                                                configuration,
			string?                                                queryData,
			Func<ChannelReader<T>,CancellationToken,Task<TResult>> read,
			CancellationToken                                      cancellationToken)
		{
			if (!cancellationToken.CanBeCanceled)
			{
				var reader = await StartAsync<T>(methodName, configuration, queryData, cancellationToken).ConfigureAwait(false);

				return await read(reader, cancellationToken).ConfigureAwait(false);
			}

			using var call = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

			var callReader = await StartAsync<T>(methodName, configuration, queryData, call.Token).ConfigureAwait(false);

			return await read(callReader, call.Token).ConfigureAwait(false);
		}

		async Task<ChannelReader<T>> StartAsync<T>(string methodName, string? configuration, string? queryData, CancellationToken cancellationToken)
		{
			if (_connection != null)
				await _connection.EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);

			return await SendAsync<T>(methodName, configuration, queryData, cancellationToken).ConfigureAwait(false);
		}

		// queryData is null for GetInfoStream, which takes the configuration only.
		async Task<ChannelReader<T>> SendAsync<T>(string methodName, string? configuration, string? queryData, CancellationToken cancellationToken)
		{
			if (queryData == null)
				return await _hubConnection.StreamAsChannelAsync<T>(methodName, configuration, cancellationToken).ConfigureAwait(false);

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
		// SignalRDataContext disposes it only when constructed with disposeHubConnection: true, and a
		// LinqToDBSignalRConnection is disposed by its owner (the DI container). RemoteDataContextBase.OwnsClient
		// is false for SignalRDataContext, so nothing releases this instance per query either.
		public ValueTask DisposeAsync() => default;
	}
}
