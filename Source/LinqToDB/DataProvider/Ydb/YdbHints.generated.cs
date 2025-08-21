#nullable enable
// Generated.
//
using System;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB.Mapping;

namespace LinqToDB.DataProvider.Ydb
{
	public static partial class YdbHints
	{
		// 1) IYdbSpecificQueryable<T>
		[ExpressionMethod(nameof(UniqueHintImpl))]
		public static IYdbSpecificQueryable<TSource> UniqueHint<TSource>(
			this IYdbSpecificQueryable<TSource> query,
			params string[]                     columns)
			where TSource : notnull
		{
			return QueryHint(query, Unique, columns);
		}
		static Expression<Func<IYdbSpecificQueryable<TSource>,string[],IYdbSpecificQueryable<TSource>>> UniqueHintImpl<TSource>()
			where TSource : notnull
		{
			return (query, columns) => QueryHint(query, Unique, columns);
		}

		[ExpressionMethod(nameof(UniqueHintQImpl))]
		public static IYdbSpecificQueryable<TSource> UniqueHint<TSource>(
			this IQueryable<TSource> query,
			params string[]          columns)
			where TSource : notnull
		{
			return QueryHint(query, Unique, columns);
		}
		static Expression<Func<IQueryable<TSource>,string[],IYdbSpecificQueryable<TSource>>> UniqueHintQImpl<TSource>()
			where TSource : notnull
		{
			return (query, columns) => QueryHint(query, Unique, columns);
		}

		// 1) IYdbSpecificQueryable<T>
		[ExpressionMethod(nameof(DistinctHintImpl))]
		public static IYdbSpecificQueryable<TSource> DistinctHint<TSource>(
			this IYdbSpecificQueryable<TSource> query,
			params string[]                     columns)
			where TSource : notnull
		{
			return QueryHint(query, Distinct, columns);
		}
		static Expression<Func<IYdbSpecificQueryable<TSource>,string[],IYdbSpecificQueryable<TSource>>> DistinctHintImpl<TSource>()
			where TSource : notnull
		{
			return (query, columns) => QueryHint(query, Distinct, columns);
		}

		[ExpressionMethod(nameof(DistinctHintQImpl))]
		public static IYdbSpecificQueryable<TSource> DistinctHint<TSource>(
			this IQueryable<TSource> query,
			params string[]          columns)
			where TSource : notnull
		{
			return QueryHint(query, Distinct, columns);
		}
		static Expression<Func<IQueryable<TSource>,string[],IYdbSpecificQueryable<TSource>>> DistinctHintQImpl<TSource>()
			where TSource : notnull
		{
			return (query, columns) => QueryHint(query, Distinct, columns);
		}

	}
}
