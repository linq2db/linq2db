using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using LinqToDB.Async;

namespace LinqToDB.Linq
{
	class LimitResultEnumerable<T> : IResultEnumerable<T>
	{
		readonly IResultEnumerable<T> _source;
		readonly int?                 _skip;
		readonly int?                 _take;

		public LimitResultEnumerable(IResultEnumerable<T> source, int? skip, int? take)
		{
			_source = source;
			_skip   = skip;
			_take   = take;
		}

		public IEnumerator<T> GetEnumerator()
		{
			if (_skip == null && _take == null)
				return _source.GetEnumerator();

			return GetLimitedEnumeration().GetEnumerator();
		}

		IEnumerable<T> GetLimitedEnumeration()
		{
			var enumerator = _source.GetEnumerator();
			if (_skip != null)
			{
				for (var i = _skip.Value; i > 0; --i)
				{
					if (!enumerator.MoveNext())
						yield break;
				}
			}

			if (_take != null)
			{
				for (var i = _take.Value; i > 0; --i)
				{
					if (!enumerator.MoveNext())
						yield break;

					yield return enumerator.Current;
				}
			}
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default(CancellationToken))
		{
			throw new NotImplementedException();
		}
	}
}
