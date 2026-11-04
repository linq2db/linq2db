using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LinqToDB.Internal.Async
{
	// Rows read to the end before the read-consistency transaction committed. The first enumeration replays them; a
	// later one runs the source again, as re-enumerating a query does.
	internal sealed class BufferedAsyncEnumerable<T> : IAsyncEnumerable<T>
	{
		readonly IAsyncEnumerable<T> _source;

		List<T>? _rows;

		BufferedAsyncEnumerable(List<T> rows, IAsyncEnumerable<T> source)
		{
			_rows   = rows;
			_source = source;
		}

		public static async Task<BufferedAsyncEnumerable<T>> CreateAsync(IAsyncEnumerable<T> source, CancellationToken cancellationToken)
		{
			var rows = new List<T>();

			await foreach (var row in source.ConfigureAwait(false).WithCancellation(cancellationToken))
				rows.Add(row);

			return new BufferedAsyncEnumerable<T>(rows, source);
		}

		public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
		{
			var rows = Interlocked.Exchange(ref _rows, null);

			return rows == null
				? _source.GetAsyncEnumerator(cancellationToken)
				: new Enumerator(rows, cancellationToken);
		}

		sealed class Enumerator : IAsyncEnumerator<T>
		{
			readonly List<T>           _rows;
			readonly CancellationToken _cancellationToken;

			int _index = -1;

			public Enumerator(List<T> rows, CancellationToken cancellationToken)
			{
				_rows              = rows;
				_cancellationToken = cancellationToken;
			}

			public T Current => _rows[_index];

			public ValueTask<bool> MoveNextAsync()
			{
				_cancellationToken.ThrowIfCancellationRequested();

				if (_index + 1 >= _rows.Count)
					return new ValueTask<bool>(false);

				_index++;

				return new ValueTask<bool>(true);
			}

			public ValueTask DisposeAsync() => default;
		}
	}
}
