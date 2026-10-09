using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.SignalR.Client;

namespace LinqToDB.Remote.SignalR
{
	/// <summary>
	/// A hub connection that linq2db starts on demand: the first query starts it, and a query after the connection
	/// was lost (a failed start, reconnect attempts exhausted, or a dropped connection on .NET Framework /
	/// .NET Standard, whose client does not reconnect) starts it again. Queries that were running when the
	/// connection was lost fail and are never sent again.
	/// <para>
	/// <see cref="ServiceConfigurationExtensions.AddLinqToDBSignalRDataContext{TContext}(Microsoft.Extensions.DependencyInjection.IServiceCollection, Uri, Func{SignalRLinqServiceClient, TContext}, Action{LinqToDBSignalRClientOptions}?)"/>
	/// registers one as a singleton shared by all contexts; use <see cref="SignalRLinqServiceClient(LinqToDBSignalRConnection)"/>
	/// to use one without DI.
	/// </para>
	/// </summary>
	public sealed class LinqToDBSignalRConnection : IAsyncDisposable, IDisposable
	{
		readonly TimeSpan _connectTimeout;
		readonly Lock     _sync = new();

		// The legacy client has neither State nor reconnect events, so the state is tracked here on every target.
		volatile bool              _connected;
		Task?                      _startTask;
		int                        _generation;
		TaskCompletionSource<bool>? _reconnecting;
		bool                       _disposed;

		/// <summary>
		/// Creates a connection to the hub at <paramref name="hubUrl"/>. The connection is not started until it is
		/// first needed.
		/// </summary>
		/// <param name="hubUrl">Hub URL.</param>
		/// <param name="options">Connection options.</param>
		public LinqToDBSignalRConnection(Uri hubUrl, LinqToDBSignalRClientOptions? options = null)
		{
			ArgumentNullException.ThrowIfNull(hubUrl);

			options ??= new();

			_connectTimeout = options.ConnectTimeout;

			var builder = new HubConnectionBuilder();

			if (options.ConfigureHttpConnection != null)
				builder.WithUrl(hubUrl, options.ConfigureHttpConnection);
			else
				builder.WithUrl(hubUrl);

#if NET8_0_OR_GREATER
			builder.WithAutomaticReconnect();
#endif

			options.ConfigureConnection?.Invoke(builder);

			HubConnection = builder.Build();

			HubConnection.Closed += OnClosed;
#if NET8_0_OR_GREATER
			HubConnection.Reconnecting += OnReconnecting;
			HubConnection.Reconnected  += OnReconnected;
#endif
		}

		/// <summary>
		/// The underlying hub connection. Do not start, stop or dispose it directly: the wrapper owns it.
		/// </summary>
		public HubConnection HubConnection { get; }

		/// <summary>
		/// Starts the connection unless it is connected already, or waits for a start or reconnect in progress.
		/// Concurrent callers share one start.
		/// </summary>
		/// <param name="cancellationToken">Cancels the wait of this caller; a shared start goes on for the others.</param>
		/// <exception cref="TimeoutException">The connection was not established within the connect timeout.</exception>
		public Task EnsureConnectedAsync(CancellationToken cancellationToken = default)
		{
			if (_connected)
				return Task.CompletedTask;

			return EnsureConnectedSlowAsync(cancellationToken);
		}

		async Task EnsureConnectedSlowAsync(CancellationToken cancellationToken)
		{
			while (true)
			{
				Task wait;

				lock (_sync)
				{
					ObjectDisposedException.ThrowIf(_disposed, this);

					if (_connected)
						return;

#if NET8_0_OR_GREATER
					// Started or reconnected outside of the wrapper.
					if (HubConnection.State == HubConnectionState.Connected && _reconnecting == null)
					{
						_connected = true;
						return;
					}
#endif

					if (_reconnecting != null)
						wait = _reconnecting.Task;
					else
					{
						// A finished start task without a connection means it failed, or the connection was lost
						// right after it; either way start again.
						if (_startTask is null or { IsCompleted: true })
							_startTask = StartAsync(_generation);

						wait = _startTask;
					}
				}

				await TaskHelper.WaitAsync(wait, cancellationToken, _connectTimeout).ConfigureAwait(false);
			}
		}

		async Task StartAsync(int generation)
		{
			// Run the start outside of the caller's lock.
			await Task.Yield();

			using (var timeout = new CancellationTokenSource(_connectTimeout))
				await HubConnection.StartAsync(timeout.Token).ConfigureAwait(false);

			lock (_sync)
			{
				// A Closed event during the start means this connection is gone already.
				if (generation == _generation)
					_connected = true;
			}
		}

		Task OnClosed(Exception? exception)
		{
			lock (_sync)
			{
				_connected = false;
				_generation++;

				_reconnecting?.TrySetResult(false);
				_reconnecting = null;
			}

			return Task.CompletedTask;
		}

#if NET8_0_OR_GREATER
		Task OnReconnecting(Exception? exception)
		{
			lock (_sync)
			{
				_connected      = false;
				_reconnecting ??= new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
			}

			return Task.CompletedTask;
		}

		Task OnReconnected(string? connectionId)
		{
			lock (_sync)
			{
				_connected = true;

				_reconnecting?.TrySetResult(true);
				_reconnecting = null;
			}

			return Task.CompletedTask;
		}
#endif

		/// <summary>
		/// Stops and disposes the hub connection.
		/// </summary>
		public async ValueTask DisposeAsync()
		{
			lock (_sync)
			{
				if (_disposed)
					return;

				_disposed  = true;
				_connected = false;
			}

			await HubConnection.DisposeAsync().ConfigureAwait(false);
		}

		/// <summary>
		/// Stops and disposes the hub connection. Blocks until the connection is stopped.
		/// </summary>
		public void Dispose()
		{
			// HubConnection has no synchronous disposal; Task.Run keeps the wait off the caller's synchronization
			// context.
			Task.Run(DisposeCoreAsync).GetAwaiter().GetResult();
		}

		// A method rather than a lambda: DisposeAsync returns ValueTask, which Task.Run does not accept.
		async Task DisposeCoreAsync()
		{
			await DisposeAsync().ConfigureAwait(false);
		}
	}
}
