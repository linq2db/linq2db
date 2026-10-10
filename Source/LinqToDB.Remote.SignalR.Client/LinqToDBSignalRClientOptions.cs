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
		/// <exception cref="ArgumentOutOfRangeException">The value is neither positive nor <see cref="Timeout.InfiniteTimeSpan"/>.</exception>
		public TimeSpan ConnectTimeout
		{
			get;
			set
			{
				if (value <= TimeSpan.Zero && value != Timeout.InfiniteTimeSpan)
					throw new ArgumentOutOfRangeException(nameof(value), value, "The value must be positive or Timeout.InfiniteTimeSpan.");

				field = value;
			}
		} = TimeSpan.FromSeconds(30);
	}
}
