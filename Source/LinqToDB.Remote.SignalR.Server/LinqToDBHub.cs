using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

#if NET8_0_OR_GREATER
using Microsoft.AspNetCore.Http;
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

		static readonly LinqToDBHubOptions _defaultOptions = new();
		static readonly object             _connectionStateKey = new();

		readonly LinqToDBHubOptions? _options;

		/// <summary>
		/// Creates a hub with default <see cref="LinqToDBHubOptions"/>. On .NET 8+ servers the hub uses the
		/// options registered in the request's services when there are any.
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

		LinqToDBHubOptions Options
		{
			get
			{
				if (_options != null)
					return _options;

#if NET8_0_OR_GREATER
				if (Context.GetHttpContext()?.RequestServices.GetService<IOptions<LinqToDBHubOptions>>() is { } options)
					return options.Value;
#endif

				return _defaultOptions;
			}
		}

		ChannelReader<T> StartOperation<T>(Func<ILinqService,CancellationToken,Task<IReadOnlyList<T>>> operation)
		{
			// Everything that needs the hub is read now: the reader runs the operation after this method returns.
			var service    = LinqService;
			var options    = Options;
			var connection = GetConnectionState(options);

			return new OperationChannelReader<T>(
				cancellationToken => RunAsync(service, options, connection, operation, cancellationToken),
				Context.ConnectionAborted);
		}

		static async Task<IReadOnlyList<T>> RunAsync<T>(
			ILinqService                                              service,
			LinqToDBHubOptions                                        options,
			ConnectionState                                           connection,
			Func<ILinqService,CancellationToken,Task<IReadOnlyList<T>>> operation,
			CancellationToken                                         cancellationToken)
		{
			await connection.Calls.WaitAsync(cancellationToken).ConfigureAwait(false);

			try
			{
				var global = options.GetGlobalLimiter();

				if (global != null)
					await global.WaitAsync(cancellationToken).ConfigureAwait(false);

				try
				{
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

		ConnectionState GetConnectionState(LinqToDBHubOptions? options = null)
		{
			var items = Context.Items;

			lock (items)
			{
				if (items.TryGetValue(_connectionStateKey, out var value) && value is ConnectionState state)
					return state;

				state = CreateConnectionState(options ?? Options);
				items[_connectionStateKey] = state;

				return state;
			}
		}

		ConnectionState CreateConnectionState(LinqToDBHubOptions options)
		{
			var perConnection = options.MaxConcurrentCallsPerConnection;
			var maxMessage    = -1L;

#if NET8_0_OR_GREATER
			if (ResolveHubOptions() is { } hubOptions)
			{
				perConnection ??= hubOptions.MaximumParallelInvocationsPerClient;
				maxMessage      = hubOptions.MaximumReceiveMessageSize ?? -1L;
			}
#endif

			return new ConnectionState(Math.Max(perConnection ?? 1, 1), maxMessage);
		}

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
		sealed class ConnectionState(int maxConcurrentCalls, long maximumReceiveMessageSize)
		{
			// Never disposed: a SemaphoreSlim holds no unmanaged resources without AvailableWaitHandle, and an
			// operation that finishes after the connection is gone still releases it.
			public SemaphoreSlim Calls                     { get; } = new(maxConcurrentCalls, maxConcurrentCalls);
			public long          MaximumReceiveMessageSize { get; } = maximumReceiveMessageSize;
		}
	}
}
