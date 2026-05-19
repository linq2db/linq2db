using System;
using System.Collections;
using System.Collections.Generic;

namespace LinqToDB.Internal.Linq
{
	/// <summary>
	/// Wraps an <see cref="IEnumerator{T}"/> so that a teardown closure (typically
	/// <see cref="QueryExecutionContext.Dispose"/>, which drops temp tables) fires exactly once
	/// when the enumerator is disposed. The wrapper survives the using-scope of
	/// <c>StartLoadTransaction</c>, so the teardown runs only after the caller finishes
	/// iterating (e.g. <c>.ToList()</c> consuming the enumerator).
	/// </summary>
	sealed class QueryRunStepTeardownEnumerator<T> : IEnumerator<T>
	{
		readonly IEnumerator<T> _inner;
		readonly Action         _teardown;
		bool                    _torn;

		public QueryRunStepTeardownEnumerator(IEnumerator<T> inner, Action teardown)
		{
			_inner    = inner;
			_teardown = teardown;
		}

		public T Current => _inner.Current;

		object? IEnumerator.Current => Current;

		public bool MoveNext() => _inner.MoveNext();

		public void Reset() => _inner.Reset();

		public void Dispose()
		{
			try
			{
				_inner.Dispose();
			}
			finally
			{
				if (!_torn)
				{
					_torn = true;
					_teardown();
				}
			}
		}
	}
}
