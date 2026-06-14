using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace Tests
{
	public sealed class CustomTestContext
	{
		public static string BASELINE          = "key-baseline";
		public static string TRACE             = "key-trace";
		public static string LIMITED           = "key-limited";
		public static string BASELINE_DISABLED = "key-baseline-disabled";
		public static string TRACE_DISABLED    = "key-trace-disabled";
		public static string LASTQUERY         = "key-lastquery";

		// Per-test context for the local/direct execution path. Established in [SetUp]
		// (TestBase.OnBeforeTest); flows to the test body, [TearDown] and any threads/tasks
		// the test spawns, so parallel tests on different providers stay isolated.
		private static readonly AsyncLocal<CustomTestContext?> _current = new();

		// The LinqService test server executes queries on its own thread-pool threads where the
		// test's AsyncLocal does not flow. Each remote (LinqService) test publishes its context
		// here keyed by provider; the server resolves the right one via _serverProvider. Keyed by
		// provider so a lagging or cross-provider server write can't land in another test's capture.
		private static readonly ConcurrentDictionary<string, CustomTestContext> _remoteByProvider =
			new ConcurrentDictionary<string, CustomTestContext>(StringComparer.OrdinalIgnoreCase);

		// Set by the test's LinqService server (TestLinqService.CreateDataContext) to the provider
		// the current request executes for; AsyncLocal so it flows through the server's async query
		// execution and is visible to the trace sink (Get) on the executing thread.
		private static readonly AsyncLocal<string?> _serverProvider = new();

		// Fallback for writes that happen outside any test (assembly init, stray logging).
		private static readonly CustomTestContext _shared = new CustomTestContext();

		private readonly ConcurrentDictionary<string, object?> _state = new ConcurrentDictionary<string, object?>();

		// the provider this context was published under (remote tests only); used by Release
		private string? _remoteProvider;

		// Establish a fresh per-test context. Call from [SetUp]. A remote (LinqService) test also
		// publishes it under its provider so the shared server can resolve it.
		public static void Begin(bool isRemote, string? provider)
		{
			var ctx = new CustomTestContext();
			_current.Value = ctx;

			if (isRemote && provider != null)
			{
				ctx._remoteProvider = provider;
				_remoteByProvider[provider] = ctx;
			}
		}

		// Called by the test LinqService server so its query logging resolves the remote test for
		// the provider being executed.
		public static void SetServerProvider(string? provider) => _serverProvider.Value = provider;

		public static CustomTestContext Get()
		{
			if (_current.Value is { } current)
				return current;

			if (_serverProvider.Value is { } provider && _remoteByProvider.TryGetValue(provider, out var remote))
				return remote;

			return _shared;
		}

		public static void Release()
		{
			var ctx = _current.Value;
			_current.Value = null;

			// only the remote test that published the register clears it (atomic key+value remove,
			// so a newer test's entry for the same provider is never dropped)
			if (ctx?._remoteProvider is { } provider)
				((ICollection<KeyValuePair<string, CustomTestContext>>)_remoteByProvider)
					.Remove(new KeyValuePair<string, CustomTestContext>(provider, ctx));
		}

		public TValue Get<TValue>(string name)
		{
			if (_state.TryGetValue(name, out var value))
				return (TValue)value!;

			return default!;
		}

		public void Set<TValue>(string name, TValue value)
		{
			_state.AddOrUpdate(name, value, (key, old) => value);
		}
	}
}
