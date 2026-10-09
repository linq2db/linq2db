using System;
using System.Threading;

namespace LinqToDB.Remote.SignalR
{
	/// <summary>
	/// Options of <see cref="LinqToDBHub"/>. Register them with
	/// <c>services.Configure&lt;LinqToDBHubOptions&gt;(o => ...)</c>; the hub receives them through its constructor
	/// that takes <c>IOptions&lt;LinqToDBHubOptions&gt;</c>.
	/// </summary>
	public sealed class LinqToDBHubOptions
	{
		readonly Lock _sync = new();

		SemaphoreSlim? _globalLimiter;
		bool           _globalLimiterCreated;

		/// <summary>
		/// Maximum number of remote calls one client connection runs on the server at the same time; further calls
		/// on that connection wait for a free slot.
		/// <para>
		/// <see langword="null"/> (the default) keeps the behaviour of the hub's
		/// <c>HubOptions.MaximumParallelInvocationsPerClient</c> (1 unless configured) on .NET 8+ servers,
		/// and 1 on the legacy (.NET Framework / .NET Standard) server. A client application usually shares one
		/// connection between all its contexts, so a value such as 8 lets their queries run in parallel.
		/// </para>
		/// </summary>
		public int? MaxConcurrentCallsPerConnection
		{
			get;
			set
			{
				if (value <= 0)
					throw new ArgumentOutOfRangeException(nameof(value), value, "The value must be positive or null.");

				field = value;
			}
		}

		/// <summary>
		/// Maximum number of remote calls the hub runs at the same time across all connections; further calls wait
		/// for a free slot. <see langword="null"/> (the default) sets no global limit.
		/// The value is read once, when the first call needs it.
		/// </summary>
		public int? MaxConcurrentCalls
		{
			get;
			set
			{
				if (value <= 0)
					throw new ArgumentOutOfRangeException(nameof(value), value, "The value must be positive or null.");

				field = value;
			}
		}

		/// <summary>
		/// When <see langword="true"/>, a server-side error is sent to the client with its full text
		/// (<see cref="Exception.ToString"/>), like the <c>transferInternalExceptionToClient</c> option of the gRPC
		/// service; otherwise the client receives the generic Signal/R error message.
		/// <para>
		/// Only .NET 8+ servers pass the text on. The legacy (.NET Framework / .NET Standard) Signal/R server drops
		/// it unless <c>HubOptions.EnableDetailedErrors</c> is set, which exposes the errors of every hub method.
		/// </para>
		/// </summary>
		public bool TransferInternalExceptionToClient { get; set; }

		internal SemaphoreSlim? GetGlobalLimiter()
		{
			if (Volatile.Read(ref _globalLimiterCreated))
				return _globalLimiter;

			lock (_sync)
			{
				if (!_globalLimiterCreated)
				{
					// Never disposed: without AvailableWaitHandle a SemaphoreSlim holds no unmanaged resources, and
					// an operation finishing late may still release it.
					_globalLimiter = MaxConcurrentCalls is { } max ? new SemaphoreSlim(max, max) : null;
					Volatile.Write(ref _globalLimiterCreated, true);
				}

				return _globalLimiter;
			}
		}
	}
}
