using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB.Async;
using LinqToDB.Internal.Common;
using LinqToDB.Internal.Extensions;
using LinqToDB.Internal.Linq;
using LinqToDB.Internal.Reflection;
using LinqToDB.Linq;
using LinqToDB.Mapping;

namespace LinqToDB.Concurrency
{
	public static class ConcurrencyExtensions
	{
		private static IQueryable<T> FilterByColumns<T>(IQueryable<T> query, T obj, ColumnDescriptor[] columns)
			where T : class
		{
			var objType           = typeof(T);
			var methodInfo        = Methods.Queryable.Where.MakeGenericMethod(objType);
			var param             = Expression.Parameter(typeof(T), "obj");
			var instance          = Expression.Constant(obj);
			Expression? predicate = null;

			foreach (var cd in columns)
			{
				var equality = Expression.Equal(
					Expression.MakeMemberAccess(param, cd.MemberInfo),
					cd.MemberAccessor.GetGetterExpression(instance));

				predicate = predicate == null ? equality : Expression.AndAlso(predicate, equality);
			}

			if (predicate != null)
				query = methodInfo.InvokeExt<IQueryable<T>>(null, new object[] { query, Expression.Lambda(predicate, param) });

			return query;
		}

		private static IQueryable<T> FilterByPrimaryKey<T>(this IQueryable<T> source, T obj, EntityDescriptor ed)
			where T : class
		{
			var objType = typeof(T);
			var pks     = ed.Columns.Where(c => c.IsPrimaryKey).ToArray();

			if (pks.Length == 0)
				throw new LinqToDBException($"Entity of type {objType} does not have primary key defined.");

			return FilterByColumns(source, obj, pks);
		}

		private static IQueryable<T> MakeConcurrentFilter<T>(IQueryable<T> source, T obj, Type objType, EntityDescriptor ed)
			where T : class
		{
			var query = FilterByPrimaryKey(source, obj, ed);

			var concurrencyColumns = GetOptimisticLockColumns(ed, objType);

			if (concurrencyColumns.Length > 0)
				query = FilterByColumns(query, obj, concurrencyColumns);

			return query;
		}

		private static IUpdatable<T> MakeUpdateOptimistic<T>(IQueryable<T> query, IDataContext dc, T obj)
			where T : class
		{
			var objType = typeof(T);
			var ed      = dc.MappingSchema.GetEntityDescriptor(objType, dc.Options.ConnectionOptions.OnEntityDescriptorCreated);
			    query   = MakeConcurrentFilter(query, obj, objType, ed);

			var updatable       = query.AsUpdatable();
			var columnsToUpdate = ed.Columns.Where(c => !c.IsPrimaryKey && !c.IsIdentity && !c.SkipOnUpdate && !c.ShouldSkip(obj, ed, SkipModification.Update));

			var param    = Expression.Parameter(objType, "u");
			var instance = Expression.Constant(obj);

			foreach (var cd in columnsToUpdate)
			{
				var updateMethod    = Methods.LinqToDB.Update.SetUpdatablePrev.MakeGenericMethod(objType, cd.MemberInfo.GetMemberType());
				var propExpression  = Expression.Lambda(Expression.MakeMemberAccess(param, cd.MemberInfo), param);

				var concurrencyAttribute = ed.MappingSchema.GetAttribute<OptimisticLockPropertyBaseAttribute>(objType, cd.MemberInfo);

				LambdaExpression? valueExpression;
				if (concurrencyAttribute != null)
				{
					valueExpression = concurrencyAttribute.GetNextValue(cd, param);

					if (valueExpression == null)
						continue;
				}
				else
					valueExpression = Expression.Lambda(cd.MemberAccessor.GetGetterExpression(instance), param);

				updatable = updateMethod.InvokeExt<IUpdatable<T>>(null, new object[] { updatable, propExpression, valueExpression });
			}

			return updatable;
		}

		/// <summary>
		/// Performs record update using optimistic lock strategy.
		/// Entity should have column annotated with <see cref="OptimisticLockPropertyBaseAttribute" />, otherwise regular update operation will be performed.
		/// </summary>
		/// <typeparam name="T">Entity type.</typeparam>
		/// <param name="dc">Database context.</param>
		/// <param name="obj">Entity instance to update.</param>
		/// <returns>
		/// Number of updated records. On providers that do not report affected rows (e.g. ClickHouse) the count is
		/// always <c>0</c>, so it cannot be used to detect an optimistic-concurrency failure on those providers.
		/// </returns>
		public static int UpdateOptimistic<T>(this IDataContext dc, T obj)
			where T : class
		{
			ArgumentNullException.ThrowIfNull(dc);
			ArgumentNullException.ThrowIfNull(obj);

			return MakeUpdateOptimistic(dc.GetTable<T>(), dc, obj).Update();
		}

		/// <summary>
		/// Performs record update using optimistic lock strategy asynchronously.
		/// Entity should have column annotated with <see cref="OptimisticLockPropertyBaseAttribute" />, otherwise regular update operation will be performed.
		/// </summary>
		/// <typeparam name="T">Entity type.</typeparam>
		/// <param name="dc">Database context.</param>
		/// <param name="obj">Entity instance to update.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>
		/// Number of updated records. On providers that do not report affected rows (e.g. ClickHouse) the count is
		/// always <c>0</c>, so it cannot be used to detect an optimistic-concurrency failure on those providers.
		/// </returns>
		public static Task<int> UpdateOptimisticAsync<T>(this IDataContext dc, T obj, CancellationToken cancellationToken = default)
			where T : class
		{
			ArgumentNullException.ThrowIfNull(dc);
			ArgumentNullException.ThrowIfNull(obj);

			return MakeUpdateOptimistic(dc.GetTable<T>(), dc, obj).UpdateAsync(cancellationToken);
		}

		/// <summary>
		/// Performs record update using optimistic lock strategy.
		/// Entity should have column annotated with <see cref="OptimisticLockPropertyBaseAttribute" />, otherwise regular update operation will be performed.
		/// </summary>
		/// <typeparam name="T">Entity type.</typeparam>
		/// <param name="source">Table source with optional filtering applied.</param>
		/// <param name="obj">Entity instance to update.</param>
		/// <returns>
		/// Number of updated records. On providers that do not report affected rows (e.g. ClickHouse) the count is
		/// always <c>0</c>, so it cannot be used to detect an optimistic-concurrency failure on those providers.
		/// </returns>
		public static int UpdateOptimistic<T>(this IQueryable<T> source, T obj)
			where T : class
		{
			ArgumentNullException.ThrowIfNull(source);
			ArgumentNullException.ThrowIfNull(obj);

			var dc = Internals.GetDataContext(source) ?? throw new ArgumentException("Linq To DB query expected", nameof(source));

			return MakeUpdateOptimistic(source, dc, obj).Update();
		}

		/// <summary>
		/// Performs record update using optimistic lock strategy asynchronously.
		/// Entity should have column annotated with <see cref="OptimisticLockPropertyBaseAttribute" />, otherwise regular update operation will be performed.
		/// </summary>
		/// <typeparam name="T">Entity type.</typeparam>
		/// <param name="source">Table source with optional filtering applied.</param>
		/// <param name="obj">Entity instance to update.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>
		/// Number of updated records. On providers that do not report affected rows (e.g. ClickHouse) the count is
		/// always <c>0</c>, so it cannot be used to detect an optimistic-concurrency failure on those providers.
		/// </returns>
		public static Task<int> UpdateOptimisticAsync<T>(this IQueryable<T> source, T obj, CancellationToken cancellationToken = default)
			where T : class
		{
			ArgumentNullException.ThrowIfNull(source);
			ArgumentNullException.ThrowIfNull(obj);

			var dc = Internals.GetDataContext(source) ?? throw new ArgumentException("Linq To DB query expected", nameof(source));

			return MakeUpdateOptimistic(source, dc, obj).UpdateAsync(cancellationToken);
		}

		/// <summary>
		/// Performs record delete using optimistic lock strategy.
		/// Entity should have column annotated with <see cref="OptimisticLockPropertyBaseAttribute" />, otherwise regular delete operation will be performed.
		/// </summary>
		/// <typeparam name="T">Entity type.</typeparam>
		/// <param name="dc">Database context.</param>
		/// <param name="obj">Entity instance to delete.</param>
		/// <returns>
		/// Number of deleted records. On providers that do not report affected rows (e.g. ClickHouse) the count is
		/// always <c>0</c>, so it cannot be used to detect an optimistic-concurrency failure on those providers.
		/// </returns>
		public static int DeleteOptimistic<T>(this IDataContext dc, T obj)
			where T : class
		{
			ArgumentNullException.ThrowIfNull(dc);
			ArgumentNullException.ThrowIfNull(obj);

			return dc.GetTable<T>().WhereKeyOptimistic(obj).Delete();
		}

		/// <summary>
		/// Performs record delete using optimistic lock strategy asynchronously.
		/// Entity should have column annotated with <see cref="OptimisticLockPropertyBaseAttribute" />, otherwise regular delete operation will be performed.
		/// </summary>
		/// <typeparam name="T">Entity type.</typeparam>
		/// <param name="dc">Database context.</param>
		/// <param name="obj">Entity instance to delete.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>
		/// Number of deleted records. On providers that do not report affected rows (e.g. ClickHouse) the count is
		/// always <c>0</c>, so it cannot be used to detect an optimistic-concurrency failure on those providers.
		/// </returns>
		public static Task<int> DeleteOptimisticAsync<T>(this IDataContext dc, T obj, CancellationToken cancellationToken = default)
			where T : class
		{
			ArgumentNullException.ThrowIfNull(dc);
			ArgumentNullException.ThrowIfNull(obj);

			return dc.GetTable<T>().WhereKeyOptimistic(obj).DeleteAsync(cancellationToken);
		}

		/// <summary>
		/// Performs record delete using optimistic lock strategy.
		/// Entity should have column annotated with <see cref="OptimisticLockPropertyBaseAttribute" />, otherwise regular delete operation will be performed.
		/// </summary>
		/// <typeparam name="T">Entity type.</typeparam>
		/// <param name="source">Table source with optional filtering applied.</param>
		/// <param name="obj">Entity instance to delete.</param>
		/// <returns>
		/// Number of deleted records. On providers that do not report affected rows (e.g. ClickHouse) the count is
		/// always <c>0</c>, so it cannot be used to detect an optimistic-concurrency failure on those providers.
		/// </returns>
		public static int DeleteOptimistic<T>(this IQueryable<T> source, T obj)
			where T : class
		{
			ArgumentNullException.ThrowIfNull(source);
			ArgumentNullException.ThrowIfNull(obj);

			return source.WhereKeyOptimistic(obj).Delete();
		}

		/// <summary>
		/// Performs record delete using optimistic lock strategy asynchronously.
		/// Entity should have column annotated with <see cref="OptimisticLockPropertyBaseAttribute" />, otherwise regular delete operation will be performed.
		/// </summary>
		/// <typeparam name="T">Entity type.</typeparam>
		/// <param name="source">Table source with optional filtering applied.</param>
		/// <param name="obj">Entity instance to delete.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>
		/// Number of deleted records. On providers that do not report affected rows (e.g. ClickHouse) the count is
		/// always <c>0</c>, so it cannot be used to detect an optimistic-concurrency failure on those providers.
		/// </returns>
		public static Task<int> DeleteOptimisticAsync<T>(this IQueryable<T> source, T obj, CancellationToken cancellationToken = default)
			where T : class
		{
			ArgumentNullException.ThrowIfNull(source);
			ArgumentNullException.ThrowIfNull(obj);

			return source.WhereKeyOptimistic(obj).DeleteAsync(cancellationToken);
		}

		/// <summary>
		/// Applies primary key and optimistic lock filters to query for specific record.
		/// Entity should have column annotated with <see cref="OptimisticLockPropertyBaseAttribute" />, otherwise only primary key filter will be applied to query.
		/// </summary>
		/// <typeparam name="T">Entity type.</typeparam>
		/// <param name="source">Entity query.</param>
		/// <param name="obj">Entity instance to take current lock field value from.</param>
		/// <returns>Query with filter over lock field.</returns>
		public static IQueryable<T> WhereKeyOptimistic<T>(this IQueryable<T> source, T obj)
			where T : class
		{
			ArgumentNullException.ThrowIfNull(source);
			ArgumentNullException.ThrowIfNull(obj);

			var dc      = Internals.GetDataContext(source) ?? throw new ArgumentException("Linq To DB query expected", nameof(source));
			var objType = typeof(T);
			var ed      = dc.MappingSchema.GetEntityDescriptor(objType, dc.Options.ConnectionOptions.OnEntityDescriptorCreated);

			return MakeConcurrentFilter(source, obj, objType, ed);
		}

		#region OUTPUT / RETURNING overloads

		private static ColumnDescriptor[] GetOptimisticLockColumns(EntityDescriptor ed, Type objType)
		{
			return ed.Columns
				.Where(c => ed.MappingSchema.GetAttribute<OptimisticLockPropertyBaseAttribute>(objType, c.MemberInfo) != null)
				.ToArray();
		}

		private const string UpdateWithRefreshNotSupportedMessage =
			"UpdateOptimisticWithRefresh requires the provider to support UPDATE OUTPUT / RETURNING " +
			"(SqlProviderFlags.IsUpdateOutputSupported) or a reliable affected-rows count " +
			"(SqlProviderFlags.IsAffectedRowsCountSupported); the current provider supports neither, so the " +
			"optimistic-concurrency result cannot be guaranteed.";

		private static void CopyColumns<T>(ColumnDescriptor[] columns, T from, T to)
			where T : class
		{
			foreach (var cd in columns)
				cd.MemberAccessor.SetValue(to, cd.MemberAccessor.GetValue(from));
		}

		// new T { lockCol = <source>.lockCol, ... } — only the optimistic-lock column(s), nothing else
		private static MemberInitExpression InitLockColumns(Type objType, ColumnDescriptor[] lockColumns, Expression source)
		{
			var bindings = new MemberBinding[lockColumns.Length];

			for (var i = 0; i < lockColumns.Length; i++)
				bindings[i] = Expression.Bind(lockColumns[i].MemberInfo, Expression.MakeMemberAccess(source, lockColumns[i].MemberInfo));

			return Expression.MemberInit(Expression.New(objType), bindings);
		}

		// (deleted, inserted) => new T { lockCol = inserted.lockCol, ... } — OUTPUT projection of new lock value(s)
		private static Expression<Func<T, T, T>> LockColumnsOutput<T>(Type objType, ColumnDescriptor[] lockColumns)
		{
			var deleted  = Expression.Parameter(objType, "deleted");
			var inserted = Expression.Parameter(objType, "inserted");

			return Expression.Lambda<Func<T, T, T>>(InitLockColumns(objType, lockColumns, inserted), deleted, inserted);
		}

		// x => new T { lockCol = x.lockCol, ... } — SELECT projection of the lock value(s) for the fallback read-back
		private static Expression<Func<T, T>> LockColumnsSelector<T>(Type objType, ColumnDescriptor[] lockColumns)
		{
			var x = Expression.Parameter(objType, "x");

			return Expression.Lambda<Func<T, T>>(InitLockColumns(objType, lockColumns, x), x);
		}

		private static async Task<List<TItem>> ToListAsync<TItem>(IAsyncEnumerable<TItem> source, CancellationToken cancellationToken)
		{
			var list = new List<TItem>();

			await foreach (var item in source.WithCancellation(cancellationToken).ConfigureAwait(false))
				list.Add(item);

			return list;
		}

		private static int UpdateOptimisticWithRefreshCore<T>(IQueryable<T> source, IDataContext dc, T obj)
			where T : class
		{
			var objType     = typeof(T);
			var ed          = dc.MappingSchema.GetEntityDescriptor(objType, dc.Options.ConnectionOptions.OnEntityDescriptorCreated);
			var updatable   = MakeUpdateOptimistic(source, dc, obj);
			var lockColumns = GetOptimisticLockColumns(ed, objType);

			// no optimistic-lock column -> nothing to refresh, behave like a plain optimistic update
			if (lockColumns.Length == 0)
				return updatable.Update();

			if (dc.SqlProviderFlags.IsUpdateOutputSupported)
			{
				// output only the regenerated lock column(s); referencing the inserted (new) row keeps it valid on
				// new-values-only providers (SQLite / PostgreSQL < 18 / Firebird < 5)
				var refreshed = updatable.UpdateWithOutput(LockColumnsOutput<T>(objType, lockColumns)).ToList();

				if (refreshed.Count > 0)
					CopyColumns(lockColumns, refreshed[0], obj);

				return refreshed.Count;
			}

			// No single-statement OUTPUT / RETURNING and no reliable affected-row count (e.g. ClickHouse) -> the
			// optimistic-concurrency result cannot be reported, so the operation is unsupported for this provider.
			if (!dc.SqlProviderFlags.IsAffectedRowsCountSupported)
				throw new LinqToDBException(UpdateWithRefreshNotSupportedMessage);

			var count = updatable.Update();

			// reliable affected-row count: 0 is a genuine concurrency failure -> leave the entity untouched
			if (count > 0)
			{
				var fresh = FilterByPrimaryKey(dc.GetTable<T>(), obj, ed).Select(LockColumnsSelector<T>(objType, lockColumns)).FirstOrDefault();

				if (fresh != null)
					CopyColumns(lockColumns, fresh, obj);
			}

			return count;
		}

		private static async Task<int> UpdateOptimisticWithRefreshCoreAsync<T>(IQueryable<T> source, IDataContext dc, T obj, CancellationToken cancellationToken)
			where T : class
		{
			var objType     = typeof(T);
			var ed          = dc.MappingSchema.GetEntityDescriptor(objType, dc.Options.ConnectionOptions.OnEntityDescriptorCreated);
			var updatable   = MakeUpdateOptimistic(source, dc, obj);
			var lockColumns = GetOptimisticLockColumns(ed, objType);

			// no optimistic-lock column -> nothing to refresh, behave like a plain optimistic update
			if (lockColumns.Length == 0)
				return await updatable.UpdateAsync(cancellationToken).ConfigureAwait(false);

			if (dc.SqlProviderFlags.IsUpdateOutputSupported)
			{
				// output only the regenerated lock column(s); referencing the inserted (new) row keeps it valid on
				// new-values-only providers (SQLite / PostgreSQL < 18 / Firebird < 5)
				var refreshed = await ToListAsync(updatable.UpdateWithOutputAsync(LockColumnsOutput<T>(objType, lockColumns)), cancellationToken).ConfigureAwait(false);

				if (refreshed.Count > 0)
					CopyColumns(lockColumns, refreshed[0], obj);

				return refreshed.Count;
			}

			// No single-statement OUTPUT / RETURNING and no reliable affected-row count (e.g. ClickHouse) -> the
			// optimistic-concurrency result cannot be reported, so the operation is unsupported for this provider.
			if (!dc.SqlProviderFlags.IsAffectedRowsCountSupported)
				throw new LinqToDBException(UpdateWithRefreshNotSupportedMessage);

			var count = await updatable.UpdateAsync(cancellationToken).ConfigureAwait(false);

			// reliable affected-row count: 0 is a genuine concurrency failure -> leave the entity untouched
			if (count > 0)
			{
				var fresh = await FilterByPrimaryKey(dc.GetTable<T>(), obj, ed).Select(LockColumnsSelector<T>(objType, lockColumns)).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

				if (fresh != null)
					CopyColumns(lockColumns, fresh, obj);
			}

			return count;
		}

		/// <summary>
		/// Performs record update using optimistic lock strategy and refreshes the optimistic-lock column(s) on
		/// <paramref name="obj"/> with the regenerated value(s) read back from the same statement (via OUTPUT / RETURNING).
		/// On providers without OUTPUT / RETURNING support the value is read back with a follow-up <c>SELECT</c> instead.
		/// </summary>
		/// <typeparam name="T">Entity type.</typeparam>
		/// <param name="dc">Database context.</param>
		/// <param name="obj">Entity instance to update. Receives the regenerated optimistic-lock value(s) on success.</param>
		/// <returns>
		/// Number of updated records. <c>0</c> indicates an optimistic-concurrency failure; the entity is left untouched.
		/// </returns>
		/// <exception cref="LinqToDBException">
		/// Thrown when the provider supports neither single-statement UPDATE OUTPUT / RETURNING nor a reliable
		/// affected-rows count (e.g. ClickHouse), so the optimistic-concurrency result cannot be guaranteed.
		/// </exception>
		public static int UpdateOptimisticWithRefresh<T>(this IDataContext dc, T obj)
			where T : class
		{
			ArgumentNullException.ThrowIfNull(dc);
			ArgumentNullException.ThrowIfNull(obj);

			return UpdateOptimisticWithRefreshCore(dc.GetTable<T>(), dc, obj);
		}

		/// <summary>
		/// Performs record update using optimistic lock strategy asynchronously and refreshes the optimistic-lock column(s)
		/// on <paramref name="obj"/> with the regenerated value(s) read back from the same statement (via OUTPUT / RETURNING).
		/// On providers without OUTPUT / RETURNING support the value is read back with a follow-up <c>SELECT</c> instead.
		/// </summary>
		/// <typeparam name="T">Entity type.</typeparam>
		/// <param name="dc">Database context.</param>
		/// <param name="obj">Entity instance to update. Receives the regenerated optimistic-lock value(s) on success.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>
		/// Number of updated records. <c>0</c> indicates an optimistic-concurrency failure; the entity is left untouched.
		/// </returns>
		/// <exception cref="LinqToDBException">
		/// Thrown when the provider supports neither single-statement UPDATE OUTPUT / RETURNING nor a reliable
		/// affected-rows count (e.g. ClickHouse), so the optimistic-concurrency result cannot be guaranteed.
		/// </exception>
		public static Task<int> UpdateOptimisticWithRefreshAsync<T>(this IDataContext dc, T obj, CancellationToken cancellationToken = default)
			where T : class
		{
			ArgumentNullException.ThrowIfNull(dc);
			ArgumentNullException.ThrowIfNull(obj);

			return UpdateOptimisticWithRefreshCoreAsync(dc.GetTable<T>(), dc, obj, cancellationToken);
		}

		/// <summary>
		/// Performs record update using optimistic lock strategy and refreshes the optimistic-lock column(s) on
		/// <paramref name="obj"/> with the regenerated value(s) read back from the same statement (via OUTPUT / RETURNING).
		/// On providers without OUTPUT / RETURNING support the value is read back with a follow-up <c>SELECT</c> instead.
		/// </summary>
		/// <typeparam name="T">Entity type.</typeparam>
		/// <param name="source">Table source with optional filtering applied.</param>
		/// <param name="obj">Entity instance to update. Receives the regenerated optimistic-lock value(s) on success.</param>
		/// <returns>
		/// Number of updated records. <c>0</c> indicates an optimistic-concurrency failure; the entity is left untouched.
		/// </returns>
		/// <exception cref="LinqToDBException">
		/// Thrown when the provider supports neither single-statement UPDATE OUTPUT / RETURNING nor a reliable
		/// affected-rows count (e.g. ClickHouse), so the optimistic-concurrency result cannot be guaranteed.
		/// </exception>
		public static int UpdateOptimisticWithRefresh<T>(this IQueryable<T> source, T obj)
			where T : class
		{
			ArgumentNullException.ThrowIfNull(source);
			ArgumentNullException.ThrowIfNull(obj);

			var dc = Internals.GetDataContext(source) ?? throw new ArgumentException("Linq To DB query expected", nameof(source));

			return UpdateOptimisticWithRefreshCore(source, dc, obj);
		}

		/// <summary>
		/// Performs record update using optimistic lock strategy asynchronously and refreshes the optimistic-lock column(s)
		/// on <paramref name="obj"/> with the regenerated value(s) read back from the same statement (via OUTPUT / RETURNING).
		/// On providers without OUTPUT / RETURNING support the value is read back with a follow-up <c>SELECT</c> instead.
		/// </summary>
		/// <typeparam name="T">Entity type.</typeparam>
		/// <param name="source">Table source with optional filtering applied.</param>
		/// <param name="obj">Entity instance to update. Receives the regenerated optimistic-lock value(s) on success.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>
		/// Number of updated records. <c>0</c> indicates an optimistic-concurrency failure; the entity is left untouched.
		/// </returns>
		/// <exception cref="LinqToDBException">
		/// Thrown when the provider supports neither single-statement UPDATE OUTPUT / RETURNING nor a reliable
		/// affected-rows count (e.g. ClickHouse), so the optimistic-concurrency result cannot be guaranteed.
		/// </exception>
		public static Task<int> UpdateOptimisticWithRefreshAsync<T>(this IQueryable<T> source, T obj, CancellationToken cancellationToken = default)
			where T : class
		{
			ArgumentNullException.ThrowIfNull(source);
			ArgumentNullException.ThrowIfNull(obj);

			var dc = Internals.GetDataContext(source) ?? throw new ArgumentException("Linq To DB query expected", nameof(source));

			return UpdateOptimisticWithRefreshCoreAsync(source, dc, obj, cancellationToken);
		}

		#endregion
	}
}
