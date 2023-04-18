// Generated.
//
using System;
using System.Linq.Expressions;

namespace LinqToDB.DataProvider.ClickHouse
{
	public static partial class ClickHouseHints
	{
		[ExpressionMethod(nameof(JoinOuterHintImpl))]
		public static IClickHouseSpecificQueryable<TSource> JoinOuterHint<TSource>(this IClickHouseSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return query.JoinHint(Join.Outer);
		}

		static Expression<Func<IClickHouseSpecificQueryable<TSource>,IClickHouseSpecificQueryable<TSource>>> JoinOuterHintImpl<TSource>()
			where TSource : notnull
		{
			return query => query.JoinHint(Join.Outer);
		}

		[ExpressionMethod(nameof(JoinOuterTableHintImpl))]
		public static IClickHouseSpecificTable<TSource> JoinOuterHint<TSource>(this IClickHouseSpecificTable<TSource> table)
			where TSource : notnull
		{
			return table.JoinHint(Join.Outer);
		}

		static Expression<Func<IClickHouseSpecificTable<TSource>,IClickHouseSpecificTable<TSource>>> JoinOuterTableHintImpl<TSource>()
			where TSource : notnull
		{
			return table => table.JoinHint(Join.Outer);
		}

		[ExpressionMethod(nameof(JoinSemiHintImpl))]
		public static IClickHouseSpecificQueryable<TSource> JoinSemiHint<TSource>(this IClickHouseSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return query.JoinHint(Join.Semi);
		}

		static Expression<Func<IClickHouseSpecificQueryable<TSource>,IClickHouseSpecificQueryable<TSource>>> JoinSemiHintImpl<TSource>()
			where TSource : notnull
		{
			return query => query.JoinHint(Join.Semi);
		}

		[ExpressionMethod(nameof(JoinSemiTableHintImpl))]
		public static IClickHouseSpecificTable<TSource> JoinSemiHint<TSource>(this IClickHouseSpecificTable<TSource> table)
			where TSource : notnull
		{
			return table.JoinHint(Join.Semi);
		}

		static Expression<Func<IClickHouseSpecificTable<TSource>,IClickHouseSpecificTable<TSource>>> JoinSemiTableHintImpl<TSource>()
			where TSource : notnull
		{
			return table => table.JoinHint(Join.Semi);
		}

		[ExpressionMethod(nameof(JoinAntiHintImpl))]
		public static IClickHouseSpecificQueryable<TSource> JoinAntiHint<TSource>(this IClickHouseSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return query.JoinHint(Join.Anti);
		}

		static Expression<Func<IClickHouseSpecificQueryable<TSource>,IClickHouseSpecificQueryable<TSource>>> JoinAntiHintImpl<TSource>()
			where TSource : notnull
		{
			return query => query.JoinHint(Join.Anti);
		}

		[ExpressionMethod(nameof(JoinAntiTableHintImpl))]
		public static IClickHouseSpecificTable<TSource> JoinAntiHint<TSource>(this IClickHouseSpecificTable<TSource> table)
			where TSource : notnull
		{
			return table.JoinHint(Join.Anti);
		}

		static Expression<Func<IClickHouseSpecificTable<TSource>,IClickHouseSpecificTable<TSource>>> JoinAntiTableHintImpl<TSource>()
			where TSource : notnull
		{
			return table => table.JoinHint(Join.Anti);
		}

		[ExpressionMethod(nameof(JoinAnyHintImpl))]
		public static IClickHouseSpecificQueryable<TSource> JoinAnyHint<TSource>(this IClickHouseSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return query.JoinHint(Join.Any);
		}

		static Expression<Func<IClickHouseSpecificQueryable<TSource>,IClickHouseSpecificQueryable<TSource>>> JoinAnyHintImpl<TSource>()
			where TSource : notnull
		{
			return query => query.JoinHint(Join.Any);
		}

		[ExpressionMethod(nameof(JoinAnyTableHintImpl))]
		public static IClickHouseSpecificTable<TSource> JoinAnyHint<TSource>(this IClickHouseSpecificTable<TSource> table)
			where TSource : notnull
		{
			return table.JoinHint(Join.Any);
		}

		static Expression<Func<IClickHouseSpecificTable<TSource>,IClickHouseSpecificTable<TSource>>> JoinAnyTableHintImpl<TSource>()
			where TSource : notnull
		{
			return table => table.JoinHint(Join.Any);
		}

		[ExpressionMethod(nameof(JoinAsOfHintImpl))]
		public static IClickHouseSpecificQueryable<TSource> JoinAsOfHint<TSource>(this IClickHouseSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return query.JoinHint(Join.AsOf);
		}

		static Expression<Func<IClickHouseSpecificQueryable<TSource>,IClickHouseSpecificQueryable<TSource>>> JoinAsOfHintImpl<TSource>()
			where TSource : notnull
		{
			return query => query.JoinHint(Join.AsOf);
		}

		[ExpressionMethod(nameof(JoinAsOfTableHintImpl))]
		public static IClickHouseSpecificTable<TSource> JoinAsOfHint<TSource>(this IClickHouseSpecificTable<TSource> table)
			where TSource : notnull
		{
			return table.JoinHint(Join.AsOf);
		}

		static Expression<Func<IClickHouseSpecificTable<TSource>,IClickHouseSpecificTable<TSource>>> JoinAsOfTableHintImpl<TSource>()
			where TSource : notnull
		{
			return table => table.JoinHint(Join.AsOf);
		}

	}
}
