using System;
using System.Collections.Concurrent;
using System.Threading;

using NUnit.Framework.Internal;
using NUnit.ParallelByResource;

namespace Tests
{
	public sealed class CustomTestContext
	{
		public static string BASELINE          = "key-baseline";
		public static string TRACE             = "key-trace";
		public static string TRACE_CAPTURED    = "key-trace-captured";
		public static string BASELINE_DISABLED = "key-baseline-disabled";
		public static string TRACE_DISABLED    = "key-trace-disabled";
		public static string LASTQUERY         = "key-lastquery";

		// Per-test context for the local/direct execution path, keyed by the NUnit test id.
		// AsyncLocal is unreliable under NUnit's non-continuous async execution flow, so the
		// context is resolved by the authoritative current-test id (available on the test's
		// setup/body/teardown threads) rather than flowed through AsyncLocal.
		private static readonly ConcurrentDictionary<string, CustomTestContext> _byTest =
			new ConcurrentDictionary<string, CustomTestContext>(StringComparer.Ordinal);

		static string? CurrentTestId => TestExecutionContext.CurrentContext?.CurrentTest?.Id;

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

			if (CurrentTestId is { } id)
				_byTest[id] = ctx;

			if (isRemote && provider != null)
			{
				ctx._remoteProvider = provider;
				_remoteByProvider[provider] = ctx;
			}
		}

		// Called by the test LinqService server so its query logging resolves the remote test for
		// the provider being executed.
		public static void SetServerProvider(string? provider) => _serverProvider.Value = provider;

		// Per-provider database-readiness gate. CreateDatabase signals it once a provider's schema
		// exists; the provider's other tests wait on it under parallel execution. Kept here (the
		// test-context infrastructure home) rather than as static state spread across TestBase.
		private static readonly ResourceReadinessLatch _databaseReady = new ResourceReadinessLatch(StringComparer.OrdinalIgnoreCase);

		// Signalled by CreateDatabase (called even on failure so waiters don't hang).
		public static void MarkDatabaseReady(string provider) => _databaseReady.MarkReady(provider);

		// Bounded wait for a provider's CreateDatabase to finish; returns false on timeout.
		public static bool AwaitDatabaseReady(string provider) => _databaseReady.WaitReady(provider, TimeSpan.FromMinutes(2));

		public static CustomTestContext Get()
		{
			// On a LinqService server execution flow, _serverProvider (set per-request in
			// TestLinqService.CreateDataContext, flowing via AsyncLocal through the query execution)
			// is the authoritative signal and must win over the ambient NUnit CurrentTestId: the
			// server runs on pooled transport threads (gRPC/Kestrel) whose CurrentTestId can resolve
			// to a stale or concurrently-running direct test, which would misroute this remote query's
			// trace into that test's baseline and leave the remote test's own capture short (the
			// direct-vs-remote baseline mismatch seen under parallel runs). _serverProvider is only
			// ever set on a genuine server flow, never on a direct/test thread, so checking it first
			// cannot hijack the direct path.
			if (_serverProvider.Value is { } provider && _remoteByProvider.TryGetValue(provider, out var remote))
				return remote;

			if (CurrentTestId is { } id && _byTest.TryGetValue(id, out var current))
				return current;

			return _shared;
		}

		public static void Release()
		{
			CustomTestContext? ctx = null;

			if (CurrentTestId is { } id)
				_byTest.TryRemove(id, out ctx);

			if (ctx?._remoteProvider is { } provider)
				_remoteByProvider.TryRemove(provider, out _);
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
