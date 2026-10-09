using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.SignalR.Client;

namespace LinqToDB.Remote.SignalR
{
	/// <summary>
	/// What the client knows about the server behind one <see cref="HubConnection"/>: the largest message the hub
	/// accepts. Fetched once per connection and forgotten when the connection closes or reconnects, since the
	/// server behind it may have changed.
	/// </summary>
	sealed class HubConnectionInfo
	{
		const string GetMaximumReceiveMessageSizeMethod = "GetMaximumReceiveMessageSize";

		// Ephemeron table: an entry lives exactly as long as its connection.
		static readonly ConditionalWeakTable<HubConnection,HubConnectionInfo> _infos = new();

		readonly HubConnection _hubConnection;
		readonly Lock          _sync = new();

		Task<long>? _maximumReceiveMessageSize;

		HubConnectionInfo(HubConnection hubConnection)
		{
			_hubConnection = hubConnection;

			_hubConnection.Closed += OnClosed;
#if NET8_0_OR_GREATER
			_hubConnection.Reconnected += OnReconnected;
#endif
		}

		public static HubConnectionInfo Get(HubConnection hubConnection)
		{
			return _infos.GetValue(hubConnection, static c => new HubConnectionInfo(c));
		}

		Task OnClosed(Exception? exception)
		{
			Reset();
			return Task.CompletedTask;
		}

#if NET8_0_OR_GREATER
		Task OnReconnected(string? connectionId)
		{
			Reset();
			return Task.CompletedTask;
		}
#endif

		void Reset()
		{
			lock (_sync)
				_maximumReceiveMessageSize = null;
		}

		/// <summary>
		/// Throws when a request is certainly larger than the hub accepts. Signal/R would close the whole connection
		/// on such a message, failing every other call that shares it; refusing it here fails only this call.
		/// </summary>
		public async Task CheckRequestSizeAsync(string methodName, string? configuration, string queryData, CancellationToken cancellationToken)
		{
			var limit = await TaskHelper.WaitAsync(GetMaximumReceiveMessageSizeAsync(), cancellationToken).ConfigureAwait(false);

			if (limit < 0)
				return;

			var size = RequestSize.Estimate(methodName, configuration, queryData, limit);

			if (size > limit)
				throw new LinqToDBException(string.Create(CultureInfo.InvariantCulture,
					$"The Signal/R request of about {size} bytes exceeds the maximum message size of {limit} bytes the hub accepts, so it was not sent (the server would close the connection). Raise HubOptions.MaximumReceiveMessageSize for the linq2db hub on the server (AddHubOptions<THub>)."));
		}

		// Shared by concurrent callers and not bound to any caller's token, so one cancelled caller does not fail
		// the others. A failed fetch is not kept: the next call tries again.
		Task<long> GetMaximumReceiveMessageSizeAsync()
		{
			lock (_sync)
			{
				if (_maximumReceiveMessageSize is not { IsFaulted: false, IsCanceled: false } task)
					_maximumReceiveMessageSize = task = _hubConnection.InvokeAsync<long>(GetMaximumReceiveMessageSizeMethod);

				return task;
			}
		}
	}
}
