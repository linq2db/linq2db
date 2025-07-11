using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB;

namespace LinqToDB.Internal.Linq
{
	sealed class LimitResultEnumerable<T> : IResultEnumerable<T>
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
			IEnumerable<T> source = _source;
			if (_skip != null)
				source = source.Skip(_skip.Value);
			if (_take != null)
				source = source.Take(_take.Value);
			return source.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		sealed class LimitAsyncEnumerator : IAsyncEnumerator<T>
		{
			readonly IAsyncEnumerable<T> _enumerable;
			readonly int?                _skip;
			readonly int?                _take;
			readonly CancellationToken   _cancellationToken;

			IAsyncEnumerator<T>? _enumerator;
			int                  _skipped;
			int                  _taken;
			bool                 _finished;

			public LimitAsyncEnumerator(IAsyncEnumerable<T> enumerable, int? skip, int? take, CancellationToken cancellationToken)
			{
				_enumerable        = enumerable;
				_skip              = skip;
				_take              = take;
				_cancellationToken = cancellationToken;
			}

			public ValueTask DisposeAsync()
			{
				if (_enumerator == null)
					return new ValueTask();

				return _enumerator.DisposeAsync();
			}

			public T Current
			{
				get
				{
					if (_enumerator == null)
						throw new InvalidOperationException("Enumeration not started.");

					return _enumerator.Current;
				}
			}

			public async ValueTask<bool> MoveNextAsync()
			{
				if (_enumerator == null)
				{
					_enumerator = _enumerable.GetAsyncEnumerator(_cancellationToken);
					_finished   = false;
					_skipped    = 0;
					_taken      = 0;
				}

				if (_finished)
					return false;

				if (_skip != null)
				{
					while (_skipped < _skip.Value)
					{
						if (!await _enumerator.MoveNextAsync().ConfigureAwait(false))
						{
							_finished = true;
							return false;
						}

						++_skipped;
					}
				}

				if (_take != null)
				{
					if (_taken >= _take.Value)
					{
						_finished = true;
						return false;
					}

					if (!await _enumerator.MoveNextAsync().ConfigureAwait(false))
					{
						_finished = true;
						return false;
					}

					++_taken;
					return true;
				}

				if (!await _enumerator.MoveNextAsync().ConfigureAwait(false))
				{
					_finished = true;
					return false;
				}

				return true;
			}
		}

		public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
		{
			return new LimitAsyncEnumerator(_source, _skip, _take, cancellationToken);
		}
	}
}
