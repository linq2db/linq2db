using System.Collections.Generic;
using System.Threading;

namespace Tests
{
	internal class CustomTestContext
	{
		public static string TRACE = "key-trace";

		private static readonly AsyncLocal<CustomTestContext?> _context = new AsyncLocal<CustomTestContext?>();

		private readonly IDictionary<string, object?> _state = new Dictionary<string, object?>();

		public static CustomTestContext Create()
		{
			_context.Value = new CustomTestContext();
			return Get();
		}

		public static CustomTestContext Get()
		{
			if (_context.Value == null)
				return Create();

			return _context.Value;
		}

		public static void Release()
		{
			_context.Value = null;
		}

		public TValue Get<TValue>(string name)
		{
			if (_state.ContainsKey(name))
				return (TValue)_state[name]!;

			return default!;
		}

		public void Set<TValue>(string name, TValue value)
		{
			_state[name] = value;
		}

	}
}
