using System;
using System.Collections.Generic;
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
	internal static class AsyncExtensions
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
			this IAsyncEnumerable<T> source, 
			CancellationToken        cancellationToken = default)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));

			var result = new List<T>();
			var enumerator = source.GetAsyncEnumerator(cancellationToken);
#if NETFRAMEWORK
			await using (enumerator)
#else
			await using (enumerator.ConfigureAwait(Common.Configuration.ContinueOnCapturedContext))
#endif
			{
				while (await enumerator.MoveNextAsync().ConfigureAwait(Common.Configuration.ContinueOnCapturedContext))
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
			this IAsyncEnumerable<T> source,
			CancellationToken        cancellationToken = default)
		{
			return (await source.ToListAsync(cancellationToken).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext)).ToArray();
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
		public static async Task<T> FirstOrDefaultAsync<T>(
			this IAsyncEnumerable<T> source,
			CancellationToken        cancellationToken = default)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));

			var enumerator = source.GetAsyncEnumerator(cancellationToken);
#if NETFRAMEWORK
			await using (enumerator)
#else
			await using (enumerator.ConfigureAwait(Common.Configuration.ContinueOnCapturedContext))
#endif
			{
				if (await enumerator.MoveNextAsync().ConfigureAwait(Common.Configuration.ContinueOnCapturedContext))
					return enumerator.Current;
				return default!;
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
		public static async Task<TSource> FirstAsync<TSource>(this IAsyncEnumerable<TSource> source, CancellationToken token = default)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));

			var enumerator = source.GetAsyncEnumerator(token);
#if NETFRAMEWORK
			await using (enumerator)
#else
			await using (enumerator.ConfigureAwait(Common.Configuration.ContinueOnCapturedContext))
#endif
			{
				if (await enumerator.MoveNextAsync().ConfigureAwait(Common.Configuration.ContinueOnCapturedContext))
					return enumerator.Current;
			}

			throw new InvalidOperationException("The source sequence is empty.");
		}

	}
}
