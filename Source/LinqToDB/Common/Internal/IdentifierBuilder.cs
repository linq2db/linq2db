using System;
using System.Collections.Concurrent;
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
			return _identifiers.GetOrAdd(_stringBuilder.ToString(), static _ => Interlocked.Increment(ref _counter));
		}
	}
}
