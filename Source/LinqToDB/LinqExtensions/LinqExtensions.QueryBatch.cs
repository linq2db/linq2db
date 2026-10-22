using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using JetBrains.Annotations;

using LinqToDB.Async;
using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.Linq;
using LinqToDB.Internal.Reflection;

namespace LinqToDB
{
	public static partial class LinqExtensions
	{
		/// <summary>
		/// Runs several reads in one round trip and returns their results. Every sequence the selector reads
		/// (<c>ToList()</c>, <c>ToArray()</c>) becomes a statement of its own, every scalar it reads becomes part of one shared
		/// statement, and all statements are sent to the database as one command.
		/// </summary>
		/// <remarks>
		/// <para>
		/// The statements are combined whatever <see cref="LinqOptions.UseCombinedCommands"/> is set to;
		/// <see cref="LinqOptions.UseDbBatch"/> chooses how they are sent. When the context or the provider cannot send them as
		/// one command, they run one after another and the results are the same.
		/// </para>
		/// <para>
		/// Where the statements are combined, collections in the selector are loaded with
		/// <see cref="EagerLoadingStrategy.Default"/>, which overrides <see cref="LinqOptions.DefaultEagerLoadingStrategy"/>
		/// and any <c>With*LoadStrategy()</c> call inside the selector; elsewhere the configured strategy applies. Either way
		/// they are explicit loads for <see cref="LinqOptions.ImplicitCollectionLoading"/>.
		/// </para>
		/// </remarks>
		/// <example>
		/// <code>
		/// var page = db.QueryBatch(() => new
		/// {
		///     Total = products.Count(),
		///     Rows  = products.OrderBy(p => p.Id).Skip(skip).Take(take).ToList(),
		/// });
		/// </code>
		/// </example>
		/// <typeparam name="T">Type of result.</typeparam>
		/// <param name="dataContext">Database connection context.</param>
		/// <param name="selector">The results to read.</param>
		/// <returns>The results.</returns>
		[Pure]
		public static T QueryBatch<T>(
							this IDataContext dataContext,
			[InstantHandle] Expression<Func<T>> selector)
		{
			ArgumentNullException.ThrowIfNull(dataContext);
			ArgumentNullException.ThrowIfNull(selector);

			var q = new ExpressionQueryImpl<T>(dataContext, CreateQueryBatchExpression(dataContext, selector));

			foreach (var item in q)
				return item;

			throw new InvalidOperationException();
		}

		/// <summary>
		/// Runs several reads in one round trip asynchronously and returns their results.
		/// See <see cref="QueryBatch{T}(IDataContext, Expression{Func{T}})"/>.
		/// </summary>
		/// <typeparam name="T">Type of result.</typeparam>
		/// <param name="dataContext">Database connection context.</param>
		/// <param name="selector">The results to read.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>The results.</returns>
		[Pure]
		public static async Task<T> QueryBatchAsync<T>(
							this IDataContext dataContext,
			[InstantHandle] Expression<Func<T>> selector,
							CancellationToken   token = default)
		{
			ArgumentNullException.ThrowIfNull(dataContext);
			ArgumentNullException.ThrowIfNull(selector);

			var q = new ExpressionQueryImpl<T>(dataContext, CreateQueryBatchExpression(dataContext, selector));

			var read = false;
			var item = default(T)!; // this is fine, as we never return it

			await q.ForEachUntilAsync(r =>
			{
				read = true;
				item = r;
				return false;
			}, token).ConfigureAwait(false);

			if (read)
				return item;

			throw new InvalidOperationException();
		}

		/// <summary>
		/// Runs two reads in one round trip. See <see cref="QueryBatch{T}(IDataContext, Expression{Func{T}})"/>.
		/// </summary>
		/// <typeparam name="T1">Type of the first result.</typeparam>
		/// <typeparam name="T2">Type of the second result.</typeparam>
		/// <param name="dataContext">Database connection context.</param>
		/// <param name="result1">First result to read.</param>
		/// <param name="result2">Second result to read.</param>
		/// <returns>The results, in argument order.</returns>
		[Pure]
		public static (T1, T2) QueryBatch<T1, T2>(
							this IDataContext    dataContext,
			[InstantHandle] Expression<Func<T1>> result1,
			[InstantHandle] Expression<Func<T2>> result2)
		{
			return dataContext.QueryBatch(Combine<(T1, T2)>(result1, result2));
		}

		/// <summary>
		/// Runs three reads in one round trip. See <see cref="QueryBatch{T}(IDataContext, Expression{Func{T}})"/>.
		/// </summary>
		/// <typeparam name="T1">Type of the first result.</typeparam>
		/// <typeparam name="T2">Type of the second result.</typeparam>
		/// <typeparam name="T3">Type of the third result.</typeparam>
		/// <param name="dataContext">Database connection context.</param>
		/// <param name="result1">First result to read.</param>
		/// <param name="result2">Second result to read.</param>
		/// <param name="result3">Third result to read.</param>
		/// <returns>The results, in argument order.</returns>
		[Pure]
		public static (T1, T2, T3) QueryBatch<T1, T2, T3>(
							this IDataContext    dataContext,
			[InstantHandle] Expression<Func<T1>> result1,
			[InstantHandle] Expression<Func<T2>> result2,
			[InstantHandle] Expression<Func<T3>> result3)
		{
			return dataContext.QueryBatch(Combine<(T1, T2, T3)>(result1, result2, result3));
		}

		/// <summary>
		/// Runs four reads in one round trip. See <see cref="QueryBatch{T}(IDataContext, Expression{Func{T}})"/>.
		/// </summary>
		/// <typeparam name="T1">Type of the first result.</typeparam>
		/// <typeparam name="T2">Type of the second result.</typeparam>
		/// <typeparam name="T3">Type of the third result.</typeparam>
		/// <typeparam name="T4">Type of the fourth result.</typeparam>
		/// <param name="dataContext">Database connection context.</param>
		/// <param name="result1">First result to read.</param>
		/// <param name="result2">Second result to read.</param>
		/// <param name="result3">Third result to read.</param>
		/// <param name="result4">Fourth result to read.</param>
		/// <returns>The results, in argument order.</returns>
		[Pure]
		public static (T1, T2, T3, T4) QueryBatch<T1, T2, T3, T4>(
							this IDataContext    dataContext,
			[InstantHandle] Expression<Func<T1>> result1,
			[InstantHandle] Expression<Func<T2>> result2,
			[InstantHandle] Expression<Func<T3>> result3,
			[InstantHandle] Expression<Func<T4>> result4)
		{
			return dataContext.QueryBatch(Combine<(T1, T2, T3, T4)>(result1, result2, result3, result4));
		}

		/// <summary>
		/// Runs five reads in one round trip. See <see cref="QueryBatch{T}(IDataContext, Expression{Func{T}})"/>.
		/// </summary>
		/// <typeparam name="T1">Type of the first result.</typeparam>
		/// <typeparam name="T2">Type of the second result.</typeparam>
		/// <typeparam name="T3">Type of the third result.</typeparam>
		/// <typeparam name="T4">Type of the fourth result.</typeparam>
		/// <typeparam name="T5">Type of the fifth result.</typeparam>
		/// <param name="dataContext">Database connection context.</param>
		/// <param name="result1">First result to read.</param>
		/// <param name="result2">Second result to read.</param>
		/// <param name="result3">Third result to read.</param>
		/// <param name="result4">Fourth result to read.</param>
		/// <param name="result5">Fifth result to read.</param>
		/// <returns>The results, in argument order.</returns>
		[Pure]
		public static (T1, T2, T3, T4, T5) QueryBatch<T1, T2, T3, T4, T5>(
							this IDataContext    dataContext,
			[InstantHandle] Expression<Func<T1>> result1,
			[InstantHandle] Expression<Func<T2>> result2,
			[InstantHandle] Expression<Func<T3>> result3,
			[InstantHandle] Expression<Func<T4>> result4,
			[InstantHandle] Expression<Func<T5>> result5)
		{
			return dataContext.QueryBatch(Combine<(T1, T2, T3, T4, T5)>(result1, result2, result3, result4, result5));
		}

		/// <summary>
		/// Runs six reads in one round trip. See <see cref="QueryBatch{T}(IDataContext, Expression{Func{T}})"/>.
		/// </summary>
		/// <typeparam name="T1">Type of the first result.</typeparam>
		/// <typeparam name="T2">Type of the second result.</typeparam>
		/// <typeparam name="T3">Type of the third result.</typeparam>
		/// <typeparam name="T4">Type of the fourth result.</typeparam>
		/// <typeparam name="T5">Type of the fifth result.</typeparam>
		/// <typeparam name="T6">Type of the sixth result.</typeparam>
		/// <param name="dataContext">Database connection context.</param>
		/// <param name="result1">First result to read.</param>
		/// <param name="result2">Second result to read.</param>
		/// <param name="result3">Third result to read.</param>
		/// <param name="result4">Fourth result to read.</param>
		/// <param name="result5">Fifth result to read.</param>
		/// <param name="result6">Sixth result to read.</param>
		/// <returns>The results, in argument order.</returns>
		[Pure]
		public static (T1, T2, T3, T4, T5, T6) QueryBatch<T1, T2, T3, T4, T5, T6>(
							this IDataContext    dataContext,
			[InstantHandle] Expression<Func<T1>> result1,
			[InstantHandle] Expression<Func<T2>> result2,
			[InstantHandle] Expression<Func<T3>> result3,
			[InstantHandle] Expression<Func<T4>> result4,
			[InstantHandle] Expression<Func<T5>> result5,
			[InstantHandle] Expression<Func<T6>> result6)
		{
			return dataContext.QueryBatch(Combine<(T1, T2, T3, T4, T5, T6)>(result1, result2, result3, result4, result5, result6));
		}

		/// <summary>
		/// Runs seven reads in one round trip. See <see cref="QueryBatch{T}(IDataContext, Expression{Func{T}})"/>.
		/// </summary>
		/// <typeparam name="T1">Type of the first result.</typeparam>
		/// <typeparam name="T2">Type of the second result.</typeparam>
		/// <typeparam name="T3">Type of the third result.</typeparam>
		/// <typeparam name="T4">Type of the fourth result.</typeparam>
		/// <typeparam name="T5">Type of the fifth result.</typeparam>
		/// <typeparam name="T6">Type of the sixth result.</typeparam>
		/// <typeparam name="T7">Type of the seventh result.</typeparam>
		/// <param name="dataContext">Database connection context.</param>
		/// <param name="result1">First result to read.</param>
		/// <param name="result2">Second result to read.</param>
		/// <param name="result3">Third result to read.</param>
		/// <param name="result4">Fourth result to read.</param>
		/// <param name="result5">Fifth result to read.</param>
		/// <param name="result6">Sixth result to read.</param>
		/// <param name="result7">Seventh result to read.</param>
		/// <returns>The results, in argument order.</returns>
		[Pure]
		public static (T1, T2, T3, T4, T5, T6, T7) QueryBatch<T1, T2, T3, T4, T5, T6, T7>(
							this IDataContext    dataContext,
			[InstantHandle] Expression<Func<T1>> result1,
			[InstantHandle] Expression<Func<T2>> result2,
			[InstantHandle] Expression<Func<T3>> result3,
			[InstantHandle] Expression<Func<T4>> result4,
			[InstantHandle] Expression<Func<T5>> result5,
			[InstantHandle] Expression<Func<T6>> result6,
			[InstantHandle] Expression<Func<T7>> result7)
		{
			return dataContext.QueryBatch(Combine<(T1, T2, T3, T4, T5, T6, T7)>(result1, result2, result3, result4, result5, result6, result7));
		}

		/// <summary>
		/// Runs two reads in one round trip asynchronously. See <see cref="QueryBatch{T}(IDataContext, Expression{Func{T}})"/>.
		/// </summary>
		/// <typeparam name="T1">Type of the first result.</typeparam>
		/// <typeparam name="T2">Type of the second result.</typeparam>
		/// <param name="dataContext">Database connection context.</param>
		/// <param name="result1">First result to read.</param>
		/// <param name="result2">Second result to read.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>The results, in argument order.</returns>
		[Pure]
		public static Task<(T1, T2)> QueryBatchAsync<T1, T2>(
							this IDataContext    dataContext,
			[InstantHandle] Expression<Func<T1>> result1,
			[InstantHandle] Expression<Func<T2>> result2,
							CancellationToken    token = default)
		{
			return dataContext.QueryBatchAsync(Combine<(T1, T2)>(result1, result2), token);
		}

		/// <summary>
		/// Runs three reads in one round trip asynchronously. See <see cref="QueryBatch{T}(IDataContext, Expression{Func{T}})"/>.
		/// </summary>
		/// <typeparam name="T1">Type of the first result.</typeparam>
		/// <typeparam name="T2">Type of the second result.</typeparam>
		/// <typeparam name="T3">Type of the third result.</typeparam>
		/// <param name="dataContext">Database connection context.</param>
		/// <param name="result1">First result to read.</param>
		/// <param name="result2">Second result to read.</param>
		/// <param name="result3">Third result to read.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>The results, in argument order.</returns>
		[Pure]
		public static Task<(T1, T2, T3)> QueryBatchAsync<T1, T2, T3>(
							this IDataContext    dataContext,
			[InstantHandle] Expression<Func<T1>> result1,
			[InstantHandle] Expression<Func<T2>> result2,
			[InstantHandle] Expression<Func<T3>> result3,
							CancellationToken    token = default)
		{
			return dataContext.QueryBatchAsync(Combine<(T1, T2, T3)>(result1, result2, result3), token);
		}

		/// <summary>
		/// Runs four reads in one round trip asynchronously. See <see cref="QueryBatch{T}(IDataContext, Expression{Func{T}})"/>.
		/// </summary>
		/// <typeparam name="T1">Type of the first result.</typeparam>
		/// <typeparam name="T2">Type of the second result.</typeparam>
		/// <typeparam name="T3">Type of the third result.</typeparam>
		/// <typeparam name="T4">Type of the fourth result.</typeparam>
		/// <param name="dataContext">Database connection context.</param>
		/// <param name="result1">First result to read.</param>
		/// <param name="result2">Second result to read.</param>
		/// <param name="result3">Third result to read.</param>
		/// <param name="result4">Fourth result to read.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>The results, in argument order.</returns>
		[Pure]
		public static Task<(T1, T2, T3, T4)> QueryBatchAsync<T1, T2, T3, T4>(
							this IDataContext    dataContext,
			[InstantHandle] Expression<Func<T1>> result1,
			[InstantHandle] Expression<Func<T2>> result2,
			[InstantHandle] Expression<Func<T3>> result3,
			[InstantHandle] Expression<Func<T4>> result4,
							CancellationToken    token = default)
		{
			return dataContext.QueryBatchAsync(Combine<(T1, T2, T3, T4)>(result1, result2, result3, result4), token);
		}

		/// <summary>
		/// Runs five reads in one round trip asynchronously. See <see cref="QueryBatch{T}(IDataContext, Expression{Func{T}})"/>.
		/// </summary>
		/// <typeparam name="T1">Type of the first result.</typeparam>
		/// <typeparam name="T2">Type of the second result.</typeparam>
		/// <typeparam name="T3">Type of the third result.</typeparam>
		/// <typeparam name="T4">Type of the fourth result.</typeparam>
		/// <typeparam name="T5">Type of the fifth result.</typeparam>
		/// <param name="dataContext">Database connection context.</param>
		/// <param name="result1">First result to read.</param>
		/// <param name="result2">Second result to read.</param>
		/// <param name="result3">Third result to read.</param>
		/// <param name="result4">Fourth result to read.</param>
		/// <param name="result5">Fifth result to read.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>The results, in argument order.</returns>
		[Pure]
		public static Task<(T1, T2, T3, T4, T5)> QueryBatchAsync<T1, T2, T3, T4, T5>(
							this IDataContext    dataContext,
			[InstantHandle] Expression<Func<T1>> result1,
			[InstantHandle] Expression<Func<T2>> result2,
			[InstantHandle] Expression<Func<T3>> result3,
			[InstantHandle] Expression<Func<T4>> result4,
			[InstantHandle] Expression<Func<T5>> result5,
							CancellationToken    token = default)
		{
			return dataContext.QueryBatchAsync(Combine<(T1, T2, T3, T4, T5)>(result1, result2, result3, result4, result5), token);
		}

		/// <summary>
		/// Runs six reads in one round trip asynchronously. See <see cref="QueryBatch{T}(IDataContext, Expression{Func{T}})"/>.
		/// </summary>
		/// <typeparam name="T1">Type of the first result.</typeparam>
		/// <typeparam name="T2">Type of the second result.</typeparam>
		/// <typeparam name="T3">Type of the third result.</typeparam>
		/// <typeparam name="T4">Type of the fourth result.</typeparam>
		/// <typeparam name="T5">Type of the fifth result.</typeparam>
		/// <typeparam name="T6">Type of the sixth result.</typeparam>
		/// <param name="dataContext">Database connection context.</param>
		/// <param name="result1">First result to read.</param>
		/// <param name="result2">Second result to read.</param>
		/// <param name="result3">Third result to read.</param>
		/// <param name="result4">Fourth result to read.</param>
		/// <param name="result5">Fifth result to read.</param>
		/// <param name="result6">Sixth result to read.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>The results, in argument order.</returns>
		[Pure]
		public static Task<(T1, T2, T3, T4, T5, T6)> QueryBatchAsync<T1, T2, T3, T4, T5, T6>(
							this IDataContext    dataContext,
			[InstantHandle] Expression<Func<T1>> result1,
			[InstantHandle] Expression<Func<T2>> result2,
			[InstantHandle] Expression<Func<T3>> result3,
			[InstantHandle] Expression<Func<T4>> result4,
			[InstantHandle] Expression<Func<T5>> result5,
			[InstantHandle] Expression<Func<T6>> result6,
							CancellationToken    token = default)
		{
			return dataContext.QueryBatchAsync(Combine<(T1, T2, T3, T4, T5, T6)>(result1, result2, result3, result4, result5, result6), token);
		}

		/// <summary>
		/// Runs seven reads in one round trip asynchronously. See <see cref="QueryBatch{T}(IDataContext, Expression{Func{T}})"/>.
		/// </summary>
		/// <typeparam name="T1">Type of the first result.</typeparam>
		/// <typeparam name="T2">Type of the second result.</typeparam>
		/// <typeparam name="T3">Type of the third result.</typeparam>
		/// <typeparam name="T4">Type of the fourth result.</typeparam>
		/// <typeparam name="T5">Type of the fifth result.</typeparam>
		/// <typeparam name="T6">Type of the sixth result.</typeparam>
		/// <typeparam name="T7">Type of the seventh result.</typeparam>
		/// <param name="dataContext">Database connection context.</param>
		/// <param name="result1">First result to read.</param>
		/// <param name="result2">Second result to read.</param>
		/// <param name="result3">Third result to read.</param>
		/// <param name="result4">Fourth result to read.</param>
		/// <param name="result5">Fifth result to read.</param>
		/// <param name="result6">Sixth result to read.</param>
		/// <param name="result7">Seventh result to read.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>The results, in argument order.</returns>
		[Pure]
		public static Task<(T1, T2, T3, T4, T5, T6, T7)> QueryBatchAsync<T1, T2, T3, T4, T5, T6, T7>(
							this IDataContext    dataContext,
			[InstantHandle] Expression<Func<T1>> result1,
			[InstantHandle] Expression<Func<T2>> result2,
			[InstantHandle] Expression<Func<T3>> result3,
			[InstantHandle] Expression<Func<T4>> result4,
			[InstantHandle] Expression<Func<T5>> result5,
			[InstantHandle] Expression<Func<T6>> result6,
			[InstantHandle] Expression<Func<T7>> result7,
							CancellationToken    token = default)
		{
			return dataContext.QueryBatchAsync(Combine<(T1, T2, T3, T4, T5, T6, T7)>(result1, result2, result3, result4, result5, result6, result7), token);
		}

		static MethodCallExpression CreateQueryBatchExpression<T>(IDataContext dataContext, Expression<Func<T>> selector)
		{
			return Expression.Call(
				null,
				Methods.LinqToDB.QueryBatch.MakeGenericMethod(typeof(T)),
				SqlQueryRootExpression.Create(dataContext), Expression.Quote(selector));
		}

		static Expression<Func<TTuple>> Combine<TTuple>(params LambdaExpression[] results)
		{
			for (var i = 0; i < results.Length; i++)
			{
				if (results[i] == null)
					throw new ArgumentNullException("result" + (i + 1).ToString(CultureInfo.InvariantCulture));
			}

			return Expression.Lambda<Func<TTuple>>(NewQueryBatchTuple(typeof(TTuple), results.Select(static r => r.Body)));
		}

		// A tuple literal is not allowed in an expression tree (CS8143), so the tuple is built here: one body that
		// constructs the ValueTuple from the result bodies, bound to its Item fields like any projection.
		internal static NewExpression NewQueryBatchTuple(Type tupleType, IEnumerable<Expression> bodies)
		{
			var itemTypes = tupleType.GetGenericArguments();
			var members   = new MemberInfo[itemTypes.Length];

			for (var i = 0; i < members.Length; i++)
				members[i] = tupleType.GetField("Item" + (i + 1).ToString(CultureInfo.InvariantCulture))!;

			return Expression.New(tupleType.GetConstructor(itemTypes)!, bodies, members);
		}
	}
}
