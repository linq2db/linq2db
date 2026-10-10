using System;
using System.Diagnostics;
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

		// The legacy client has neither State nor reconnect events: there the wrapper tracks the state itself, from
		// its own starts and the Closed event. On .NET 8+ the connection's own State is the truth: Signal/R raises
		// Reconnecting/Reconnected/Closed asynchronously, so their order is not to be trusted, and they only wake
		// waiters to look at the state again.
#if !NET8_0_OR_GREATER
		volatile bool              _connected;
#endif
		Task?                      _startTask;
		int                        _generation;
		TaskCompletionSource<bool> _stateChanged = NewStateChanged();
		bool                       _disposed;

		static readonly TimeSpan _statePollInterval = TimeSpan.FromMilliseconds(100);

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
			if (IsConnected)
				return Task.CompletedTask;

			return EnsureConnectedSlowAsync(cancellationToken);
		}

		bool IsConnected
		{
			get
			{
#if NET8_0_OR_GREATER
				return HubConnection.State == HubConnectionState.Connected;
#else
				return _connected;
#endif
			}
		}

		async Task EnsureConnectedSlowAsync(CancellationToken cancellationToken)
		{
			var elapsed = Stopwatch.StartNew();

			while (true)
			{
				Task wait;
				var  starting = true;

				lock (_sync)
				{
					ObjectDisposedException.ThrowIf(_disposed, this);

					if (IsConnected)
						return;

#if NET8_0_OR_GREATER
					// Connecting or reconnecting on the client's own initiative (automatic reconnect, or a start from
					// outside the wrapper): wait for it rather than start.
					if (HubConnection.State != HubConnectionState.Disconnected && _startTask is null or { IsCompleted: true })
					{
						wait     = _stateChanged.Task;
						starting = false;
					}
					else
#endif
					{
						// A finished start task without a connection means it failed, or the connection was lost
						// right after it; either way start again.
						if (_startTask is null or { IsCompleted: true })
							_startTask = StartAsync(_generation);

						wait = _startTask;
					}
				}

				TimeSpan? remaining = null;

				if (_connectTimeout != Timeout.InfiniteTimeSpan)
				{
					remaining = _connectTimeout - elapsed.Elapsed;

					if (remaining <= TimeSpan.Zero)
						throw new TimeoutException($"The Signal/R connection was not established within {_connectTimeout}.");
				}

				if (starting)
				{
					await TaskHelper.WaitAsync(wait, cancellationToken, remaining).ConfigureAwait(false);
				}
				else
				{
					// A state change wakes the wait early; the poll covers a change no event reports.
					var poll = remaining is { } r && r < _statePollInterval ? r : _statePollInterval;

					await Task.WhenAny(wait, Task.Delay(poll, cancellationToken)).ConfigureAwait(false);

					cancellationToken.ThrowIfCancellationRequested();
				}
			}
		}

		async Task StartAsync(int generation)
		{
			// Run the start outside of the caller's lock.
			await Task.Yield();

			try
			{
				using var timeout = new CancellationTokenSource(_connectTimeout);

				try
				{
					await HubConnection.StartAsync(timeout.Token).ConfigureAwait(false);
				}
				catch (OperationCanceledException) when (timeout.IsCancellationRequested)
				{
					// Every caller sharing this start sees the same exception, whatever its own deadline.
					throw new TimeoutException($"The Signal/R connection was not established within {_connectTimeout}.");
				}

#if !NET8_0_OR_GREATER
				lock (_sync)
				{
					// A Closed event during the start means this connection is gone already.
					if (generation == _generation)
						_connected = true;
				}
#endif
			}
#if NET8_0_OR_GREATER
			catch (InvalidOperationException) when (HubConnection.State != HubConnectionState.Disconnected)
			{
				// The client started or began reconnecting on its own between the check and this start; the caller
				// looks at the state again.
			}
#endif
			finally
			{
				PulseStateChanged();
			}
		}

		static TaskCompletionSource<bool> NewStateChanged()
		{
			return new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		}

		void PulseStateChanged()
		{
			TaskCompletionSource<bool> changed;

			lock (_sync)
			{
				changed       = _stateChanged;
				_stateChanged = NewStateChanged();
			}

			changed.TrySetResult(true);
		}

		Task OnClosed(Exception? exception)
		{
			lock (_sync)
			{
#if !NET8_0_OR_GREATER
				_connected = false;
#endif
				_generation++;
			}

			PulseStateChanged();
			return Task.CompletedTask;
		}

#if NET8_0_OR_GREATER
		Task OnReconnecting(Exception? exception)
		{
			PulseStateChanged();
			return Task.CompletedTask;
		}

		Task OnReconnected(string? connectionId)
		{
			PulseStateChanged();
			return Task.CompletedTask;
		}
#endif

		/// <summary>
		/// Stops and disposes the hub connection. On .NET Framework / .NET Standard a start still in progress is not
		/// waited for (that client cannot cancel it): the connection is disposed when the start ends.
		/// </summary>
		public async ValueTask DisposeAsync()
		{
#if !NET8_0_OR_GREATER
			Task? start;
#endif

			lock (_sync)
			{
				if (_disposed)
					return;

				_disposed = true;
#if !NET8_0_OR_GREATER
				start     = _startTask;
#endif
			}

#if !NET8_0_OR_GREATER
			// The legacy client ignores the start's token and holds its connection lock until the start ends (minutes
			// against a server that never answers), and its disposal waits for that lock. Do not wait for a start the
			// callers have given up on: dispose the connection once the start ends.
			if (start is { IsCompleted: false })
			{
				_ = DisposeAfterStartAsync(start);
				return;
			}
#endif

			await HubConnection.DisposeAsync().ConfigureAwait(false);
		}

#if !NET8_0_OR_GREATER
		async Task DisposeAfterStartAsync(Task start)
		{
			try
			{
				await start.ConfigureAwait(false);
			}
			catch
			{
				// The callers waiting for the start have seen its failure.
			}

			try
			{
				await HubConnection.DisposeAsync().ConfigureAwait(false);
			}
			catch
			{
				// Nobody waits for this disposal to report a failure to.
			}
		}
#endif

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
