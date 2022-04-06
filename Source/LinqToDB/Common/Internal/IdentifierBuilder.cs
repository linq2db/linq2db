using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace LinqToDB.Common.Internal
{
	class IdentifierBuilder
	{
		public IdentifierBuilder()
		{
		}

		public IdentifierBuilder(object data)
		{
			Add(data);
		}

		readonly StringBuilder _stringBuilder = new ();

		static          int                              _counter;
		static readonly ConcurrentDictionary<string,int> _identifiers = new ();

		public IdentifierBuilder Add(object data)
		{
			_stringBuilder
				.Append('.')
				.Append(data)
				;
			return this;
		}

		public int CreateID()
		{
			var key = _stringBuilder.ToString();
			var id  = _identifiers.GetOrAdd(key, static _ => Interlocked.Increment(ref _counter));

#if DEBUG
			Debug.WriteLine($"CreateID: '{key}' ({id})");
#endif

			return id;
		}

		public static int CreateID(Type? type)
		{
			return type == null ? 0 : new IdentifierBuilder().Add(type.FullName ?? type.Name).CreateID();
		}
	}
}
