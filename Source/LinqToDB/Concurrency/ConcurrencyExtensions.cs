using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB.Extensions;
using LinqToDB.Internal.Common;
using LinqToDB.Internal.Extensions;
using LinqToDB.Internal.Linq;
using LinqToDB.Linq;
using LinqToDB.Mapping;
using LinqToDB.Reflection;

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
					Attr   = ed.MappingSchema.GetAttribute<OptimisticLockPropertyBaseAttribute>(objType, c.MemberInfo)
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

		private static IQueryable<T> MakeDeleteConcurrent<T>(IQueryable<T> source, IDataContext dc, T obj)
			where T : class
		{
			var objType = typeof(T);
			var ed      = dc.MappingSchema.GetEntityDescriptor(objType, dc.Options.ConnectionOptions.OnEntityDescriptorCreated);
			var query   = MakeConcurrentFilter(source, obj, objType, ed);

			return query;
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
			if (dc  == null) throw new ArgumentNullException(nameof(dc));
			if (obj == null) throw new ArgumentNullException(nameof(obj));

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
			if (dc  == null) throw new ArgumentNullException(nameof(dc));
			if (obj == null) throw new ArgumentNullException(nameof(obj));

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
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (obj    == null) throw new ArgumentNullException(nameof(obj));

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
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (obj    == null) throw new ArgumentNullException(nameof(obj));

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
			if (dc  == null) throw new ArgumentNullException(nameof(dc));

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
			if (dc  == null) throw new ArgumentNullException(nameof(dc));

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
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (obj    == null) throw new ArgumentNullException(nameof(obj));

			var dc      = Internals.GetDataContext(source) ?? throw new ArgumentException("Linq To DB query expected", nameof(source));
			var objType = typeof(T);
			var ed      = dc.MappingSchema.GetEntityDescriptor(objType, dc.Options.ConnectionOptions.OnEntityDescriptorCreated);

			return MakeConcurrentFilter(source, obj, objType, ed);
		}
	}
}
