using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB.Internal.Linq;

namespace LinqToDB.Internal.Async
{
	// Rows read to the end before the read-consistency transaction committed. The first enumeration replays them; a
	// later one runs the source again, as re-enumerating a query does.
	internal sealed class BufferedAsyncEnumerable<T> : IAsyncEnumerable<T>
	{
		readonly IAsyncEnumerable<T> _source;

		BufferedRows<T>? _rows;

		BufferedAsyncEnumerable(BufferedRows<T> rows, IAsyncEnumerable<T> source)
		{
			_rows   = rows;
			_source = source;
		}

		public static async Task<BufferedAsyncEnumerable<T>> CreateAsync(IAsyncEnumerable<T> source, CancellationToken cancellationToken)
		{
			var rows = await BufferedRows<T>.ReadAsync(source, cancellationToken).ConfigureAwait(false);

			return new BufferedAsyncEnumerable<T>(rows, source);
		}

		public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
		{
			var rows = Interlocked.Exchange(ref _rows, null);

			return rows == null
				? _source.GetAsyncEnumerator(cancellationToken)
				: rows.WithCancellation(cancellationToken);
		}
	}
}
