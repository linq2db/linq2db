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

			var concurrencyColumns = ed.Columns
				.Select(c => new
				{
					Column = c,
					Attr   = ed.MappingSchema.GetAttribute<OptimisticLockPropertyBaseAttribute>(objType, c.MemberInfo),
				})
				.Where(_ => _.Attr != null)
				.Select(_ => _.Column)
				.ToArray();

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
		/// <returns>Number of updated records.</returns>
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
		/// <returns>Number of updated records.</returns>
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
		/// <returns>Number of updated records.</returns>
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
		/// <returns>Number of updated records.</returns>
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
		/// <returns>Number of deleted records.</returns>
		public static int DeleteOptimistic<T>(this IDataContext dc, T obj)
			where T : class
		{
			ArgumentNullException.ThrowIfNull(dc);

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
		/// <returns>Number of deleted records.</returns>
		public static Task<int> DeleteOptimisticAsync<T>(this IDataContext dc, T obj, CancellationToken cancellationToken = default)
			where T : class
		{
			ArgumentNullException.ThrowIfNull(dc);

			return dc.GetTable<T>().WhereKeyOptimistic(obj).DeleteAsync(cancellationToken);
		}

		/// <summary>
		/// Performs record delete using optimistic lock strategy.
		/// Entity should have column annotated with <see cref="OptimisticLockPropertyBaseAttribute" />, otherwise regular delete operation will be performed.
		/// </summary>
		/// <typeparam name="T">Entity type.</typeparam>
		/// <param name="source">Table source with optional filtering applied.</param>
		/// <param name="obj">Entity instance to delete.</param>
		/// <returns>Number of deleted records.</returns>
		public static int DeleteOptimistic<T>(this IQueryable<T> source, T obj)
			where T : class
		{
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
		/// <returns>Number of deleted records.</returns>
		public static Task<int> DeleteOptimisticAsync<T>(this IQueryable<T> source, T obj, CancellationToken cancellationToken = default)
			where T : class
		{
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

		private static void CopyColumns<T>(ColumnDescriptor[] columns, T from, T to)
			where T : class
		{
			foreach (var cd in columns)
				cd.MemberAccessor.SetValue(to, cd.MemberAccessor.GetValue(from));
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
			var objType   = typeof(T);
			var ed        = dc.MappingSchema.GetEntityDescriptor(objType, dc.Options.ConnectionOptions.OnEntityDescriptorCreated);
			var updatable = MakeUpdateOptimistic(source, dc, obj);

			if (dc.SqlProviderFlags.IsUpdateOutputSupported)
			{
				// project only the inserted (post-update) row so the OLD pseudo-table isn't referenced —
				// SQLite / PostgreSQL < 18 / Firebird < 5 expose new values only
				var inserted = updatable.UpdateWithOutput((deleted, ins) => ins).ToList();

				if (inserted.Count > 0)
					CopyColumns(GetOptimisticLockColumns(ed, objType), inserted[0], obj);

				return inserted.Count;
			}

			var count = updatable.Update();

			if (count > 0)
			{
				var lockColumns = GetOptimisticLockColumns(ed, objType);

				if (lockColumns.Length > 0)
				{
					var fresh = FilterByPrimaryKey(dc.GetTable<T>(), obj, ed).FirstOrDefault();

					if (fresh != null)
						CopyColumns(lockColumns, fresh, obj);
				}
			}

			return count;
		}

		private static async Task<int> UpdateOptimisticWithRefreshCoreAsync<T>(IQueryable<T> source, IDataContext dc, T obj, CancellationToken cancellationToken)
			where T : class
		{
			var objType   = typeof(T);
			var ed        = dc.MappingSchema.GetEntityDescriptor(objType, dc.Options.ConnectionOptions.OnEntityDescriptorCreated);
			var updatable = MakeUpdateOptimistic(source, dc, obj);

			if (dc.SqlProviderFlags.IsUpdateOutputSupported)
			{
				// project only the inserted (post-update) row so the OLD pseudo-table isn't referenced —
				// SQLite / PostgreSQL < 18 / Firebird < 5 expose new values only
				var inserted = await ToListAsync(updatable.UpdateWithOutputAsync((deleted, ins) => ins), cancellationToken).ConfigureAwait(false);

				if (inserted.Count > 0)
					CopyColumns(GetOptimisticLockColumns(ed, objType), inserted[0], obj);

				return inserted.Count;
			}

			var count = await updatable.UpdateAsync(cancellationToken).ConfigureAwait(false);

			if (count > 0)
			{
				var lockColumns = GetOptimisticLockColumns(ed, objType);

				if (lockColumns.Length > 0)
				{
					var fresh = await FilterByPrimaryKey(dc.GetTable<T>(), obj, ed).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

					if (fresh != null)
						CopyColumns(lockColumns, fresh, obj);
				}
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
		/// <returns>Number of updated records. <c>0</c> indicates a concurrency failure.</returns>
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
		/// <returns>Number of updated records. <c>0</c> indicates a concurrency failure.</returns>
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
		/// <returns>Number of updated records. <c>0</c> indicates a concurrency failure.</returns>
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
		/// <returns>Number of updated records. <c>0</c> indicates a concurrency failure.</returns>
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
