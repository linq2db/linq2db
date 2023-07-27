using System.Collections.Concurrent;

namespace Tests
{
	public sealed class CustomTestContext
	{
		public static string BASELINE          = "key-baseline";
		public static string TRACE             = "key-trace";
		public static string LIMITED           = "key-limited";
		public static string BASELINE_DISABLED = "key-baseline-disabled";
		public static string TRACE_DISABLED    = "key-trace-disabled";

		// because we don't use parallel test run, we just use single global context instance
		// it allows us to access context from other threads, started by tests and from linqservice server
		private static readonly CustomTestContext _context = new CustomTestContext();

		private readonly ConcurrentDictionary<string, object?> _state = new ConcurrentDictionary<string, object?>();

		public static CustomTestContext Get()
		{
			return _context;
		}

		public static void Release()
		{
			_context.Reset();
		}

		private void Reset()
		{
			_state.Clear();
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
