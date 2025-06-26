#nullable enable
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

		[ExpressionMethod(nameof(JoinGlobalHintImpl))]
		public static IClickHouseSpecificQueryable<TSource> JoinGlobalHint<TSource>(this IClickHouseSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return query.JoinHint(Join.Global);
		}

		static Expression<Func<IClickHouseSpecificQueryable<TSource>,IClickHouseSpecificQueryable<TSource>>> JoinGlobalHintImpl<TSource>()
			where TSource : notnull
		{
			return query => query.JoinHint(Join.Global);
		}

		[ExpressionMethod(nameof(JoinGlobalTableHintImpl))]
		public static IClickHouseSpecificTable<TSource> JoinGlobalHint<TSource>(this IClickHouseSpecificTable<TSource> table)
			where TSource : notnull
		{
			return table.JoinHint(Join.Global);
		}

		static Expression<Func<IClickHouseSpecificTable<TSource>,IClickHouseSpecificTable<TSource>>> JoinGlobalTableHintImpl<TSource>()
			where TSource : notnull
		{
			return table => table.JoinHint(Join.Global);
		}

		[ExpressionMethod(nameof(JoinGlobalOuterHintImpl))]
		public static IClickHouseSpecificQueryable<TSource> JoinGlobalOuterHint<TSource>(this IClickHouseSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return query.JoinHint(Join.GlobalOuter);
		}

		static Expression<Func<IClickHouseSpecificQueryable<TSource>,IClickHouseSpecificQueryable<TSource>>> JoinGlobalOuterHintImpl<TSource>()
			where TSource : notnull
		{
			return query => query.JoinHint(Join.GlobalOuter);
		}

		[ExpressionMethod(nameof(JoinGlobalOuterTableHintImpl))]
		public static IClickHouseSpecificTable<TSource> JoinGlobalOuterHint<TSource>(this IClickHouseSpecificTable<TSource> table)
			where TSource : notnull
		{
			return table.JoinHint(Join.GlobalOuter);
		}

		static Expression<Func<IClickHouseSpecificTable<TSource>,IClickHouseSpecificTable<TSource>>> JoinGlobalOuterTableHintImpl<TSource>()
			where TSource : notnull
		{
			return table => table.JoinHint(Join.GlobalOuter);
		}

		[ExpressionMethod(nameof(JoinGlobalSemiHintImpl))]
		public static IClickHouseSpecificQueryable<TSource> JoinGlobalSemiHint<TSource>(this IClickHouseSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return query.JoinHint(Join.GlobalSemi);
		}

		static Expression<Func<IClickHouseSpecificQueryable<TSource>,IClickHouseSpecificQueryable<TSource>>> JoinGlobalSemiHintImpl<TSource>()
			where TSource : notnull
		{
			return query => query.JoinHint(Join.GlobalSemi);
		}

		[ExpressionMethod(nameof(JoinGlobalSemiTableHintImpl))]
		public static IClickHouseSpecificTable<TSource> JoinGlobalSemiHint<TSource>(this IClickHouseSpecificTable<TSource> table)
			where TSource : notnull
		{
			return table.JoinHint(Join.GlobalSemi);
		}

		static Expression<Func<IClickHouseSpecificTable<TSource>,IClickHouseSpecificTable<TSource>>> JoinGlobalSemiTableHintImpl<TSource>()
			where TSource : notnull
		{
			return table => table.JoinHint(Join.GlobalSemi);
		}

		[ExpressionMethod(nameof(JoinGlobalAntiHintImpl))]
		public static IClickHouseSpecificQueryable<TSource> JoinGlobalAntiHint<TSource>(this IClickHouseSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return query.JoinHint(Join.GlobalAnti);
		}

		static Expression<Func<IClickHouseSpecificQueryable<TSource>,IClickHouseSpecificQueryable<TSource>>> JoinGlobalAntiHintImpl<TSource>()
			where TSource : notnull
		{
			return query => query.JoinHint(Join.GlobalAnti);
		}

		[ExpressionMethod(nameof(JoinGlobalAntiTableHintImpl))]
		public static IClickHouseSpecificTable<TSource> JoinGlobalAntiHint<TSource>(this IClickHouseSpecificTable<TSource> table)
			where TSource : notnull
		{
			return table.JoinHint(Join.GlobalAnti);
		}

		static Expression<Func<IClickHouseSpecificTable<TSource>,IClickHouseSpecificTable<TSource>>> JoinGlobalAntiTableHintImpl<TSource>()
			where TSource : notnull
		{
			return table => table.JoinHint(Join.GlobalAnti);
		}

		[ExpressionMethod(nameof(JoinGlobalAnyHintImpl))]
		public static IClickHouseSpecificQueryable<TSource> JoinGlobalAnyHint<TSource>(this IClickHouseSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return query.JoinHint(Join.GlobalAny);
		}

		static Expression<Func<IClickHouseSpecificQueryable<TSource>,IClickHouseSpecificQueryable<TSource>>> JoinGlobalAnyHintImpl<TSource>()
			where TSource : notnull
		{
			return query => query.JoinHint(Join.GlobalAny);
		}

		[ExpressionMethod(nameof(JoinGlobalAnyTableHintImpl))]
		public static IClickHouseSpecificTable<TSource> JoinGlobalAnyHint<TSource>(this IClickHouseSpecificTable<TSource> table)
			where TSource : notnull
		{
			return table.JoinHint(Join.GlobalAny);
		}

		static Expression<Func<IClickHouseSpecificTable<TSource>,IClickHouseSpecificTable<TSource>>> JoinGlobalAnyTableHintImpl<TSource>()
			where TSource : notnull
		{
			return table => table.JoinHint(Join.GlobalAny);
		}

		[ExpressionMethod(nameof(JoinGlobalAsOfHintImpl))]
		public static IClickHouseSpecificQueryable<TSource> JoinGlobalAsOfHint<TSource>(this IClickHouseSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return query.JoinHint(Join.GlobalAsOf);
		}

		static Expression<Func<IClickHouseSpecificQueryable<TSource>,IClickHouseSpecificQueryable<TSource>>> JoinGlobalAsOfHintImpl<TSource>()
			where TSource : notnull
		{
			return query => query.JoinHint(Join.GlobalAsOf);
		}

		[ExpressionMethod(nameof(JoinGlobalAsOfTableHintImpl))]
		public static IClickHouseSpecificTable<TSource> JoinGlobalAsOfHint<TSource>(this IClickHouseSpecificTable<TSource> table)
			where TSource : notnull
		{
			return table.JoinHint(Join.GlobalAsOf);
		}

		static Expression<Func<IClickHouseSpecificTable<TSource>,IClickHouseSpecificTable<TSource>>> JoinGlobalAsOfTableHintImpl<TSource>()
			where TSource : notnull
		{
			return table => table.JoinHint(Join.GlobalAsOf);
		}

		[ExpressionMethod(nameof(JoinAllHintImpl))]
		public static IClickHouseSpecificQueryable<TSource> JoinAllHint<TSource>(this IClickHouseSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return query.JoinHint(Join.All);
		}

		static Expression<Func<IClickHouseSpecificQueryable<TSource>,IClickHouseSpecificQueryable<TSource>>> JoinAllHintImpl<TSource>()
			where TSource : notnull
		{
			return query => query.JoinHint(Join.All);
		}

		[ExpressionMethod(nameof(JoinAllTableHintImpl))]
		public static IClickHouseSpecificTable<TSource> JoinAllHint<TSource>(this IClickHouseSpecificTable<TSource> table)
			where TSource : notnull
		{
			return table.JoinHint(Join.All);
		}

		static Expression<Func<IClickHouseSpecificTable<TSource>,IClickHouseSpecificTable<TSource>>> JoinAllTableHintImpl<TSource>()
			where TSource : notnull
		{
			return table => table.JoinHint(Join.All);
		}

		[ExpressionMethod(nameof(JoinAllOuterHintImpl))]
		public static IClickHouseSpecificQueryable<TSource> JoinAllOuterHint<TSource>(this IClickHouseSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return query.JoinHint(Join.AllOuter);
		}

		static Expression<Func<IClickHouseSpecificQueryable<TSource>,IClickHouseSpecificQueryable<TSource>>> JoinAllOuterHintImpl<TSource>()
			where TSource : notnull
		{
			return query => query.JoinHint(Join.AllOuter);
		}

		[ExpressionMethod(nameof(JoinAllOuterTableHintImpl))]
		public static IClickHouseSpecificTable<TSource> JoinAllOuterHint<TSource>(this IClickHouseSpecificTable<TSource> table)
			where TSource : notnull
		{
			return table.JoinHint(Join.AllOuter);
		}

		static Expression<Func<IClickHouseSpecificTable<TSource>,IClickHouseSpecificTable<TSource>>> JoinAllOuterTableHintImpl<TSource>()
			where TSource : notnull
		{
			return table => table.JoinHint(Join.AllOuter);
		}

		[ExpressionMethod(nameof(JoinAllSemiHintImpl))]
		public static IClickHouseSpecificQueryable<TSource> JoinAllSemiHint<TSource>(this IClickHouseSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return query.JoinHint(Join.AllSemi);
		}

		static Expression<Func<IClickHouseSpecificQueryable<TSource>,IClickHouseSpecificQueryable<TSource>>> JoinAllSemiHintImpl<TSource>()
			where TSource : notnull
		{
			return query => query.JoinHint(Join.AllSemi);
		}

		[ExpressionMethod(nameof(JoinAllSemiTableHintImpl))]
		public static IClickHouseSpecificTable<TSource> JoinAllSemiHint<TSource>(this IClickHouseSpecificTable<TSource> table)
			where TSource : notnull
		{
			return table.JoinHint(Join.AllSemi);
		}

		static Expression<Func<IClickHouseSpecificTable<TSource>,IClickHouseSpecificTable<TSource>>> JoinAllSemiTableHintImpl<TSource>()
			where TSource : notnull
		{
			return table => table.JoinHint(Join.AllSemi);
		}

		[ExpressionMethod(nameof(JoinAllAntiHintImpl))]
		public static IClickHouseSpecificQueryable<TSource> JoinAllAntiHint<TSource>(this IClickHouseSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return query.JoinHint(Join.AllAnti);
		}

		static Expression<Func<IClickHouseSpecificQueryable<TSource>,IClickHouseSpecificQueryable<TSource>>> JoinAllAntiHintImpl<TSource>()
			where TSource : notnull
		{
			return query => query.JoinHint(Join.AllAnti);
		}

		[ExpressionMethod(nameof(JoinAllAntiTableHintImpl))]
		public static IClickHouseSpecificTable<TSource> JoinAllAntiHint<TSource>(this IClickHouseSpecificTable<TSource> table)
			where TSource : notnull
		{
			return table.JoinHint(Join.AllAnti);
		}

		static Expression<Func<IClickHouseSpecificTable<TSource>,IClickHouseSpecificTable<TSource>>> JoinAllAntiTableHintImpl<TSource>()
			where TSource : notnull
		{
			return table => table.JoinHint(Join.AllAnti);
		}

		[ExpressionMethod(nameof(JoinAllAnyHintImpl))]
		public static IClickHouseSpecificQueryable<TSource> JoinAllAnyHint<TSource>(this IClickHouseSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return query.JoinHint(Join.AllAny);
		}

		static Expression<Func<IClickHouseSpecificQueryable<TSource>,IClickHouseSpecificQueryable<TSource>>> JoinAllAnyHintImpl<TSource>()
			where TSource : notnull
		{
			return query => query.JoinHint(Join.AllAny);
		}

		[ExpressionMethod(nameof(JoinAllAnyTableHintImpl))]
		public static IClickHouseSpecificTable<TSource> JoinAllAnyHint<TSource>(this IClickHouseSpecificTable<TSource> table)
			where TSource : notnull
		{
			return table.JoinHint(Join.AllAny);
		}

		static Expression<Func<IClickHouseSpecificTable<TSource>,IClickHouseSpecificTable<TSource>>> JoinAllAnyTableHintImpl<TSource>()
			where TSource : notnull
		{
			return table => table.JoinHint(Join.AllAny);
		}

		[ExpressionMethod(nameof(JoinAllAsOfHintImpl))]
		public static IClickHouseSpecificQueryable<TSource> JoinAllAsOfHint<TSource>(this IClickHouseSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return query.JoinHint(Join.AllAsOf);
		}

		static Expression<Func<IClickHouseSpecificQueryable<TSource>,IClickHouseSpecificQueryable<TSource>>> JoinAllAsOfHintImpl<TSource>()
			where TSource : notnull
		{
			return query => query.JoinHint(Join.AllAsOf);
		}

		[ExpressionMethod(nameof(JoinAllAsOfTableHintImpl))]
		public static IClickHouseSpecificTable<TSource> JoinAllAsOfHint<TSource>(this IClickHouseSpecificTable<TSource> table)
			where TSource : notnull
		{
			return table.JoinHint(Join.AllAsOf);
		}

		static Expression<Func<IClickHouseSpecificTable<TSource>,IClickHouseSpecificTable<TSource>>> JoinAllAsOfTableHintImpl<TSource>()
			where TSource : notnull
		{
			return table => table.JoinHint(Join.AllAsOf);
		}

	}
}
