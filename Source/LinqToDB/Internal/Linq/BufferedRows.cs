using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

#if SUPPORTS_SPAN
using System.Buffers;
using System.Runtime.CompilerServices;
#endif

namespace LinqToDB.Internal.Linq
{
	// The main rows of an eager load, read to the end before its read-consistency transaction commits. Where the target has
	// ArrayPool the array is rented and goes back on the first Dispose only: returning it twice would hand one array to two
	// later renters.
	internal sealed class BufferedRows<T> : IEnumerator<T>, IAsyncEnumerator<T>
	{
		const int InitialCapacity = 16;

		readonly int      _count;
		T[]?              _rows;
		int               _index = -1;
		CancellationToken _cancellationToken;

		BufferedRows(T[] rows, int count)
		{
			_rows  = rows;
			_count = count;
		}

		public static BufferedRows<T> Read(IEnumerable<T> source)
		{
			var rows  = Rent(InitialCapacity);
			var count = 0;

			try
			{
				foreach (var row in source)
				{
					if (count == rows.Length)
						rows = Grow(rows, count);

					rows[count++] = row;
				}
			}
			catch
			{
				Return(rows, count);
				throw;
			}

			return new BufferedRows<T>(rows, count);
		}

		public static async Task<BufferedRows<T>> ReadAsync(IAsyncEnumerable<T> source, CancellationToken cancellationToken)
		{
			var rows  = Rent(InitialCapacity);
			var count = 0;

			try
			{
				await foreach (var row in source.ConfigureAwait(false).WithCancellation(cancellationToken))
				{
					if (count == rows.Length)
						rows = Grow(rows, count);

					rows[count++] = row;
				}
			}
			catch
			{
				Return(rows, count);
				throw;
			}

			return new BufferedRows<T>(rows, count);
		}

		public BufferedRows<T> WithCancellation(CancellationToken cancellationToken)
		{
			_cancellationToken = cancellationToken;
			return this;
		}

		public T Current => _rows != null && _index >= 0 && _index < _count ? _rows[_index] : default!;

		object? IEnumerator.Current => Current;

		public bool MoveNext()
		{
			if (_rows == null || _index + 1 >= _count)
				return false;

			_index++;
			return true;
		}

		public ValueTask<bool> MoveNextAsync()
		{
			_cancellationToken.ThrowIfCancellationRequested();

			return new ValueTask<bool>(MoveNext());
		}

		public void Reset()
		{
			_index = -1;
		}

		public void Dispose()
		{
			var rows = Interlocked.Exchange(ref _rows, null);

			if (rows != null)
				Return(rows, _count);
		}

		public ValueTask DisposeAsync()
		{
			Dispose();
			return default;
		}

		static T[] Grow(T[] rows, int count)
		{
			var grown = Rent(rows.Length * 2);

			Array.Copy(rows, grown, count);
			Return(rows, count);

			return grown;
		}

#if SUPPORTS_SPAN
		static T[] Rent(int minimumLength) => ArrayPool<T>.Shared.Rent(minimumLength);

		static void Return(T[] rows, int count)
		{
			if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
				Array.Clear(rows, 0, count);

			ArrayPool<T>.Shared.Return(rows);
		}
#else
		static T[] Rent(int minimumLength) => new T[minimumLength];

		static void Return(T[] rows, int count)
		{
		}
#endif
	}
}
