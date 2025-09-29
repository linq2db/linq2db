using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LinqToDB.Internal.Async
{
	// Do not expose to public surface, will conflict with other libraries
	/// <summary>
	/// Contains custom extension methods for <see cref="IAsyncEnumerable{T}"/> interface.
	/// </summary>
	static class AsyncEnumerableExtensions
	{
		/// <summary>
		///     Asynchronously creates a <see cref="List{T}" /> from <see cref="IAsyncEnumerable{T}" />
		///     by enumerating it asynchronously.
		/// </summary>
		/// <remarks>
		///     Multiple active operations on the same context instance are not supported.  Use 'await' to ensure
		///     that any asynchronous operations have completed before calling another method on this context.
		/// </remarks>
		/// <param name="source">Async enumerable.</param>
		/// <param name="cancellationToken">
		///     A <see cref="CancellationToken" /> to observe while waiting for the task to complete.
		/// </param>
		/// <returns>
		///     A task that represents the asynchronous operation.
		///     The task result contains a <see cref="List{T}" /> that contains elements from the input sequence.
		/// </returns>
		public static async Task<List<T>> ToListAsync<T>(
#if !NET10_0_OR_GREATER
			this
#endif
			IAsyncEnumerable<T> source,
			CancellationToken        cancellationToken = default)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));

			var result = new List<T>();
			var enumerator = source.GetAsyncEnumerator(cancellationToken);
			await using (enumerator.ConfigureAwait(false))
			{
				while (await enumerator.MoveNextAsync().ConfigureAwait(false))
				{
					result.Add(enumerator.Current);
				}
			}

			return result;
		}

		/// <summary>
		///     Asynchronously creates an array from <see cref="IAsyncEnumerable{T}" />.
		/// </summary>
		/// <remarks>
		///     Multiple active operations on the same context instance are not supported.  Use 'await' to ensure
		///     that any asynchronous operations have completed before calling another method on this context.
		/// </remarks>
		/// <param name="source">Async enumerable.</param>
		/// <param name="cancellationToken">
		///     A <see cref="CancellationToken" /> to observe while waiting for the task to complete.
		/// </param>
		/// <returns>
		///     A task that represents the asynchronous operation.
		///     The task result contains an array that contains elements from the input sequence.
		/// </returns>
		public static async Task<T[]> ToArrayAsync<T>(
#if !NET10_0_OR_GREATER
			this
#endif
			IAsyncEnumerable<T> source,
			CancellationToken cancellationToken = default)
		{
			return (await source.ToListAsync(cancellationToken).ConfigureAwait(false)).ToArray();
		}

		/// <summary>
		///     Asynchronously returns the first element of a sequence, or a default value if the sequence contains no elements />
		///     by enumerating it asynchronously.
		/// </summary>
		/// <remarks>
		///     Multiple active operations on the same context instance are not supported.  Use 'await' to ensure
		///     that any asynchronous operations have completed before calling another method on this context.
		/// </remarks>
		/// <param name="source">Async enumerable.</param>
		/// <param name="cancellationToken">
		///     A <see cref="CancellationToken" /> to observe while waiting for the task to complete.
		/// </param>
		/// <returns>
		///     A task that represents the asynchronous operation.
		///     The task result contains a <see cref="List{T}" /> that contains elements from the input sequence.
		/// </returns>
		public static async Task<T?> FirstOrDefaultAsync<T>(
#if !NET10_0_OR_GREATER
			this
#endif
			IAsyncEnumerable<T> source,
			CancellationToken cancellationToken = default)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));

			var enumerator = source.GetAsyncEnumerator(cancellationToken);
			await using (enumerator.ConfigureAwait(false))
			{
				if (await enumerator.MoveNextAsync().ConfigureAwait(false))
					return enumerator.Current;
				return default;
			}
		}

		/// <summary>Returns the first element of a sequence.</summary>
		/// <param name="source">The <see cref="IEnumerable{T}" /> to return the first element of.</param>
		/// <param name="token">Cancellation token</param>
		/// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
		/// <returns>The first element in the specified sequence.</returns>
		/// <exception cref="ArgumentNullException">
		/// <paramref name="source" /> is <see langword="null" />.</exception>
		/// <exception cref="InvalidOperationException">The source sequence is empty.</exception>
		public static async Task<TSource> FirstAsync<TSource>(
#if !NET10_0_OR_GREATER
			this
#endif
			IAsyncEnumerable<TSource> source,
			CancellationToken token = default)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));

			var enumerator = source.GetAsyncEnumerator(token);
			await using (enumerator.ConfigureAwait(false))
			{
				if (await enumerator.MoveNextAsync().ConfigureAwait(false))
					return enumerator.Current;
			}

			throw new InvalidOperationException("The source sequence is empty.");
		}

		/// <summary>
		/// Asynchronously returns the only element of a sequence, or a default value if the sequence is empty;
		/// this method throws an exception if there is more than one element in the sequence.
		/// </summary>
		/// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
		/// <param name="source">An <see cref="IQueryable{T}" /> to return the single element of.</param>
		/// <param name="cancellationToken">A <see cref="CancellationToken" /> to observe while waiting for the task to complete.</param>
		/// <returns>
		/// A task that represents the asynchronous operation.
		/// The task result contains the single element of the input sequence, or <see langword="default" /> (
		/// <typeparamref name="TSource" />)
		/// if the sequence contains no elements.
		/// </returns>
		/// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
		/// <exception cref="InvalidOperationException"><paramref name="source" /> contains more than one element.</exception>
		/// <exception cref="OperationCanceledException">If the <see cref="CancellationToken" /> is canceled.</exception>
		public static async Task<TSource?> SingleOrDefaultAsync<TSource>(
#if !NET10_0_OR_GREATER
			this
#endif
			IAsyncEnumerable<TSource> source,
			CancellationToken cancellationToken = default)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));

			var enumerator = source.GetAsyncEnumerator(cancellationToken);
			await using (enumerator.ConfigureAwait(false))
			{
				if (!await enumerator.MoveNextAsync().ConfigureAwait(false))
					return default;

				var first = enumerator.Current;
				if (await enumerator.MoveNextAsync().ConfigureAwait(false))
					throw new InvalidOperationException("The input sequence contains more than one element.");

				return first;
			}
		}

		/// <summary>
		/// Asynchronously returns the only element of a sequence, and throws an exception
		/// if there is not exactly one element in the sequence.
		/// </summary>
		/// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
		/// <param name="source">An <see cref="IQueryable{T}" /> to return the single element of.</param>
		/// <param name="cancellationToken">A <see cref="CancellationToken" /> to observe while waiting for the task to complete.</param>
		/// <returns>
		/// A task that represents the asynchronous operation.
		/// The task result contains the single element of the input sequence.
		/// </returns>
		/// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
		/// <exception cref="InvalidOperationException">
		///     <para>
		///         <paramref name="source" /> contains more than one elements.
		///     </para>
		///     <para>
		///         -or-
		///     </para>
		///     <para>
		///         <paramref name="source" /> contains no elements.
		///     </para>
		/// </exception>
		/// <exception cref="OperationCanceledException">If the <see cref="CancellationToken" /> is canceled.</exception>
		public static async Task<TSource> SingleAsync<TSource>(
#if !NET10_0_OR_GREATER
			this
#endif
			IAsyncEnumerable<TSource> source,
			CancellationToken cancellationToken = default)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));

			var enumerator = source.GetAsyncEnumerator(cancellationToken);
			await using (enumerator.ConfigureAwait(false))
			{
				if (!await enumerator.MoveNextAsync().ConfigureAwait(false))
					throw new InvalidOperationException("Sequence contains no elements.");

				var first = enumerator.Current;
				if (await enumerator.MoveNextAsync().ConfigureAwait(false))
					throw new InvalidOperationException("Sequence contains more than one element.");

				return first;
			}
		}
	}
}
