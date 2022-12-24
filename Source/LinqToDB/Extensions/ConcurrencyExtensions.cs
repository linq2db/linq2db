using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using LinqToDB.Expressions;
using LinqToDB.Extensions;
using LinqToDB.Linq;
using LinqToDB.Mapping;
using LinqToDB.Reflection;

namespace LinqToDB
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
					cd.MemberAccessor.GetterExpression.GetBody(instance));

				predicate = predicate == null ? equality : Expression.AndAlso(predicate, equality);
			}

			if (predicate != null)
				query = (IQueryable<T>)methodInfo.Invoke(null, new object[] { query, Expression.Lambda(predicate, param) })!;

			return query;
		}

		private static IQueryable<T> FilterByPrimaryKey<T>(this IDataContext dc, T obj, EntityDescriptor ed)
			where T : class
		{
			var objType = typeof(T);
			var pks     = ed.Columns.Where(c => c.IsPrimaryKey).ToArray();

			if (pks.Length == 0)
				throw new LinqToDBException($"Entity of type {objType} does not have primary key defined.");

			return FilterByColumns(dc.GetTable<T>(), obj, pks);
		}

		private static IQueryable<T> MakeConcurrentFilter<T>(IDataContext dc, T obj, Type objType, EntityDescriptor ed)
			where T : class
		{
			var query   = FilterByPrimaryKey(dc, obj, ed);

			var concurrencyColumns = ed.Columns
				.Select(c => new
				{
					Column = c,
					Attr   = dc.MappingSchema.GetAttribute<ConcurrencyPropertyAttribute>(objType, c.MemberInfo)
				})
				.Where(_ => _.Attr != null)
				.Select(_ => _.Column)
				.ToArray();

			if (concurrencyColumns.Length > 0)
				query = FilterByColumns(query, obj, concurrencyColumns);

			return query;
		}

		private static IUpdatable<T> MakeUpdateConcurrent<T>(IDataContext dc, T obj)
			where T : class
		{
			var objType = typeof(T);
			var ed      = dc.MappingSchema.GetEntityDescriptor(objType);
			var query   = MakeConcurrentFilter(dc, obj, objType, ed);

			var updatable       = query.AsUpdatable();
			var columnsToUpdate = ed.Columns.Where(c => !c.IsPrimaryKey && !c.IsIdentity && !c.SkipOnUpdate && !c.ShouldSkip(obj, ed, SkipModification.Update));

			var param    = Expression.Parameter(objType, "u");
			var instance = Expression.Constant(obj);

			foreach (var cd in columnsToUpdate)
			{
				var updateMethod    = Methods.LinqToDB.Update.SetUpdatablePrev.MakeGenericMethod(objType, cd.MemberInfo.GetMemberType());
				var propExpression  = Expression.Lambda(Expression.MakeMemberAccess(param, cd.MemberInfo), param);

				var concurrencyAttribute = dc.MappingSchema.GetAttribute<ConcurrencyPropertyAttribute>(objType, cd.MemberInfo);

				LambdaExpression? valueExpression;
				if (concurrencyAttribute != null)
				{
					valueExpression = concurrencyAttribute?.GetNextValue(cd, param);

					if (valueExpression == null)
						continue;
				}
				else
					valueExpression = Expression.Lambda(cd.MemberAccessor.GetterExpression.GetBody(instance), param);

				updatable = (IUpdatable<T>)updateMethod.Invoke(null, new object[] { updatable, propExpression, valueExpression })!;
			}

			return updatable;
		}

		private static IQueryable<T> MakeDeleteConcurrent<T>(IDataContext dc, T obj)
			where T : class
		{
			var objType = typeof(T);
			var ed      = dc.MappingSchema.GetEntityDescriptor(objType);
			var query   = MakeConcurrentFilter(dc, obj, objType, ed);

			return query;
		}

		/// <summary>
		/// Performs record update using optimistic lock strategy.
		/// Entity should have column annotated with <see cref="ConcurrencyPropertyAttribute" />, otherwise regular update operation will be performed.
		/// </summary>
		/// <typeparam name="T">Entity type.</typeparam>
		/// <param name="dc">Database context.</param>
		/// <param name="obj">Entity instance to update.</param>
		/// <returns>Number of updated records.</returns>
		public static int UpdateConcurrent<T>(this IDataContext dc, T obj)
			where T : class
		{
			if (obj == null) throw new ArgumentNullException(nameof(obj));

			return MakeUpdateConcurrent(dc, obj).Update();
		}

		/// <summary>
		/// Performs record update using optimistic lock strategy asynchronously.
		/// Entity should have column annotated with <see cref="ConcurrencyPropertyAttribute" />, otherwise regular update operation will be performed.
		/// </summary>
		/// <typeparam name="T">Entity type.</typeparam>
		/// <param name="dc">Database context.</param>
		/// <param name="obj">Entity instance to update.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Number of updated records.</returns>
		public static Task<int> UpdateConcurrentAsync<T>(this IDataContext dc, T obj, CancellationToken cancellationToken = default)
			where T : class
		{
			if (obj == null) throw new ArgumentNullException(nameof(obj));

			return MakeUpdateConcurrent(dc, obj).UpdateAsync(cancellationToken);
		}

		/// <summary>
		/// Performs record delete using optimistic lock strategy.
		/// Entity should have column annotated with <see cref="ConcurrencyPropertyAttribute" />, otherwise regular delete operation will be performed.
		/// </summary>
		/// <typeparam name="T">Entity type.</typeparam>
		/// <param name="dc">Database context.</param>
		/// <param name="obj">Entity instance to delete.</param>
		/// <returns>Number of deleted records.</returns>
		public static int DeleteConcurrent<T>(this IDataContext dc, T obj)
			where T : class
		{
			if (obj == null) throw new ArgumentNullException(nameof(obj));

			return MakeDeleteConcurrent(dc, obj).Delete();
		}

		/// <summary>
		/// Performs record delete using optimistic lock strategy asynchronously.
		/// Entity should have column annotated with <see cref="ConcurrencyPropertyAttribute" />, otherwise regular delete operation will be performed.
		/// </summary>
		/// <typeparam name="T">Entity type.</typeparam>
		/// <param name="dc">Database context.</param>
		/// <param name="obj">Entity instance to delete.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Number of deleted records.</returns>
		public static Task<int> DeleteConcurrentAsync<T>(this IDataContext dc, T obj, CancellationToken cancellationToken = default)
			where T : class
		{
			if (obj == null) throw new ArgumentNullException(nameof(obj));

			return MakeDeleteConcurrent(dc, obj).DeleteAsync(cancellationToken);
		}
	}
}
