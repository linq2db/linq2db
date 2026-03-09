using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using JetBrains.Annotations;

using LinqToDB.Async;
using LinqToDB.Expressions;
using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.Linq;
using LinqToDB.Internal.Linq.Builder;
using LinqToDB.Internal.Reflection;
using LinqToDB.Linq;
using LinqToDB.Mapping;

using static LinqToDB.MultiInsertExtensions;

namespace LinqToDB
{
	/// <summary>
	/// Contains extension methods for LINQ queries.
	/// </summary>
	[PublicAPI]
	public static partial class LinqExtensions
	{
		#region Scalar Select

		/// <summary>
		/// Loads scalar value or record from database without explicit table source.
		/// Could be usefull for function calls, querying of database variables or properties, subqueries, execution of code on server side.
		/// </summary>
		/// <typeparam name="T">Type of result.</typeparam>
		/// <param name="dataContext">Database connection context.</param>
		/// <param name="selector">Value selection expression.</param>
		/// <returns>Requested value.</returns>
		[Pure]
		public static T Select<T>(
							this IDataContext dataContext,
			[InstantHandle] Expression<Func<T>> selector)
		{
			ArgumentNullException.ThrowIfNull(dataContext);
			ArgumentNullException.ThrowIfNull(selector);

			var expr = Expression.Call(
				null,
				Methods.LinqToDB.Select.MakeGenericMethod(typeof(T)),
				SqlQueryRootExpression.Create(dataContext), Expression.Quote(selector));

			var q = new ExpressionQueryImpl<T>(dataContext, expr);

			foreach (var item in q)
				return item;

			throw new InvalidOperationException();
		}

		/// <summary>
		/// Loads scalar value or record from database without explicit table source asynchronously.
		/// Could be usefull for function calls, querying of database variables or properties, subqueries, execution of code on server side.
		/// </summary>
		/// <typeparam name="T">Type of result.</typeparam>
		/// <param name="dataContext">Database connection context.</param>
		/// <param name="selector">Value selection expression.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Requested value.</returns>
		[Pure]
		public static async Task<T> SelectAsync<T>(
							this IDataContext dataContext,
			[InstantHandle] Expression<Func<T>> selector,
							CancellationToken token = default)
		{
			ArgumentNullException.ThrowIfNull(dataContext);
			ArgumentNullException.ThrowIfNull(selector);

			var expr = Expression.Call(
				null,
				Methods.LinqToDB.Select.MakeGenericMethod(typeof(T)),
				SqlQueryRootExpression.Create(dataContext), Expression.Quote(selector));

			var q = new ExpressionQueryImpl<T>(dataContext, expr);

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

		#endregion

		#region InsertOrUpdate

		static readonly MethodInfo _insertOrUpdateMethodInfo =
			MemberHelper.MethodOf(() => InsertOrUpdate<int>(null!,null!,null!)).GetGenericMethodDefinition();

		/// <summary>
		/// Inserts new record into target table or updates existing record if record with the same primary key value already exists in target table.
		/// When <see langword="null"/> value or expression without field setters passed to <paramref name="onDuplicateKeyUpdateSetter"/>, this method
		/// implements <c>INSERT IF NOT EXISTS</c> logic.
		/// </summary>
		/// <typeparam name="T">Table record type.</typeparam>
		/// <param name="target">Target table.</param>
		/// <param name="insertSetter">Inserted record constructor expression.
		/// Expression supports only target table record new expression with field initializers.</param>
		/// <param name="onDuplicateKeyUpdateSetter">Updated record constructor expression.
		/// Expression supports only target table record new expression with field initializers.
		/// Accepts updated record as parameter.</param>
		/// <returns>Number of affected records.</returns>
		public static int InsertOrUpdate<T>(
							this ITable<T> target,
			[InstantHandle] Expression<Func<T>> insertSetter,
			[InstantHandle] Expression<Func<T, T?>>? onDuplicateKeyUpdateSetter)
			where T : notnull
		{
			ArgumentNullException.ThrowIfNull(target);
			ArgumentNullException.ThrowIfNull(insertSetter);

			var currentSource = target.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				_insertOrUpdateMethodInfo.MakeGenericMethod(typeof(T)),
				currentSource.Expression, Expression.Quote(insertSetter),
				onDuplicateKeyUpdateSetter != null ? Expression.Quote(onDuplicateKeyUpdateSetter) : Expression.Constant(null, typeof(Expression<Func<T, T>>)));

			return currentSource.Execute<int>(expr);
		}

		/// <summary>
		/// Asynchronously inserts new record into target table or updates existing record if record with the same primary key value already exists in target table.
		/// When <see langword="null"/> value or expression without field setters passed to <paramref name="onDuplicateKeyUpdateSetter"/>, this method
		/// implements <c>INSERT IF NOT EXISTS</c> logic.
		/// </summary>
		/// <typeparam name="T">Table record type.</typeparam>
		/// <param name="target">Target table.</param>
		/// <param name="insertSetter">Inserted record constructor expression.
		/// Expression supports only target table record new expression with field initializers.</param>
		/// <param name="onDuplicateKeyUpdateSetter">Updated record constructor expression.
		/// Expression supports only target table record new expression with field initializers.
		/// Accepts updated record as parameter.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Number of affected records.</returns>
		public static Task<int> InsertOrUpdateAsync<T>(
							this ITable<T> target,
			[InstantHandle] Expression<Func<T>> insertSetter,
			[InstantHandle] Expression<Func<T, T?>>? onDuplicateKeyUpdateSetter,
			CancellationToken token = default)
			where T : notnull
		{
			ArgumentNullException.ThrowIfNull(target);
			ArgumentNullException.ThrowIfNull(insertSetter);

			var currentSource = target.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				_insertOrUpdateMethodInfo.MakeGenericMethod(typeof(T)),
				currentSource.Expression, Expression.Quote(insertSetter), onDuplicateKeyUpdateSetter != null ? Expression.Quote(onDuplicateKeyUpdateSetter) : Expression.Constant(null, typeof(Expression<Func<T, T>>)));

			return currentSource.ExecuteAsync<int>(expr, token);
		}

		static readonly MethodInfo _insertOrUpdateMethodInfo2 =
			MemberHelper.MethodOf(() => InsertOrUpdate<int>(null!,null!,null!,null!)).GetGenericMethodDefinition();

		/// <summary>
		/// Inserts new record into target table or updates existing record if record with the same key value already exists in target table.
		/// When <see langword="null"/> value or expression without field setters passed to <paramref name="onDuplicateKeyUpdateSetter"/>, this method
		/// implements <c>INSERT IF NOT EXISTS</c> logic.
		/// </summary>
		/// <typeparam name="T">Table record type.</typeparam>
		/// <param name="target">Target table.</param>
		/// <param name="insertSetter">Inserted record constructor expression.
		/// Expression supports only target table record new expression with field initializers.</param>
		/// <param name="onDuplicateKeyUpdateSetter">Updated record constructor expression.
		/// Expression supports only target table record new expression with field initializers.
		/// Accepts updated record as parameter.</param>
		/// <param name="keySelector">Key fields selector to specify what fields and values must be used as key fields for selection between insert and update operations.
		/// Expression supports only target table record new expression with field initializers for each key field. Assigned key field value will be used as key value by operation type selector.</param>
		/// <returns>Number of affected records.</returns>
		public static int InsertOrUpdate<T>(
							this ITable<T> target,
			[InstantHandle] Expression<Func<T>> insertSetter,
			[InstantHandle] Expression<Func<T, T?>>? onDuplicateKeyUpdateSetter,
			[InstantHandle] Expression<Func<T>> keySelector)
			where T : notnull
		{
			ArgumentNullException.ThrowIfNull(target);
			ArgumentNullException.ThrowIfNull(insertSetter);
			ArgumentNullException.ThrowIfNull(keySelector);

			var currentSource = target.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				_insertOrUpdateMethodInfo2.MakeGenericMethod(typeof(T)),
				currentSource.Expression,
				Expression.Quote(insertSetter),
				onDuplicateKeyUpdateSetter != null ? Expression.Quote(onDuplicateKeyUpdateSetter) : Expression.Constant(null, typeof(Expression<Func<T, T>>)),
				Expression.Quote(keySelector));

			return currentSource.Execute<int>(expr);
		}

		/// <summary>
		/// Asynchronously inserts new record into target table or updates existing record if record with the same key value already exists in target table.
		/// When <see langword="null"/> value or expression without field setters passed to <paramref name="onDuplicateKeyUpdateSetter"/>, this method
		/// implements <c>INSERT IF NOT EXISTS</c> logic.
		/// </summary>
		/// <typeparam name="T">Table record type.</typeparam>
		/// <param name="target">Target table.</param>
		/// <param name="insertSetter">Inserted record constructor expression.
		/// Expression supports only target table record new expression with field initializers.</param>
		/// <param name="onDuplicateKeyUpdateSetter">Updated record constructor expression.
		/// Expression supports only target table record new expression with field initializers.
		/// Accepts updated record as parameter.</param>
		/// <param name="keySelector">Key fields selector to specify what fields and values must be used as key fields for selection between insert and update operations.
		/// Expression supports only target table record new expression with field initializers for each key field. Assigned key field value will be used as key value by operation type selector.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Number of affected records.</returns>
		public static Task<int> InsertOrUpdateAsync<T>(
							this ITable<T> target,
			[InstantHandle] Expression<Func<T>> insertSetter,
			[InstantHandle] Expression<Func<T, T?>>? onDuplicateKeyUpdateSetter,
			[InstantHandle] Expression<Func<T>> keySelector,
			CancellationToken token = default)
			where T : notnull
		{
			ArgumentNullException.ThrowIfNull(target);
			ArgumentNullException.ThrowIfNull(insertSetter);
			ArgumentNullException.ThrowIfNull(keySelector);

			var currentSource = target.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				_insertOrUpdateMethodInfo2.MakeGenericMethod(typeof(T)),
				currentSource.Expression,
				Expression.Quote(insertSetter),
				onDuplicateKeyUpdateSetter != null ? Expression.Quote(onDuplicateKeyUpdateSetter) : Expression.Constant(null, typeof(Expression<Func<T, T>>)),
				Expression.Quote(keySelector));

			return currentSource.ExecuteAsync<int>(expr, token);
		}

		#endregion

		#region Drop

		static readonly MethodInfo _dropMethodInfo2 = MemberHelper.MethodOf(() => Drop<int>(null!, true)).GetGenericMethodDefinition();

		/// <summary>
		/// Drops database table.
		/// </summary>
		/// <typeparam name="T">Table record type.</typeparam>
		/// <param name="target">Dropped table.</param>
		/// <param name="throwExceptionIfNotExists">If <see langword="false"/>, any exception during drop operation will be silently catched and <c>0</c> returned.
		/// This behavior is not correct and will be fixed in future to mask only missing table exceptions.
		/// Tracked by <a href="https://github.com/linq2db/linq2db/issues/798">issue</a>.
		/// Default value: <see langword="true"/>.</param>
		/// <returns>Number of affected records. Usually <c>-1</c> as it is not data modification operation.</returns>
		public static int Drop<T>(this ITable<T> target, bool throwExceptionIfNotExists = true)
			where T : notnull
		{
			ArgumentNullException.ThrowIfNull(target);

			var currentSource = target.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				_dropMethodInfo2.MakeGenericMethod(typeof(T)),
				currentSource.Expression, ExpressionInstances.Boolean(throwExceptionIfNotExists));

			try
			{
				return currentSource.Execute<int>(expr);
			}
			catch when (!throwExceptionIfNotExists)
			{
			}

			return 0;
		}

		/// <summary>
		/// Drops database table asynchronously.
		/// </summary>
		/// <typeparam name="T">Table record type.</typeparam>
		/// <param name="target">Dropped table.</param>
		/// <param name="throwExceptionIfNotExists">If <see langword="false"/>, any exception during drop operation will be silently catched and <c>0</c> returned.
		/// This behavior is not correct and will be fixed in future to mask only missing table exceptions.
		/// Tracked by <a href="https://github.com/linq2db/linq2db/issues/798">issue</a>.
		/// Default value: <see langword="true"/>.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Number of affected records. Usually <c>-1</c> as it is not data modification operation.</returns>
		public static async Task<int> DropAsync<T>(
			this ITable<T> target,
			bool throwExceptionIfNotExists = true,
			CancellationToken token = default)
			where T : notnull
		{
			ArgumentNullException.ThrowIfNull(target);

			var currentSource = target.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				_dropMethodInfo2.MakeGenericMethod(typeof(T)),
				currentSource.Expression, ExpressionInstances.Boolean(throwExceptionIfNotExists));

			try
			{
				return await currentSource.ExecuteAsync<int>(expr, token).ConfigureAwait(false);
			}
			catch when (!throwExceptionIfNotExists)
			{
			}

			return 0;
		}

		#endregion

		#region Truncate

		static readonly MethodInfo _truncateMethodInfo = MemberHelper.MethodOf(() => Truncate<int>(null!, true)).GetGenericMethodDefinition();

		/// <summary>
		/// Truncates database table.
		/// </summary>
		/// <typeparam name="T">Table record type.</typeparam>
		/// <param name="target">Truncated table.</param>
		/// <param name="resetIdentity">Performs reset identity column.</param>
		/// <returns>Number of affected records. Usually <c>-1</c> as it is not data modification operation.</returns>
		public static int Truncate<T>(this ITable<T> target, bool resetIdentity = true)
			where T : notnull
		{
			ArgumentNullException.ThrowIfNull(target);

			var currentSource = target.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				_truncateMethodInfo.MakeGenericMethod(typeof(T)),
				currentSource.Expression, ExpressionInstances.Boolean(resetIdentity));

			return currentSource.Execute<int>(expr);
		}

		/// <summary>
		/// Truncates database table asynchronously.
		/// </summary>
		/// <typeparam name="T">Table record type.</typeparam>
		/// <param name="target">Truncated table.</param>
		/// <param name="resetIdentity">Performs reset identity column.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Number of affected records. Usually <c>-1</c> as it is not data modification operation.</returns>
		public static Task<int> TruncateAsync<T>(
			this ITable<T> target,
			bool resetIdentity = true,
			CancellationToken token = default)
			where T : notnull
		{
			ArgumentNullException.ThrowIfNull(target);

			var currentSource = target.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				_truncateMethodInfo.MakeGenericMethod(typeof(T)),
				currentSource.Expression, ExpressionInstances.Boolean(resetIdentity));

			return currentSource.ExecuteAsync<int>(expr, token);
		}

		#endregion

		#region Take / Skip / ElementAt

		static readonly MethodInfo _takeMethodInfo = MemberHelper.MethodOf(() => Take<int>(null!,null!)).GetGenericMethodDefinition();

		/// <summary>
		/// Limits number of records, returned from query.
		/// </summary>
		/// <typeparam name="TSource">Source table record type.</typeparam>
		/// <param name="source">Source query.</param>
		/// <param name="count">Expression that defines number of records to select.</param>
		/// <returns>Query with limit applied.</returns>
		[LinqTunnel]
		[Pure]
		public static IQueryable<TSource> Take<TSource>(
					   this IQueryable<TSource> source,
			[InstantHandle] Expression<Func<int>> count)
		{
			ArgumentNullException.ThrowIfNull(source);
			ArgumentNullException.ThrowIfNull(count);

			var currentSource = source.ProcessIQueryable();

			var expr = Expression.Call(
				null,
				_takeMethodInfo.MakeGenericMethod(typeof(TSource)),
				currentSource.Expression, Expression.Quote(count));

			return currentSource.Provider.CreateQuery<TSource>(expr);
		}

		static readonly MethodInfo _takeMethodInfo2 = MemberHelper.MethodOf(() => Take<int>(null!,null!,TakeHints.Percent)).GetGenericMethodDefinition();

		/// <summary>
		/// Limits number of records, returned from query. Allows to specify TAKE clause hints.
		/// Using this method may cause runtime <see cref="LinqToDBException"/> if take hints are not supported by database.
		/// </summary>
		/// <typeparam name="TSource">Source table record type.</typeparam>
		/// <param name="source">Source query.</param>
		/// <param name="count">Expression that defines SQL TAKE parameter value.</param>
		/// <param name="hints"><see cref="TakeHints"/> hints for SQL TAKE clause.</param>
		/// <returns>Query with limit applied.</returns>
		[LinqTunnel]
		[Pure]
		public static IQueryable<TSource> Take<TSource>(
								this IQueryable<TSource> source,
			[InstantHandle] Expression<Func<int>> count,
			[SqlQueryDependent] TakeHints hints)
		{
			ArgumentNullException.ThrowIfNull(source);
			ArgumentNullException.ThrowIfNull(count);

			var currentSource = source.ProcessIQueryable();

			var expr = Expression.Call(
				null,
				_takeMethodInfo2.MakeGenericMethod(typeof(TSource)),
				currentSource.Expression, Expression.Quote(count), Expression.Constant(hints));

			return currentSource.Provider.CreateQuery<TSource>(expr);
		}

		static readonly MethodInfo _takeMethodInfo3 = MemberHelper.MethodOf(() => Take<int>(null!,0,TakeHints.Percent)).GetGenericMethodDefinition();

		/// <summary>
		/// Limits number of records, returned from query. Allows to specify TAKE clause hints.
		/// Using this method may cause runtime <see cref="LinqToDBException"/> if take hints are not supported by database.
		/// </summary>
		/// <typeparam name="TSource">Source table record type.</typeparam>
		/// <param name="source">Source query.</param>
		/// <param name="count">SQL TAKE parameter value.</param>
		/// <param name="hints"><see cref="TakeHints"/> hints for SQL TAKE clause.</param>
		/// <returns>Query with limit applied.</returns>
		[LinqTunnel]
		[Pure]
		public static IQueryable<TSource> Take<TSource>(
				  this IQueryable<TSource> source,
								int count,
			[SqlQueryDependent] TakeHints hints)
		{
			ArgumentNullException.ThrowIfNull(source);

			var currentSource = source.ProcessIQueryable();

			var expr = Expression.Call(
				null,
				_takeMethodInfo3.MakeGenericMethod(typeof(TSource)),
				currentSource.Expression, ExpressionInstances.Int32(count), Expression.Constant(hints));

			return currentSource.Provider.CreateQuery<TSource>(expr);
		}

		static readonly MethodInfo _skipMethodInfo = MemberHelper.MethodOf(() => Skip<int>(null!,null!)).GetGenericMethodDefinition();

		/// <summary>
		/// Ignores first N records from source query.
		/// </summary>
		/// <typeparam name="TSource">Source table record type.</typeparam>
		/// <param name="source">Source query.</param>
		/// <param name="count">Expression that defines number of records to skip.</param>
		/// <returns>Query without skipped records.</returns>
		[LinqTunnel]
		[Pure]
		public static IQueryable<TSource> Skip<TSource>(
					   this IQueryable<TSource> source,
			[InstantHandle] Expression<Func<int>> count)
		{
			ArgumentNullException.ThrowIfNull(source);
			ArgumentNullException.ThrowIfNull(count);

			var currentSource = source.ProcessIQueryable();

			var expr = Expression.Call(
				null,
				_skipMethodInfo.MakeGenericMethod(typeof(TSource)),
				currentSource.Expression, Expression.Quote(count));

			return currentSource.Provider.CreateQuery<TSource>(expr);
		}

		/// <summary>
		/// Selects record at specified position from source query.
		/// If query doesn't return enough records, <see cref="InvalidOperationException"/> will be thrown.
		/// </summary>
		/// <typeparam name="TSource">Source table record type.</typeparam>
		/// <param name="source">Source query.</param>
		/// <param name="index">Expression that defines index of record to select.</param>
		/// <exception cref="InvalidOperationException">Source query doesn't have record with specified index.</exception>
		/// <returns>Record at specified position.</returns>
		[Pure]
		public static TSource ElementAt<TSource>(
					   this IQueryable<TSource> source,
			[InstantHandle] Expression<Func<int>> index)
		{
			ArgumentNullException.ThrowIfNull(source);
			ArgumentNullException.ThrowIfNull(index);

			var currentSource = source.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				Methods.LinqToDB.ElementAtLambda.MakeGenericMethod(typeof(TSource)),
				currentSource.Expression, Expression.Quote(index));

			return currentSource.Execute<TSource>(expr);
		}

		/// <summary>
		/// Selects record at specified position from source query asynchronously.
		/// If query doesn't return enough records, <see cref="InvalidOperationException"/> will be thrown.
		/// </summary>
		/// <typeparam name="TSource">Source table record type.</typeparam>
		/// <param name="source">Source query.</param>
		/// <param name="index">Expression that defines index of record to select.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <exception cref="InvalidOperationException">Source query doesn't have record with specified index.</exception>
		/// <returns>Record at specified position.</returns>
		[Pure]
		public static Task<TSource> ElementAtAsync<TSource>(
					   this IQueryable<TSource> source,
			[InstantHandle] Expression<Func<int>> index,
			CancellationToken token = default)
		{
			ArgumentNullException.ThrowIfNull(source);
			ArgumentNullException.ThrowIfNull(index);

			var currentSource = source.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				Methods.LinqToDB.ElementAtLambda.MakeGenericMethod(typeof(TSource)),
				currentSource.Expression, Expression.Quote(index));

			return currentSource.ExecuteAsync<TSource>(expr, token);
		}

		static readonly MethodInfo _elementAtOrDefaultMethodInfo = MemberHelper.MethodOf(() => ElementAtOrDefault<int>(null!,null!)).GetGenericMethodDefinition();

		/// <summary>
		/// Selects record at specified position from source query.
		/// </summary>
		/// <typeparam name="TSource">Source table record type.</typeparam>
		/// <param name="source">Source query.</param>
		/// <param name="index">Expression that defines index of record to select.</param>
		/// <returns>Record at specified position or default value, if source query doesn't have record with such index.</returns>
		[Pure]
		public static TSource ElementAtOrDefault<TSource>(
					   this IQueryable<TSource> source,
			[InstantHandle] Expression<Func<int>> index)
		{
			ArgumentNullException.ThrowIfNull(source);
			ArgumentNullException.ThrowIfNull(index);

			var currentSource = source.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				_elementAtOrDefaultMethodInfo.MakeGenericMethod(typeof(TSource)),
				currentSource.Expression, Expression.Quote(index));

			return currentSource.Execute<TSource>(expr);
		}

		/// <summary>
		/// Selects record at specified position from source query asynchronously.
		/// </summary>
		/// <typeparam name="TSource">Source table record type.</typeparam>
		/// <param name="source">Source query.</param>
		/// <param name="index">Expression that defines index of record to select.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Record at specified position or default value, if source query doesn't have record with such index.</returns>
		[Pure]
		public static Task<TSource> ElementAtOrDefaultAsync<TSource>(
					   this IQueryable<TSource> source,
			[InstantHandle] Expression<Func<int>> index,
							CancellationToken token = default)
		{
			ArgumentNullException.ThrowIfNull(source);
			ArgumentNullException.ThrowIfNull(index);

			var currentSource = source.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				_elementAtOrDefaultMethodInfo.MakeGenericMethod(typeof(TSource)),
				currentSource.Expression, Expression.Quote(index));

			return currentSource.ExecuteAsync<TSource>(expr, token);
		}

		#endregion

		#region Having

		static readonly MethodInfo _setMethodInfo7 = MemberHelper.MethodOf(() => Having((IQueryable<int>)null!,null!)).GetGenericMethodDefinition();

		/// <summary>
		/// Filters source query using HAVING SQL clause.
		/// In general you don't need to use this method as linq2db is able to propely identify current context for
		/// <see cref="Queryable.Where{TSource}(IQueryable{TSource}, Expression{Func{TSource, bool}})"/> method and generate
		/// HAVING clause.
		/// <a href="https://github.com/linq2db/linq2db/issues/133">More details</a>.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <param name="source">Source query to filter.</param>
		/// <param name="predicate">Filtering expression.</param>
		/// <returns>Filtered query.</returns>
		[LinqTunnel]
		[Pure]
		public static IQueryable<TSource> Having<TSource>(
							this IQueryable<TSource> source,
			[InstantHandle] Expression<Func<TSource, bool>> predicate)
		{
			ArgumentNullException.ThrowIfNull(source);
			ArgumentNullException.ThrowIfNull(predicate);

			var currentSource = source.ProcessIQueryable();

			var expr = Expression.Call(
				null,
				_setMethodInfo7.MakeGenericMethod(typeof(TSource)),
				currentSource.Expression, Expression.Quote(predicate));

			return currentSource.Provider.CreateQuery<TSource>(expr);
		}

		#endregion

		#region IOrderedQueryable

		/// <summary>
		/// Adds ascending sort expression to a query.
		/// If query already sorted, existing sorting will be preserved and updated with new sort.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TKey">Sort expression type.</typeparam>
		/// <param name="source">Source query.</param>
		/// <param name="keySelector">Sort expression selector.</param>
		/// <returns>Sorted query.</returns>
		[LinqTunnel]
		[Pure]
		public static IOrderedQueryable<TSource> ThenOrBy<TSource, TKey>(
					   this IQueryable<TSource> source,
			[InstantHandle] Expression<Func<TSource, TKey>> keySelector)
		{
			ArgumentNullException.ThrowIfNull(source);
			ArgumentNullException.ThrowIfNull(keySelector);

			var currentSource = source.ProcessIQueryable();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(ThenOrBy, source, keySelector),
				currentSource.Expression, Expression.Quote(keySelector));

			return (IOrderedQueryable<TSource>)currentSource.Provider.CreateQuery<TSource>(expr);
		}

		/// <summary>
		/// Adds descending sort expression to a query.
		/// If query already sorted, existing sorting will be preserved and updated with new sort.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TKey">Sort expression type.</typeparam>
		/// <param name="source">Source query.</param>
		/// <param name="keySelector">Sort expression selector.</param>
		/// <returns>Sorted query.</returns>
		[LinqTunnel]
		[Pure]
		public static IOrderedQueryable<TSource> ThenOrByDescending<TSource, TKey>(
					   this IQueryable<TSource> source,
			[InstantHandle] Expression<Func<TSource, TKey>> keySelector)
		{
			ArgumentNullException.ThrowIfNull(source);
			ArgumentNullException.ThrowIfNull(keySelector);

			var currentSource = source.ProcessIQueryable();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(ThenOrByDescending, source, keySelector),
				currentSource.Expression, Expression.Quote(keySelector));

			return (IOrderedQueryable<TSource>)currentSource.Provider.CreateQuery<TSource>(expr);
		}

		/// <summary>
		/// Removes ordering from current query.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <param name="source">Source query.</param>
		/// <returns>Unsorted query.</returns>
		[LinqTunnel]
		[Pure]
		public static IQueryable<TSource> RemoveOrderBy<TSource>(this IQueryable<TSource> source)
		{
			ArgumentNullException.ThrowIfNull(source);

			var currentSource = source.ProcessIQueryable();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(RemoveOrderBy, source), currentSource.Expression);

			return currentSource.Provider.CreateQuery<TSource>(expr);
		}

		#endregion

		#region SqlJoin

		/// <summary>
		/// Defines inner or outer join between two sub-queries or tables.
		/// </summary>
		/// <typeparam name="TSource">Type of record for right join operand.</typeparam>
		/// <param name="source">Right join operand.</param>
		/// <param name="joinType">Type of join.</param>
		/// <param name="predicate">Join predicate.</param>
		/// <returns>Right operand.</returns>
		[Pure]
		[LinqTunnel]
		public static IQueryable<TSource> Join<TSource>(
					   this IQueryable<TSource> source,
			[SqlQueryDependent] SqlJoinType joinType,
			[InstantHandle] Expression<Func<TSource, bool>> predicate)
		{
			ArgumentNullException.ThrowIfNull(source);
			ArgumentNullException.ThrowIfNull(predicate);

			var currentSource = source.ProcessIQueryable();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(Join, source, joinType, predicate),
				currentSource.Expression,
				Expression.Constant(joinType),
				Expression.Quote(predicate));

			return currentSource.Provider.CreateQuery<TSource>(expr);
		}

		/// <summary>
		/// Defines inner or outer join between two sub-queries or tables.
		/// </summary>
		/// <typeparam name="TOuter">Type of record for left join operand.</typeparam>
		/// <typeparam name="TInner">Type of record for right join operand.</typeparam>
		/// <typeparam name="TResult">The type of the result elements.</typeparam>
		/// <param name="outer">Left join operand.</param>
		/// <param name="inner">Right join operand.</param>
		/// <param name="joinType">Type of join.</param>
		/// <param name="predicate">Join predicate.</param>
		/// <param name="resultSelector">A function to create a result element from two matching elements.</param>
		/// <returns>Right operand.</returns>
		[Pure]
		[LinqTunnel]
		public static IQueryable<TResult> Join<TOuter, TInner, TResult>(
						   this IQueryable<TOuter> outer,
								IQueryable<TInner> inner,
			[SqlQueryDependent] SqlJoinType joinType,
			[InstantHandle] Expression<Func<TOuter, TInner, bool>> predicate,
			[InstantHandle] Expression<Func<TOuter, TInner, TResult>> resultSelector)
		{
			ArgumentNullException.ThrowIfNull(outer);
			ArgumentNullException.ThrowIfNull(inner);
			ArgumentNullException.ThrowIfNull(predicate);
			ArgumentNullException.ThrowIfNull(resultSelector);

			var currentSource = outer.ProcessIQueryable();

			var expr = Expression.Call(
				null,
				Methods.LinqToDB.JoinTypePredicateSelector.MakeGenericMethod(typeof(TOuter), typeof(TInner), typeof(TResult)),
				currentSource.Expression,
				inner.Expression,
				Expression.Constant(joinType),
				Expression.Quote(predicate),
				Expression.Quote(resultSelector));

			return currentSource.Provider.CreateQuery<TResult>(expr);
		}

		/// <summary>
		/// Defines inner join between two sub-queries or tables.
		/// </summary>
		/// <typeparam name="TSource">Type of record for right join operand.</typeparam>
		/// <param name="source">Right join operand.</param>
		/// <param name="predicate">Join predicate.</param>
		/// <returns>Right operand.</returns>
		[Pure]
		[LinqTunnel]
		public static IQueryable<TSource> InnerJoin<TSource>(
					   this IQueryable<TSource> source,
			[InstantHandle] Expression<Func<TSource, bool>> predicate)
		{
			return Join(source, SqlJoinType.Inner, predicate);
		}

		/// <summary>
		/// Defines inner or outer join between two sub-queries or tables.
		/// </summary>
		/// <typeparam name="TOuter">Type of record for left join operand.</typeparam>
		/// <typeparam name="TInner">Type of record for right join operand.</typeparam>
		/// <typeparam name="TResult">The type of the result elements.</typeparam>
		/// <param name="outer">Left join operand.</param>
		/// <param name="inner">Right join operand.</param>
		/// <param name="predicate">Join predicate.</param>
		/// <param name="resultSelector">A function to create a result element from two matching elements.</param>
		/// <returns>Right operand.</returns>
		[Pure]
		[LinqTunnel]
		public static IQueryable<TResult> InnerJoin<TOuter, TInner, TResult>(
					   this IQueryable<TOuter> outer,
							IQueryable<TInner> inner,
			[InstantHandle] Expression<Func<TOuter, TInner, bool>> predicate,
			[InstantHandle] Expression<Func<TOuter, TInner, TResult>> resultSelector)
		{
			return Join(outer, inner, SqlJoinType.Inner, predicate, resultSelector);
		}

		/// <summary>
		/// Defines left outer join between two sub-queries or tables.
		/// </summary>
		/// <typeparam name="TSource">Type of record for right join operand.</typeparam>
		/// <param name="source">Right join operand.</param>
		/// <param name="predicate">Join predicate.</param>
		/// <returns>Right operand.</returns>
		[Pure]
		[LinqTunnel]
		public static IQueryable<TSource> LeftJoin<TSource>(
					   this IQueryable<TSource> source,
			[InstantHandle] Expression<Func<TSource, bool>> predicate)
		{
			return Join(source, SqlJoinType.Left, predicate);
		}

		/// <summary>
		/// Defines left outer join between two sub-queries or tables.
		/// </summary>
		/// <typeparam name="TOuter">Type of record for left join operand.</typeparam>
		/// <typeparam name="TInner">Type of record for right join operand.</typeparam>
		/// <typeparam name="TResult">The type of the result elements.</typeparam>
		/// <param name="outer">Left join operand.</param>
		/// <param name="inner">Right join operand.</param>
		/// <param name="predicate">Join predicate.</param>
		/// <param name="resultSelector">A function to create a result element from two matching elements.</param>
		/// <returns>Right operand.</returns>
		[Pure]
		[LinqTunnel]
		public static IQueryable<TResult> LeftJoin<TOuter, TInner, TResult>(
					   this IQueryable<TOuter> outer,
							IQueryable<TInner> inner,
			[InstantHandle] Expression<Func<TOuter, TInner, bool>> predicate,
			[InstantHandle] Expression<Func<TOuter, TInner, TResult>> resultSelector)
		{
			return Join(outer, inner, SqlJoinType.Left, predicate, resultSelector);
		}

		/// <summary>
		/// Defines right outer join between two sub-queries or tables.
		/// </summary>
		/// <typeparam name="TSource">Type of record for right join operand.</typeparam>
		/// <param name="source">Right join operand.</param>
		/// <param name="predicate">Join predicate.</param>
		/// <returns>Right operand.</returns>
		[Pure]
		[LinqTunnel]
		public static IQueryable<TSource> RightJoin<TSource>(
					   this IQueryable<TSource> source,
			[InstantHandle] Expression<Func<TSource, bool>> predicate)
		{
			return Join(source, SqlJoinType.Right, predicate);
		}

		/// <summary>
		/// Defines right outer join between two sub-queries or tables.
		/// </summary>
		/// <typeparam name="TOuter">Type of record for left join operand.</typeparam>
		/// <typeparam name="TInner">Type of record for right join operand.</typeparam>
		/// <typeparam name="TResult">The type of the result elements.</typeparam>
		/// <param name="outer">Left join operand.</param>
		/// <param name="inner">Right join operand.</param>
		/// <param name="predicate">Join predicate.</param>
		/// <param name="resultSelector">A function to create a result element from two matching elements.</param>
		/// <returns>Right operand.</returns>
		[Pure]
		[LinqTunnel]
		public static IQueryable<TResult> RightJoin<TOuter, TInner, TResult>(
					   this IQueryable<TOuter> outer,
							IQueryable<TInner> inner,
			[InstantHandle] Expression<Func<TOuter, TInner, bool>> predicate,
			[InstantHandle] Expression<Func<TOuter, TInner, TResult>> resultSelector)
		{
			return Join(outer, inner, SqlJoinType.Right, predicate, resultSelector);
		}

		/// <summary>
		/// Defines full outer join between two sub-queries or tables.
		/// </summary>
		/// <typeparam name="TSource">Type of record for right join operand.</typeparam>
		/// <param name="source">Right join operand.</param>
		/// <param name="predicate">Join predicate.</param>
		/// <returns>Right operand.</returns>
		[Pure]
		[LinqTunnel]
		public static IQueryable<TSource> FullJoin<TSource>(
					   this IQueryable<TSource> source,
			[InstantHandle] Expression<Func<TSource, bool>> predicate)
		{
			return Join(source, SqlJoinType.Full, predicate);
		}

		/// <summary>
		/// Defines full outer join between two sub-queries or tables.
		/// </summary>
		/// <typeparam name="TOuter">Type of record for left join operand.</typeparam>
		/// <typeparam name="TInner">Type of record for right join operand.</typeparam>
		/// <typeparam name="TResult">The type of the result elements.</typeparam>
		/// <param name="outer">Left join operand.</param>
		/// <param name="inner">Right join operand.</param>
		/// <param name="predicate">Join predicate.</param>
		/// <param name="resultSelector">A function to create a result element from two matching elements.</param>
		/// <returns>Right operand.</returns>
		[Pure]
		[LinqTunnel]
		public static IQueryable<TResult> FullJoin<TOuter, TInner, TResult>(
					   this IQueryable<TOuter> outer,
							IQueryable<TInner> inner,
			[InstantHandle] Expression<Func<TOuter, TInner, bool>> predicate,
			[InstantHandle] Expression<Func<TOuter, TInner, TResult>> resultSelector)
		{
			return Join(outer, inner, SqlJoinType.Full, predicate, resultSelector);
		}

		/// <summary>
		/// Defines cross join between two sub-queries or tables.
		/// </summary>
		/// <typeparam name="TOuter">Type of record for left join operand.</typeparam>
		/// <typeparam name="TInner">Type of record for right join operand.</typeparam>
		/// <typeparam name="TResult">The type of the result elements.</typeparam>
		/// <param name="outer">Left join operand.</param>
		/// <param name="inner">Right join operand.</param>
		/// <param name="resultSelector">A function to create a result element from two matching elements.</param>
		/// <returns>Right operand.</returns>
		[Pure]
		[LinqTunnel]
		public static IQueryable<TResult> CrossJoin<TOuter, TInner, TResult>(
					   this IQueryable<TOuter> outer,
							IQueryable<TInner> inner,
			[InstantHandle] Expression<Func<TOuter, TInner, TResult>> resultSelector)
		{
			ArgumentNullException.ThrowIfNull(outer);
			ArgumentNullException.ThrowIfNull(inner);
			ArgumentNullException.ThrowIfNull(resultSelector);

			var currentSource = outer.ProcessIQueryable();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(CrossJoin, outer, inner, resultSelector),
				currentSource.Expression,
				inner.Expression,
				Expression.Quote(resultSelector));

			return currentSource.Provider.CreateQuery<TResult>(expr);
		}

		#endregion

		#region CTE

		/// <summary>
		/// Specifies a temporary named result set, known as a common table expression (CTE).
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <param name="source">Source query.</param>
		/// <returns>Common table expression.</returns>
		[Pure]
		[LinqTunnel]
		public static IQueryable<TSource> AsCte<TSource>(this IQueryable<TSource> source)
		{
			ArgumentNullException.ThrowIfNull(source);

			var currentSource = source.ProcessIQueryable();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(AsCte, source),
				currentSource.Expression);

			return currentSource.Provider.CreateQuery<TSource>(expr);
		}

		/// <summary>
		/// Specifies a temporary named result set, known as a common table expression (CTE).
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <param name="source">Source query.</param>
		/// <param name="name">Common table expression name.</param>
		/// <returns>Common table expression.</returns>
		[Pure]
		[LinqTunnel]
		public static IQueryable<TSource> AsCte<TSource>(
			this IQueryable<TSource> source,
			string? name)
		{
			ArgumentNullException.ThrowIfNull(source);

			var currentSource = source.ProcessIQueryable();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(AsCte, source, name),
				currentSource.Expression, Expression.Constant(name ?? string.Empty));

			return currentSource.Provider.CreateQuery<TSource>(expr);
		}

		#endregion

		#region AsQueryable

		/// <summary>Converts a generic <see cref="IEnumerable{T}" /> to Linq To DB query.</summary>
		/// <param name="source">A sequence to convert.</param>
		/// <param name="dataContext">Database connection context.</param>
		/// <typeparam name="TElement">The type of the elements of <paramref name="source" />.</typeparam>
		/// <returns>An <see cref="IQueryable{T}" /> that represents the input sequence.</returns>
		/// <exception cref="ArgumentNullException">
		/// <paramref name="source" /> is <see langword="null" />.</exception>
		public static IQueryable<TElement> AsQueryable<TElement>(
			this IEnumerable<TElement> source,
			IDataContext dataContext)
		{
			ArgumentNullException.ThrowIfNull(source);
			ArgumentNullException.ThrowIfNull(dataContext);

			if (source is IQueryable<TElement> already)
				return (IQueryable<TElement>)(ProcessSourceQueryable?.Invoke(already) ?? already);

			var query = new ExpressionQueryImpl<TElement>(dataContext,
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(AsQueryable, source, dataContext),
					Expression.Constant(source),
					SqlQueryRootExpression.Create(dataContext)
				));

			return query;
		}

		#endregion

		#region AsSubQuery

		/// <summary>
		/// Defines that sub-query is mandatory for <paramref name="source"/> query and cannot be removed during the query optimization.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <param name="source">Source data query.</param>
		/// <returns>Query converted into sub-query.</returns>
		[Pure]
		[LinqTunnel]
		public static IQueryable<TSource> AsSubQuery<TSource>(this IQueryable<TSource> source)
		{
			ArgumentNullException.ThrowIfNull(source);

			var currentSource = source.ProcessIQueryable();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(AsSubQuery, source), source.Expression);

			return currentSource.Provider.CreateQuery<TSource>(expr);
		}

		/// <summary>
		/// Defines that sub-query is mandatory for <paramref name="grouping"/> query and cannot be removed during the query optimization.
		/// </summary>
		/// <typeparam name="TKey">The type of the key of the <see cref="IGrouping{TKey, TElement}" />.</typeparam>
		/// <typeparam name="TElement">The type of the values in the <see cref="IGrouping{TKey, TElement}" />.</typeparam>
		/// <param name="grouping">Source data query.</param>
		/// <returns>Query converted into sub-query.</returns>
		[Pure]
		[LinqTunnel]
		public static IQueryable<TKey> AsSubQuery<TKey, TElement>(this IQueryable<IGrouping<TKey, TElement>> grouping)
		{
			ArgumentNullException.ThrowIfNull(grouping);

			var currentSource = grouping.ProcessIQueryable();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(AsSubQuery, grouping),
				currentSource.Expression);

			return currentSource.Provider.CreateQuery<TKey>(expr);
		}

		/// <summary>
		/// Defines that sub-query is mandatory for <paramref name="source"/> query and cannot be removed during the query optimization.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <param name="source">Source data query.</param>
		/// <param name="queryName">Query name.</param>
		/// <returns>Query converted into sub-query.</returns>
		[Pure]
		[LinqTunnel]
		public static IQueryable<TSource> AsSubQuery<TSource>(this IQueryable<TSource> source, [SqlQueryDependent] string queryName)
		{
			ArgumentNullException.ThrowIfNull(source);

			var currentSource = source.ProcessIQueryable();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(AsSubQuery, source, queryName),
				currentSource.Expression,
				Expression.Constant(queryName));

			return currentSource.Provider.CreateQuery<TSource>(expr);
		}

		/// <summary>
		/// Defines that sub-query is mandatory for <paramref name="grouping"/> query and cannot be removed during the query optimization.
		/// </summary>
		/// <typeparam name="TKey">The type of the key of the <see cref="IGrouping{TKey, TElement}" />.</typeparam>
		/// <typeparam name="TElement">The type of the values in the <see cref="IGrouping{TKey, TElement}" />.</typeparam>
		/// <param name="grouping">Source data query.</param>
		/// <param name="queryName">Query name.</param>
		/// <returns>Query converted into sub-query.</returns>
		[Pure]
		[LinqTunnel]
		public static IQueryable<TKey> AsSubQuery<TKey, TElement>(this IQueryable<IGrouping<TKey, TElement>> grouping, [SqlQueryDependent] string queryName)
		{
			ArgumentNullException.ThrowIfNull(grouping);

			var currentSource = grouping.ProcessIQueryable();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(AsSubQuery, grouping, queryName),
				currentSource.Expression,
				Expression.Constant(queryName));

			return currentSource.Provider.CreateQuery<TKey>(expr);
		}

		#endregion

		#region QueryName

		/// <summary>
		/// Defines query name for specified sub-query. The query cannot be removed during the query optimization.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <param name="source">Source data query.</param>
		/// <param name="queryName">Query name.</param>
		/// <returns>Query converted into sub-query.</returns>
		[Pure]
		[LinqTunnel]
		public static IQueryable<TSource> QueryName<TSource>(this IQueryable<TSource> source, [SqlQueryDependent] string queryName)
		{
			ArgumentNullException.ThrowIfNull(source);

			var currentSource = source.ProcessIQueryable();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(QueryName, source, queryName),
				currentSource.Expression,
				Expression.Constant(queryName));

			return currentSource.Provider.CreateQuery<TSource>(expr);
		}

		/// <summary>
		/// Defines query name for specified sub-query. The query cannot be removed during the query optimization.
		/// </summary>
		/// <typeparam name="TKey">The type of the key of the <see cref="IGrouping{TKey, TElement}" />.</typeparam>
		/// <typeparam name="TElement">The type of the values in the <see cref="IGrouping{TKey, TElement}" />.</typeparam>
		/// <param name="grouping">Source data query.</param>
		/// <param name="queryName">Query name.</param>
		/// <returns>Query converted into sub-query.</returns>
		[Pure]
		[LinqTunnel]
		public static IQueryable<TKey> QueryName<TKey, TElement>(this IQueryable<IGrouping<TKey, TElement>> grouping, [SqlQueryDependent] string queryName)
		{
			ArgumentNullException.ThrowIfNull(grouping);

			var currentSource = grouping.ProcessIQueryable();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(QueryName, grouping, queryName),
				currentSource.Expression,
				Expression.Constant(queryName));

			return currentSource.Provider.CreateQuery<TKey>(expr);
		}

		#endregion

		#region InlineParameters

		/// <summary>
		/// Inline parameters in query which can be converted to SQL Literal.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <param name="source">Source data query.</param>
		/// <returns>Query with inlined parameters.</returns>
		[Pure]
		[LinqTunnel]
		public static IQueryable<TSource> InlineParameters<TSource>(this IQueryable<TSource> source)
		{
			ArgumentNullException.ThrowIfNull(source);

			var currentSource = source.ProcessIQueryable();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(InlineParameters, source),
				currentSource.Expression);

			return currentSource.Provider.CreateQuery<TSource>(expr);
		}

		#endregion

		#region Disable Grouping Guard

		/// <summary>
		/// Disables grouping guard for particular <paramref name="grouping"/> query.
		/// </summary>
		/// <typeparam name="TKey">The type of the key of the <see cref="IGrouping{TKey, TElement}" />.</typeparam>
		/// <typeparam name="TElement">The type of the values in the <see cref="IGrouping{TKey, TElement}" />.</typeparam>
		/// <param name="grouping">Source data query.</param>
		/// <returns>Query with suppressed grouping guard.</returns>
		[Pure]
		[LinqTunnel]
		public static IQueryable<IGrouping<TKey, TElement>> DisableGuard<TKey, TElement>(this IQueryable<IGrouping<TKey, TElement>> grouping)
		{
			ArgumentNullException.ThrowIfNull(grouping);

			var currentSource = grouping.ProcessIQueryable();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(DisableGuard, grouping),
				currentSource.Expression);

			return currentSource.Provider.CreateQuery<IGrouping<TKey, TElement>>(expr);
		}

		#endregion

		#region HasUniqueKey

		/// <summary>
		/// Records unique key for IQueryable. It allows sub-query to be optimized out in LEFT JOIN if columns from sub-query are not used in final projection and predicate.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TKey">Key type.</typeparam>
		/// <param name="source">Source data query.</param>
		/// <param name="keySelector">A function to specify which fields are unique.</param>
		/// <returns>Query converted into sub-query.</returns>
		[Pure]
		[LinqTunnel]
		public static IQueryable<TSource> HasUniqueKey<TSource, TKey>(
			 this IQueryable<TSource> source,
				  Expression<Func<TSource, TKey>> keySelector)
		{
			ArgumentNullException.ThrowIfNull(source);
			ArgumentNullException.ThrowIfNull(keySelector);

			var currentSource = source.ProcessIQueryable();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(HasUniqueKey, source, keySelector),
				currentSource.Expression,
				Expression.Quote(keySelector));

			return currentSource.Provider.CreateQuery<TSource>(expr);
		}

		#endregion

		#region Set operators

		static Expression GetSourceExpression<TSource>(IEnumerable<TSource> source)
		{
			return source switch
			{
				IQueryable<TSource> queryable => queryable.Expression,
				_ => Expression.Constant(source, typeof(IEnumerable<TSource>)),
			};
		}

		/// <summary>Concatenates two sequences, similar to <see cref="Queryable.Concat{TSource}"/>.</summary>
		/// <param name="source1">The first sequence to concatenate.</param>
		/// <param name="source2">The sequence to concatenate to the first sequence.</param>
		/// <typeparam name="TSource">The type of the elements of the input sequences.</typeparam>
		/// <returns>An <see cref="IQueryable{T}" /> that contains the concatenated elements of the two input sequences.</returns>
		/// <exception cref="ArgumentNullException">
		/// <paramref name="source1" /> or <paramref name="source2" /> is <see langword="null" />.</exception>
		public static IQueryable<TSource> UnionAll<TSource>(
			 this IQueryable<TSource> source1,
				  IEnumerable<TSource> source2)
		{
			ArgumentNullException.ThrowIfNull(source1);
			ArgumentNullException.ThrowIfNull(source2);

			return source1.Concat(source2);
		}

		/// <summary>Produces the set difference of two sequences.</summary>
		/// <param name="source1">An <see cref="IQueryable{T}" /> whose elements that are not also in <paramref name="source2" /> will be returned.</param>
		/// <param name="source2">An <see cref="IEnumerable{T}" /> whose elements that also occur in the first sequence will not appear in the returned sequence.</param>
		/// <typeparam name="TSource">The type of the elements of the input sequences.</typeparam>
		/// <returns>An <see cref="IQueryable{T}" /> that contains the set difference of the two sequences.</returns>
		/// <exception cref="ArgumentNullException">
		/// <paramref name="source1" /> or <paramref name="source2" /> is <see langword="null" />.</exception>
		public static IQueryable<TSource> ExceptAll<TSource>(
			 this IQueryable<TSource> source1,
				  IEnumerable<TSource> source2)
		{
			ArgumentNullException.ThrowIfNull(source1);
			ArgumentNullException.ThrowIfNull(source2);

			var currentSource = source1.ProcessIQueryable();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(ExceptAll, source1, source2),
				currentSource.Expression,
				GetSourceExpression(source2));

			return currentSource.Provider.CreateQuery<TSource>(expr);
		}

		/// <summary>Produces the set intersection of two sequences.</summary>
		/// <param name="source1">A sequence whose elements that also appear in <paramref name="source2" /> are returned.</param>
		/// <param name="source2">A sequence whose elements that also appear in the first sequence are returned.</param>
		/// <typeparam name="TSource">The type of the elements of the input sequences.</typeparam>
		/// <returns>A sequence that contains the set intersection of the two sequences.</returns>
		/// <exception cref="ArgumentNullException">
		/// <paramref name="source1" /> or <paramref name="source2" /> is <see langword="null" />.</exception>
		public static IQueryable<TSource> IntersectAll<TSource>(
			 this IQueryable<TSource> source1,
				  IEnumerable<TSource> source2)
		{
			ArgumentNullException.ThrowIfNull(source1);
			ArgumentNullException.ThrowIfNull(source2);

			var currentSource = source1.ProcessIQueryable();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(IntersectAll, source1, source2),
				currentSource.Expression,
				GetSourceExpression(source2));

			return currentSource.Provider.CreateQuery<TSource>(expr);
		}

		#endregion

		#region Query Filters

		/// <summary>
		/// Disables Query Filters in current query.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <param name="source">Source query.</param>
		/// <param name="entityTypes">Optional types with which filters should be disabled.</param>
		/// <returns>Query with disabled filters.</returns>
		[LinqTunnel]
		[Pure]
		public static IQueryable<TSource> IgnoreFilters<TSource>(this IQueryable<TSource> source, [SqlQueryDependent] params Type[] entityTypes)
		{
			ArgumentNullException.ThrowIfNull(source);

			var currentSource = source.ProcessIQueryable();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(IgnoreFilters, source, entityTypes), currentSource.Expression, Expression.Constant(entityTypes));

			return currentSource.Provider.CreateQuery<TSource>(expr);
		}

		#endregion

		#region Tests

		/// <summary>
		/// Generates test source code for specified query.
		/// This method could be usefull to debug queries and attach test code to linq2db issue reports.
		/// </summary>
		/// <param name="query">Query to test.</param>
		/// <param name="mangleNames">Should we use real names for used types, members and namespace or generate obfuscated names.</param>
		/// <returns>Test source code.</returns>
		public static string GenerateTestString<T>(this IQueryable<T> query, bool mangleNames = false)
		{
			return ExpressionTestGenerator.GenerateSourceString(
				Internals.GetDataContext(query) ?? throw new ArgumentException("Query is not a Linq To DB query", nameof(query)),
				query.Expression,
				mangleNames);
		}

		#endregion

		#region Queryable Helpers

		/// <summary>
		/// Gets or sets callback for preprocessing query before execution.
		/// Useful for intercepting queries.
		/// </summary>
		public static Func<IQueryable, IQueryable>? ProcessSourceQueryable { get; set; }

		public static IExtensionsAdapter? ExtensionsAdapter { get; set; }

		#endregion

		#region Tag

		/// <summary>
		/// Adds a tag comment before generated query.
		/// <code>
		/// The example below will produce following code before generated query: /* my tag */\r\n
		/// db.Table.TagQuery("my tag");
		/// </code>
		/// </summary>
		/// <typeparam name="TSource">Table record mapping class.</typeparam>
		/// <param name="source">Source data query.</param>
		/// <param name="tagValue">Tag text to be added as comment before generated query.</param>
		/// <returns>Query with tag.</returns>
		[LinqTunnel]
		[Pure]
		public static IQueryable<TSource> TagQuery<TSource>(this IQueryable<TSource> source, [SqlQueryDependent] string tagValue)
		{
			ArgumentNullException.ThrowIfNull(source);
			ArgumentNullException.ThrowIfNull(tagValue);

			var currentSource = source.ProcessIQueryable();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(TagQuery, source, tagValue),
				source.Expression,
				Expression.Constant(tagValue));

			return currentSource.Provider.CreateQuery<TSource>(expr);
		}

		/// <summary>
		/// Adds a tag comment before generated query for table.
		/// <code>
		/// The example below will produce following code before generated query: /* my tag */\r\n
		/// db.Table.TagQuery("my tag");
		/// </code>
		/// </summary>
		/// <typeparam name="T">Table record mapping class.</typeparam>
		/// <param name="table">Table-like query source.</param>
		/// <param name="tagValue">Tag text to be added as comment before generated query.</param>
		/// <returns>Table-like query source with tag.</returns>
		[LinqTunnel]
		[Pure]
		public static ITable<T> TagQuery<T>(this ITable<T> table, [SqlQueryDependent] string tagValue) where T : notnull
		{
			ArgumentNullException.ThrowIfNull(table);
			ArgumentNullException.ThrowIfNull(tagValue);

			var newTable = new Table<T>(table.DataContext,
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(TagQuery, table, tagValue),
					table.Expression, Expression.Constant(tagValue))
			);

			return newTable;
		}

		#endregion

		#region ToSqlQuery
		/// <summary>
		/// Convert Linq To DB query to SQL command with parameters.
		/// </summary>
		/// <remarks>Eager load queries currently return only SQL for main query.</remarks>
		public static QuerySql ToSqlQuery<T>(this IQueryable<T> query, SqlGenerationOptions? options = null)
		{
			if (query is LoadWithQueryableBase<T> loadWith)
				query = loadWith.Query;

			var expressionQuery = (IExpressionQuery)query.GetLinqToDBSource();

			// currently we have only non-linq APIs that could generate multiple commands like
			// InsertOrReplace, CreateTable, DropTable
			return expressionQuery.GetSqlQueries(options)[0];
		}

		/// <summary>
		/// Convert Linq To DB query to SQL command with parameters.
		/// </summary>
		public static QuerySql ToSqlQuery<T>(this IUpdatable<T> query, SqlGenerationOptions? options = null)
		{
			var expressionQuery = (IExpressionQuery)((Updatable<T>)query).Query.GetLinqToDBSource();

			return expressionQuery.GetSqlQueries(options)[0];
		}

		/// <summary>
		/// Convert Linq To DB query to SQL command with parameters.
		/// </summary>
		public static QuerySql ToSqlQuery<T>(this IValueInsertable<T> query, SqlGenerationOptions? options = null)
		{
			var expressionQuery = (IExpressionQuery)((ValueInsertable<T>)query).Query.GetLinqToDBSource();

			return expressionQuery.GetSqlQueries(options)[0];
		}

		/// <summary>
		/// Convert Linq To DB query to SQL command with parameters.
		/// </summary>
		public static QuerySql ToSqlQuery<TSource, TTarget>(this ISelectInsertable<TSource, TTarget> query, SqlGenerationOptions? options = null)
		{
			var expressionQuery = (IExpressionQuery)((SelectInsertable<TSource, TTarget>)query).Query.GetLinqToDBSource();

			return expressionQuery.GetSqlQueries(options)[0];
		}

		/// <summary>
		/// Convert Linq To DB query to SQL command with parameters.
		/// </summary>
		/// <remarks>Eager load queries currently return only SQL for main query.</remarks>
		public static QuerySql ToSqlQuery<TSource>(this IMultiInsertInto<TSource> query, SqlGenerationOptions? options = null)
		{
			var expressionQuery = (IExpressionQuery)((MultiInsertQuery<TSource>)query).Query.GetLinqToDBSource();

			return expressionQuery.GetSqlQueries(options)[0];
		}

		/// <summary>
		/// Convert Linq To DB query to SQL command with parameters.
		/// </summary>
		/// <remarks>Eager load queries currently return only SQL for main query.</remarks>
		public static QuerySql ToSqlQuery<TSource>(this IMultiInsertElse<TSource> query, SqlGenerationOptions? options = null)
		{
			var expressionQuery = (IExpressionQuery)((MultiInsertQuery<TSource>)query).Query.GetLinqToDBSource();

			return expressionQuery.GetSqlQueries(options)[0];
		}

		/// <summary>
		/// Convert Linq To DB query to SQL command with parameters.
		/// </summary>
		/// <remarks>Eager load queries currently return only SQL for main query.</remarks>
		public static QuerySql ToSqlQuery<TSource, TTarget>(this IMergeable<TSource, TTarget> query, SqlGenerationOptions? options = null)
		{
			var expressionQuery = (IExpressionQuery)((MergeQuery<TSource, TTarget>)query).Query.GetLinqToDBSource();

			return expressionQuery.GetSqlQueries(options)[0];
		}
		#endregion

		[Pure]
		public static TResult AggregateExecute<TSource, TResult>(this IQueryable<TSource> source, Expression<Func<IEnumerable<TSource>, TResult>> aggregate)
		{
			ArgumentNullException.ThrowIfNull(source);
			ArgumentNullException.ThrowIfNull(aggregate);

			var currentSource = source.ProcessIQueryable();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(AggregateExecute, source, aggregate), currentSource.Expression, aggregate);

			return currentSource.Provider.Execute<TResult>(expr);
		}

		[Pure]
		public static Task<TResult> AggregateExecuteAsync<TSource, TResult>(this IQueryable<TSource> source, Expression<Func<IEnumerable<TSource>, TResult>> aggregate, CancellationToken cancellationToken = default)
		{
			ArgumentNullException.ThrowIfNull(source);
			ArgumentNullException.ThrowIfNull(aggregate);

			var currentSource = source.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(AggregateExecute, source, aggregate), currentSource.Expression, aggregate);

			return currentSource.ExecuteAsync<TResult>(expr, cancellationToken);
		}
	}
}
