﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LinqToDB.Common
{
	internal class EnumerableHelper
	{
#if !NETFRAMEWORK
		public static IEnumerable<T> AsyncToSyncEnumerable<T>(IAsyncEnumerator<T> enumerator)
		{
			while (enumerator.MoveNextAsync().GetAwaiter().GetResult())
			{
				yield return enumerator.Current;
			}
		}

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
		public static async IAsyncEnumerable<T> SyncToAsyncEnumerable<T>(IEnumerable<T> enumerable)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
		{
			foreach (var item in enumerable)
			{
				yield return item;
			}
		}
#endif

		/// <summary>
		/// Split enumerable source into batches of specified size.
		/// Limitation: each batch could be enumerated only once or exception will be generated.
		/// </summary>
		/// <typeparam name="T">Type of element in source.</typeparam>
		/// <param name="source">Source collection to split into batches.</param>
		/// <param name="batchSize">Size of each batch. Must be positive number.</param>
		/// <returns>New enumerable of batches.</returns>
		public static IEnumerable<IEnumerable<T>> Batch<T>(IEnumerable<T> source, int batchSize)
		{
			var batcher = new Batcher<T>(source, batchSize);

			yield return batcher.Current;

			while (batcher.MoveNext())
				yield return batcher.Current;
		}

		private class Batcher<T>
		{
			private readonly int _batchSize;
			private readonly IEnumerator<T> _enumerator;

			private bool _sourceDepleted;

			private bool _currentBatchEnumerateStarted;
			private bool _currentBatchEnumerateCompleted;
			private int  _currentBatchEnumeratedCount;

			public Batcher(IEnumerable<T> source, int batchSize)
			{
				if (batchSize < 1)
					throw new ArgumentException($"{nameof(batchSize)} must be >= 1");

				_batchSize = batchSize;
				_enumerator = source.GetEnumerator();
			}

			public IEnumerable<T> Current
			{
				get
				{
					if (_currentBatchEnumerateStarted)
						throw new InvalidOperationException("Cannot enumerate IBatched.Current multiple times");

					_currentBatchEnumerateStarted = true;
					for (var i = 0; i < _batchSize; i++)
					{
						_currentBatchEnumeratedCount++;

						if (_enumerator.MoveNext())
							yield return _enumerator.Current;
						else 
						{
							_sourceDepleted = true;
							break;
						}
					}

					_currentBatchEnumerateCompleted = true;
					yield break;
				}
			}

			public bool MoveNext()
			{
				if (_sourceDepleted)
					return false;

				if (!_currentBatchEnumerateCompleted)
					for (var i = _currentBatchEnumeratedCount; i < _batchSize; i++)
					{
						if (!_enumerator.MoveNext())
						{
							_sourceDepleted = true;
							return false;
						}
					}

				_currentBatchEnumerateStarted = false;
				_currentBatchEnumeratedCount = 0;
				_currentBatchEnumerateCompleted = false;

				return true;
			}
		}

#if !NETFRAMEWORK
		/// <summary>
		/// Split enumerable source into batches of specified size.
		/// Limitation: each batch should be enumerated only once or exception will be generated.
		/// </summary>
		/// <typeparam name="T">Type of element in source.</typeparam>
		/// <param name="source">Source collection to split into batches.</param>
		/// <param name="batchSize">Size of each batch. Must be positive number.</param>
		/// <returns>New enumerable of batches.</returns>
		public static IAsyncEnumerable<IAsyncEnumerable<T>> Batch<T>(IAsyncEnumerable<T> source, int batchSize)
		{
			if (batchSize <= 0) throw new ArgumentOutOfRangeException(nameof(batchSize));
			if (batchSize < Int32.MaxValue) return new AsyncBatchEnumerable<T>(source, batchSize);
			return BatchSingle(source);
		}

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
		private static async IAsyncEnumerable<IAsyncEnumerable<T>> BatchSingle<T>(IAsyncEnumerable<T> source)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
		{
			yield return source;
		}

		private class AsyncBatchEnumerable<T> : IAsyncEnumerable<IAsyncEnumerable<T>>
		{
			IAsyncEnumerable<T> _source;
			int _batchSize;

			public AsyncBatchEnumerable(IAsyncEnumerable<T> source, int batchSize)
			{
				_source    = source;
				_batchSize = batchSize;
			}

			public IAsyncEnumerator<IAsyncEnumerable<T>> GetAsyncEnumerator(CancellationToken cancellationToken = default)
				=> new AsyncBatchEnumerator<T>(_source, _batchSize, cancellationToken);
		}

		private class AsyncBatchEnumerator<T> : IAsyncEnumerator<IAsyncEnumerable<T>>, IAsyncEnumerable<T>
		{
			readonly IAsyncEnumerator<T> _source;
			readonly int                 _batchSize;
			bool                         _finished;
			bool                         _isCurrent;
			IAsyncEnumerator<T>?         _current;

			public AsyncBatchEnumerator(IAsyncEnumerable<T> source, int batchSize, CancellationToken cancellationToken)
			{
				_source    = source.GetAsyncEnumerator(cancellationToken);
				_batchSize = batchSize;
			}

			public IAsyncEnumerable<T> Current => _isCurrent ? this : throw new InvalidOperationException();

			IAsyncEnumerator<T> IAsyncEnumerable<T>.GetAsyncEnumerator(CancellationToken cancellationToken)
			{
				if (_current == null) throw new InvalidOperationException();

				var enumerator = _current;
				_current       = null;

				return enumerator;
			}

			public ValueTask DisposeAsync() => _source.DisposeAsync();

			public async ValueTask<bool> MoveNextAsync()
			{
				if (_finished) return false;

				_isCurrent = await _source.MoveNextAsync().ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
				_current   = _isCurrent ? GetNewEnumerable() : null;
				_finished  = !_isCurrent;

				return _isCurrent;
			}

			private async IAsyncEnumerator<T> GetNewEnumerable()
			{
				int returned = 0;
				yield return _source.Current;
				while (++returned < _batchSize)
				{
					if (_finished || !await _source.MoveNextAsync().ConfigureAwait(Common.Configuration.ContinueOnCapturedContext))
					{
						_finished = true;
						yield break;
					}
					yield return _source.Current;
				}
			}
		}
#endif
	}
}
