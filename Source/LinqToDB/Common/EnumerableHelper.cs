using System;
using System.Collections.Generic;

namespace LinqToDB.Common
{
	internal class EnumerableHelper
	{
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
	}
}
