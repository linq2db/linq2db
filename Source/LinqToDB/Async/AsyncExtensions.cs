using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace LinqToDB.Async
{
	using Linq;

	/// <summary>
	/// This API supports the LinqToDB infrastructure and is not intended to be used  directly from your code.
	/// This API may change or be removed in future releases.
	/// </summary>
	public static class AsyncExtensions
	{
		#region AsAsyncEnumerable

		/// <summary>
		/// This API supports the LinqToDB infrastructure and is not intended to be used  directly from your code.
		/// This API may change or be removed in future releases.
		/// </summary>
		public static IAsyncEnumerable<TSource> AsAsyncEnumerable<TSource>(
			[NotNull] this IQueryable<TSource> source,
			CancellationToken                  token = default)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));

			if (source is ExpressionQuery<TSource> query)
				return query.GetAsyncEnumerable();

			throw new InvalidOperationException("ExpressionQuery expected.");
		}

		#endregion

		/// <summary>Returns the first element of a sequence.</summary>
		/// <param name="source">The <see cref="T:System.Collections.Generic.IEnumerable`1" /> to return the first element of.</param>
		/// <param name="token">Cancellation token</param>
		/// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
		/// <returns>The first element in the specified sequence.</returns>
		/// <exception cref="T:System.ArgumentNullException">
		/// <paramref name="source" /> is <see langword="null" />.</exception>
		/// <exception cref="T:System.InvalidOperationException">The source sequence is empty.</exception>
		public static async Task<TSource> FirstAsync<TSource>([NotNull] this IAsyncEnumerable<TSource> source, CancellationToken token = default)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));

	        using (var enumerator = source.GetEnumerator())
	        {
		        if (await enumerator.MoveNext(token))
			        return enumerator.Current;
	        }

	        throw new InvalidOperationException("The source sequence is empty.");
		}

	}
}
