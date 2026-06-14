using System.Collections.Concurrent;
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

		// The LinqService test server executes queries on its own thread-pool threads
		// (ConfigureAwait(false)), so the originating test's AsyncLocal does not flow there.
		// Remote (LinqService) test variants are serialized, so exactly one is active at a
		// time and this server-visible register is unambiguous.
		private static volatile CustomTestContext? _remote;

		// Fallback for writes that happen outside any test (assembly init, stray logging).
		private static readonly CustomTestContext _shared = new CustomTestContext();

		private readonly ConcurrentDictionary<string, object?> _state = new ConcurrentDictionary<string, object?>();

		// Establish a fresh per-test context. Call from [SetUp]. For remote (LinqService)
		// tests, also publish it to the server-visible register.
		public static void Begin(bool isRemote)
		{
			var ctx = new CustomTestContext();
			_current.Value = ctx;
			if (isRemote)
				_remote = ctx;
		}

		public static CustomTestContext Get()
		{
			return _current.Value ?? _remote ?? _shared;
		}

		public static void Release()
		{
			var ctx = _current.Value;
			_current.Value = null;

			// only the remote test that published the register clears it; a concurrent
			// direct test must not wipe an in-flight remote test's context
			if (ctx != null && ReferenceEquals(_remote, ctx))
				_remote = null;
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
