using System;
using System.Threading;

using Microsoft.AspNetCore.Http.Connections.Client;
using Microsoft.AspNetCore.SignalR.Client;

namespace LinqToDB.Remote.SignalR
{
	/// <summary>
	/// Options of a <see cref="LinqToDBSignalRConnection"/>.
	/// </summary>
	public sealed class LinqToDBSignalRClientOptions
	{
		/// <summary>
		/// Configures the HTTP connection: access token (<see cref="HttpConnectionOptions.AccessTokenProvider"/>),
		/// headers, transports and so on.
		/// </summary>
		public Action<HttpConnectionOptions>? ConfigureHttpConnection { get; set; }

		/// <summary>
		/// Configures the hub connection builder after the URL is set: hub protocol, logging, reconnect policy and
		/// so on. On .NET 8+ the connection is built with <c>WithAutomaticReconnect()</c>; a reconnect policy set
		/// here replaces it.
		/// </summary>
		public Action<IHubConnectionBuilder>? ConfigureConnection { get; set; }

		/// <summary>
		/// How long a query waits for the connection to start or to reconnect before it fails with a
		/// <see cref="TimeoutException"/>. The default is 30 seconds; <see cref="Timeout.InfiniteTimeSpan"/> waits
		/// without a limit.
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">The value is neither <see cref="Timeout.InfiniteTimeSpan"/> nor positive and at most <see cref="int.MaxValue"/> milliseconds.</exception>
		public TimeSpan ConnectTimeout
		{
			get;
			set
			{
				// The timers that enforce it (CancellationTokenSource, Task.WaitAsync) reject longer delays; int.MaxValue
				// milliseconds is the .NET Framework limit and the smaller one.
				if (value != Timeout.InfiniteTimeSpan && (value <= TimeSpan.Zero || value.TotalMilliseconds > int.MaxValue))
					throw new ArgumentOutOfRangeException(nameof(value), value, "The value must be positive and at most Int32.MaxValue milliseconds, or Timeout.InfiniteTimeSpan.");

				field = value;
			}
		} = TimeSpan.FromSeconds(30);
	}
}
