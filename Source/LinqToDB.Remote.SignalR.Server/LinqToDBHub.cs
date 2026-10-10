using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

#if NET8_0_OR_GREATER
using Microsoft.AspNetCore.Http;
#else
using System.Reflection;
#endif

namespace LinqToDB.Remote.SignalR
{
	/// <summary>
	/// Signal/R hub that serves linq2db remote data contexts (<c>SignalRDataContext</c>).
	/// <para>
	/// Every remote call is a Signal/R streaming invocation, so cancelling the call on the client, or losing the
	/// connection, cancels it on the server and in the database. Results are sent as one or more stream items.
	/// </para>
	/// <para>
	/// To validate or authorize calls, decorate the <see cref="ILinqService"/> the hub uses
	/// (<see cref="CreateLinqService"/>, or <c>ILinqService&lt;T&gt;</c> registered in DI for
	/// <see cref="LinqToDBHub{T}"/>): the decorator sees every call together with its cancellation token.
	/// Class-level <c>[Authorize]</c> on a derived hub applies to all hub methods.
	/// </para>
	/// </summary>
	public class LinqToDBHub : Hub
	{
		/// <summary>
		/// Maximum length of one result stream item, in characters. Larger results are split into several items.
		/// </summary>
		internal const int ResultChunkSize = 32 * 1024;

		static readonly LinqToDBHubOptions _defaultOptions     = new();
		static readonly object             _connectionStateKey = new();

		// MaxConcurrentCalls: one semaphore per hub type and limit for the whole process, since Signal/R creates a hub
		// for every call and a hub may create its options itself. Never disposed: without AvailableWaitHandle a
		// SemaphoreSlim holds no unmanaged resources, and an operation finishing late may still release it.
		static readonly ConcurrentDictionary<(Type HubType, int MaxConcurrentCalls),SemaphoreSlim> _globalLimiters = new();

		readonly LinqToDBHubOptions? _options;

		/// <summary>
		/// Creates a hub that uses the <see cref="LinqToDBHubOptions"/> registered in the services
		/// (<c>services.Configure&lt;LinqToDBHubOptions&gt;</c>), or the default options when none are registered.
		/// <para>
		/// The legacy (.NET Framework / .NET Standard) server gives connections of the long polling transport no request
		/// services, so there such a hub uses the default options: a hub that needs its options on every transport takes
		/// <c>IOptions&lt;LinqToDBHubOptions&gt;</c> in its constructor.
		/// </para>
		/// </summary>
		public LinqToDBHub()
		{
		}

		/// <summary>
		/// Creates a hub with the given options.
		/// </summary>
		/// <param name="options">Hub options.</param>
		[ActivatorUtilitiesConstructor]
		public LinqToDBHub(IOptions<LinqToDBHubOptions> options)
		{
			ArgumentNullException.ThrowIfNull(options);

			_options = options.Value;
		}

		/// <summary>
		/// Returns the server's configuration information. Streams one item.
		/// </summary>
		public ChannelReader<LinqServiceInfo> GetInfoStream(string? configuration)
		{
			return StartOperation<LinqServiceInfo>(async (service, cancellationToken) =>
			{
				var info = await service.GetInfoAsync(configuration, cancellationToken).ConfigureAwait(false);
				return [info];
			});
		}

		/// <summary>
		/// Executes a non-query command. Streams one item: the number of affected rows.
		/// </summary>
		public ChannelReader<int> ExecuteNonQueryStream(string? configuration, string queryData)
		{
			return StartOperation<int>(async (service, cancellationToken) =>
			{
				var result = await service.ExecuteNonQueryAsync(configuration, queryData, cancellationToken).ConfigureAwait(false);
				return [result];
			});
		}

		/// <summary>
		/// Executes a scalar query. Streams the serialized result in chunks; no items means <see langword="null"/>.
		/// </summary>
		public ChannelReader<string> ExecuteScalarStream(string? configuration, string queryData)
		{
			return StartOperation<string>(async (service, cancellationToken) =>
			{
				var result = await service.ExecuteScalarAsync(configuration, queryData, cancellationToken).ConfigureAwait(false);
				return result == null ? [] : SplitResult(result);
			});
		}

		/// <summary>
		/// Executes a query. Streams the serialized result in chunks.
		/// </summary>
		public ChannelReader<string> ExecuteReaderStream(string? configuration, string queryData)
		{
			return StartOperation<string>(async (service, cancellationToken) =>
			{
				var result = await service.ExecuteReaderAsync(configuration, queryData, cancellationToken).ConfigureAwait(false);
				return SplitResult(result);
			});
		}

		/// <summary>
		/// Executes a batch of commands in a transaction. Streams one item.
		/// </summary>
		public ChannelReader<int> ExecuteBatchStream(string? configuration, string queryData)
		{
			return StartOperation<int>(async (service, cancellationToken) =>
			{
				var result = await service.ExecuteBatchAsync(configuration, queryData, cancellationToken).ConfigureAwait(false);
				return [result];
			});
		}

		/// <summary>
		/// Returns the server's configuration information to a client older than 6.6.0, which calls it as a plain
		/// invocation. Such a call honours the hub's concurrency limits, and only a lost connection cancels it.
		/// </summary>
		[Obsolete("Serves clients older than 6.6.0, which call it as a plain invocation; 6.6.0 and later clients call GetInfoStream. API will be removed in version 7"), EditorBrowsable(EditorBrowsableState.Never)]
		public virtual Task<LinqServiceInfo> GetInfoAsync(string? configuration)
		{
			return InvokeOperation((service, cancellationToken) => service.GetInfoAsync(configuration, cancellationToken));
		}

		/// <summary>
		/// Executes a non-query command for a client older than 6.6.0 (see <see cref="GetInfoAsync"/>).
		/// </summary>
		[Obsolete("Serves clients older than 6.6.0, which call it as a plain invocation; 6.6.0 and later clients call ExecuteNonQueryStream. API will be removed in version 7"), EditorBrowsable(EditorBrowsableState.Never)]
		public virtual Task<int> ExecuteNonQueryAsync(string? configuration, string queryData)
		{
			return InvokeOperation((service, cancellationToken) => service.ExecuteNonQueryAsync(configuration, queryData, cancellationToken));
		}

		/// <summary>
		/// Executes a scalar query for a client older than 6.6.0 (see <see cref="GetInfoAsync"/>).
		/// </summary>
		[Obsolete("Serves clients older than 6.6.0, which call it as a plain invocation; 6.6.0 and later clients call ExecuteScalarStream. API will be removed in version 7"), EditorBrowsable(EditorBrowsableState.Never)]
		public virtual Task<string?> ExecuteScalarAsync(string? configuration, string queryData)
		{
			return InvokeOperation((service, cancellationToken) => service.ExecuteScalarAsync(configuration, queryData, cancellationToken));
		}

		/// <summary>
		/// Executes a query for a client older than 6.6.0 (see <see cref="GetInfoAsync"/>). The result is sent as one
		/// message.
		/// </summary>
		[Obsolete("Serves clients older than 6.6.0, which call it as a plain invocation; 6.6.0 and later clients call ExecuteReaderStream. API will be removed in version 7"), EditorBrowsable(EditorBrowsableState.Never)]
		public virtual Task<string> ExecuteReaderAsync(string? configuration, string queryData)
		{
			return InvokeOperation((service, cancellationToken) => service.ExecuteReaderAsync(configuration, queryData, cancellationToken));
		}

		/// <summary>
		/// Executes a batch of commands in a transaction for a client older than 6.6.0 (see <see cref="GetInfoAsync"/>).
		/// </summary>
		[Obsolete("Serves clients older than 6.6.0, which call it as a plain invocation; 6.6.0 and later clients call ExecuteBatchStream. API will be removed in version 7"), EditorBrowsable(EditorBrowsableState.Never)]
		public virtual Task<int> ExecuteBatchAsync(string? configuration, string queryData)
		{
			return InvokeOperation((service, cancellationToken) => service.ExecuteBatchAsync(configuration, queryData, cancellationToken));
		}

		/// <summary>
		/// Returns the largest message, in bytes, this hub accepts from a client, or -1 when the server sets no limit
		/// or cannot tell (the legacy .NET Framework / .NET Standard server). The client checks its requests against
		/// it before sending: Signal/R closes the whole connection on an oversized message.
		/// </summary>
		public long GetMaximumReceiveMessageSize()
		{
			return GetConnectionState().MaximumReceiveMessageSize;
		}

		ILinqService? _linqService;

		/// <summary>
		/// The service that executes remote calls. By default it is created once per hub instance by
		/// <see cref="CreateLinqService"/>.
		/// </summary>
		protected virtual ILinqService LinqService => _linqService ??= CreateLinqService();

		/// <summary>
		/// Creates the service that executes remote calls. Override it to use a configured
		/// <see cref="Remote.LinqService"/>, or a decorator that validates or authorizes calls.
		/// </summary>
		protected virtual ILinqService CreateLinqService()
		{
			return new LinqService { AllowUpdates = false, RemoteClientTag = "Signal/R" };
		}

		ChannelReader<T> StartOperation<T>(Func<ILinqService,CancellationToken,Task<IReadOnlyList<T>>> operation)
		{
			return new OperationChannelReader<T>(PrepareOperation(operation), Context.ConnectionAborted);
		}

		// A plain invocation of a client older than 6.6.0 carries no cancellation of its own: only a lost connection
		// cancels it.
		Task<TResult> InvokeOperation<TResult>(Func<ILinqService,CancellationToken,Task<TResult>> operation)
		{
			return PrepareOperation(operation)(Context.ConnectionAborted);
		}

		// Everything that needs the hub is read now: a stream runs the operation after its hub method has returned.
		Func<CancellationToken,Task<TResult>> PrepareOperation<TResult>(Func<ILinqService,CancellationToken,Task<TResult>> operation)
		{
			var service    = LinqService;
			var connection = GetConnectionState();
			var options    = _options ?? connection.Options;
			var global     = options.MaxConcurrentCalls is { } max
				? _globalLimiters.GetOrAdd((GetType(), max), static key => new SemaphoreSlim(key.MaxConcurrentCalls, key.MaxConcurrentCalls))
				: null;

			return cancellationToken => RunAsync(service, options, connection, global, operation, cancellationToken);
		}

		static async Task<TResult> RunAsync<TResult>(
			ILinqService                                          service,
			LinqToDBHubOptions                                    options,
			ConnectionState                                       connection,
			SemaphoreSlim?                                        global,
			Func<ILinqService,CancellationToken,Task<TResult>>    operation,
			CancellationToken                                     cancellationToken)
		{
			await connection.Calls.WaitAsync(cancellationToken).ConfigureAwait(false);

			try
			{
				// SemaphoreSlim completes a cancelled wait asynchronously, so a slot freed in that window is still granted
				// to the waiter: a call cancelled while it was queued must not run all the same.
				cancellationToken.ThrowIfCancellationRequested();

				if (global != null)
					await global.WaitAsync(cancellationToken).ConfigureAwait(false);

				try
				{
					cancellationToken.ThrowIfCancellationRequested();

					return await operation(service, cancellationToken).ConfigureAwait(false);
				}
				catch (Exception exception) when (options.TransferInternalExceptionToClient && exception is not OperationCanceledException and not HubException)
				{
					throw new HubException(exception.ToString(), exception);
				}
				finally
				{
					global?.Release();
				}
			}
			finally
			{
				connection.Calls.Release();
			}
		}

		static IReadOnlyList<string> SplitResult(string result)
		{
			if (result.Length <= ResultChunkSize)
				return [result];

			var chunks = new List<string>(result.Length / ResultChunkSize + 1);

			for (var start = 0; start < result.Length;)
			{
				var length = Math.Min(ResultChunkSize, result.Length - start);

				// Never split a surrogate pair: each chunk is a string of its own on the wire.
				if (start + length < result.Length && char.IsHighSurrogate(result[start + length - 1]))
					length--;

				chunks.Add(result.Substring(start, length));
				start += length;
			}

			return chunks;
		}

		ConnectionState GetConnectionState()
		{
			var items = Context.Items;

			lock (items)
			{
				if (items.TryGetValue(_connectionStateKey, out var value) && value is ConnectionState state)
					return state;

				state = CreateConnectionState();
				items[_connectionStateKey] = state;

				return state;
			}
		}

		ConnectionState CreateConnectionState()
		{
			var options       = _options ?? ResolveRegisteredOptions() ?? _defaultOptions;
			var perConnection = options.MaxConcurrentCallsPerConnection;
			var maxMessage    = -1L;

#if NET8_0_OR_GREATER
			if (ResolveHubOptions() is { } hubOptions)
			{
				perConnection ??= hubOptions.MaximumParallelInvocationsPerClient;
				maxMessage      = hubOptions.MaximumReceiveMessageSize ?? -1L;
			}
#endif

			return new ConnectionState(options, Math.Max(perConnection ?? 1, 1), maxMessage);
		}

		// The options registered in the services, for a hub that was not given options by its constructor.
		LinqToDBHubOptions? ResolveRegisteredOptions()
		{
#if NET8_0_OR_GREATER
			return Context.GetHttpContext()?.RequestServices.GetService<IOptions<LinqToDBHubOptions>>()?.Value;
#else
			try
			{
				return (GetRequestServices()?.GetService(typeof(IOptions<LinqToDBHubOptions>)) as IOptions<LinqToDBHubOptions>)?.Value;
			}
			catch (Exception ex) when (ex is ObjectDisposedException or TargetInvocationException)
			{
				// The connection may hold the HTTP context of a request that has ended, and its services with it.
				return null;
			}
#endif
		}

#if !NET8_0_OR_GREATER
		const string HttpContextFeatureName = "Microsoft.AspNetCore.Http.Connections.Features.IHttpContextFeature";

		// The legacy server's GetHttpContext() is in Microsoft.AspNetCore.SignalR, which this package does not
		// reference (it needs Microsoft.AspNetCore.SignalR.Core only); it reads the connection feature below. A long
		// polling connection holds a copy of the HTTP context without services there.
		IServiceProvider? GetRequestServices()
		{
			foreach (var feature in Context.Features)
			{
				if (!string.Equals(feature.Key.FullName, HttpContextFeatureName, StringComparison.Ordinal))
					continue;

				var httpContext = feature.Key.GetProperty("HttpContext")?.GetValue(feature.Value);

				return httpContext?.GetType().GetProperty("RequestServices")?.GetValue(httpContext) as IServiceProvider;
			}

			return null;
		}
#endif

#if NET8_0_OR_GREATER
		// The options Signal/R applies to this hub: HubOptions<THub> when AddHubOptions<THub> configured them (it
		// registers HubOptionsSetup<THub>, which copies the global values and marks the per-hub options as set),
		// the global HubOptions otherwise. That is the rule Signal/R's connection handler follows, and it applies
		// to every property at once. The hub type is only known at run time here.
		[UnconditionalSuppressMessage("AOT", "IL3050", Justification = "The generic types are instantiated over reference types, so their shared code exists; the hub's own registration (MapHub<THub>, AddHubOptions<THub>) roots the same instantiations.")]
		[UnconditionalSuppressMessage("Trimming", "IL2055", Justification = "See IL3050: HubOptions<>, HubOptionsSetup<>, IOptions<> and IConfigureOptions<> are kept by Signal/R and the options library.")]
		HubOptions? ResolveHubOptions()
		{
			var services = Context.GetHttpContext()?.RequestServices;

			if (services == null)
				return null;

			var hubType      = GetType();
			var optionsType  = typeof(HubOptions<>).MakeGenericType(hubType);
			var setupType    = typeof(HubOptionsSetup<>).MakeGenericType(hubType);
			var configureAll = typeof(IEnumerable<>).MakeGenericType(typeof(IConfigureOptions<>).MakeGenericType(optionsType));

			if (services.GetService(configureAll) is IEnumerable<object> configures)
			{
				foreach (var configure in configures)
				{
					if (configure.GetType() == setupType)
						return (services.GetService(typeof(IOptions<>).MakeGenericType(optionsType)) as IOptions<HubOptions>)?.Value;
				}
			}

			return services.GetService<IOptions<HubOptions>>()?.Value;
		}
#endif

		/// <summary>
		/// Per-connection state, kept in the connection's items.
		/// </summary>
		sealed class ConnectionState(LinqToDBHubOptions options, int maxConcurrentCalls, long maximumReceiveMessageSize)
		{
			// The options the connection's first call resolved: the registered ones for a hub that was not given options
			// by its constructor.
			public LinqToDBHubOptions Options { get; } = options;

			// Never disposed: a SemaphoreSlim holds no unmanaged resources without AvailableWaitHandle, and an
			// operation that finishes after the connection is gone still releases it.
			public SemaphoreSlim Calls                     { get; } = new(maxConcurrentCalls, maxConcurrentCalls);
			public long          MaximumReceiveMessageSize { get; } = maximumReceiveMessageSize;
		}
	}
}
