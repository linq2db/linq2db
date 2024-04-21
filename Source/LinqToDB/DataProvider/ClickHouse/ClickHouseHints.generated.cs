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
		internal static IClickHouseSpecificQueryable<TSource> JoinOuterHint<TSource>(this IClickHouseSpecificQueryable<TSource> query)
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
		internal static IClickHouseSpecificTable<TSource> JoinOuterHint<TSource>(this IClickHouseSpecificTable<TSource> table)
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
		internal static IClickHouseSpecificQueryable<TSource> JoinSemiHint<TSource>(this IClickHouseSpecificQueryable<TSource> query)
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
		internal static IClickHouseSpecificTable<TSource> JoinSemiHint<TSource>(this IClickHouseSpecificTable<TSource> table)
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
		internal static IClickHouseSpecificQueryable<TSource> JoinAntiHint<TSource>(this IClickHouseSpecificQueryable<TSource> query)
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
		internal static IClickHouseSpecificTable<TSource> JoinAntiHint<TSource>(this IClickHouseSpecificTable<TSource> table)
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
		internal static IClickHouseSpecificQueryable<TSource> JoinAnyHint<TSource>(this IClickHouseSpecificQueryable<TSource> query)
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
		internal static IClickHouseSpecificTable<TSource> JoinAnyHint<TSource>(this IClickHouseSpecificTable<TSource> table)
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
		internal static IClickHouseSpecificQueryable<TSource> JoinAsOfHint<TSource>(this IClickHouseSpecificQueryable<TSource> query)
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
		internal static IClickHouseSpecificTable<TSource> JoinAsOfHint<TSource>(this IClickHouseSpecificTable<TSource> table)
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
		internal static IClickHouseSpecificQueryable<TSource> JoinGlobalHint<TSource>(this IClickHouseSpecificQueryable<TSource> query)
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
		internal static IClickHouseSpecificTable<TSource> JoinGlobalHint<TSource>(this IClickHouseSpecificTable<TSource> table)
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
		internal static IClickHouseSpecificQueryable<TSource> JoinGlobalOuterHint<TSource>(this IClickHouseSpecificQueryable<TSource> query)
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
		internal static IClickHouseSpecificTable<TSource> JoinGlobalOuterHint<TSource>(this IClickHouseSpecificTable<TSource> table)
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
		internal static IClickHouseSpecificQueryable<TSource> JoinGlobalSemiHint<TSource>(this IClickHouseSpecificQueryable<TSource> query)
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
		internal static IClickHouseSpecificTable<TSource> JoinGlobalSemiHint<TSource>(this IClickHouseSpecificTable<TSource> table)
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
		internal static IClickHouseSpecificQueryable<TSource> JoinGlobalAntiHint<TSource>(this IClickHouseSpecificQueryable<TSource> query)
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
		internal static IClickHouseSpecificTable<TSource> JoinGlobalAntiHint<TSource>(this IClickHouseSpecificTable<TSource> table)
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
		internal static IClickHouseSpecificQueryable<TSource> JoinGlobalAnyHint<TSource>(this IClickHouseSpecificQueryable<TSource> query)
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
		internal static IClickHouseSpecificTable<TSource> JoinGlobalAnyHint<TSource>(this IClickHouseSpecificTable<TSource> table)
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
		internal static IClickHouseSpecificQueryable<TSource> JoinGlobalAsOfHint<TSource>(this IClickHouseSpecificQueryable<TSource> query)
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
		internal static IClickHouseSpecificTable<TSource> JoinGlobalAsOfHint<TSource>(this IClickHouseSpecificTable<TSource> table)
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
		internal static IClickHouseSpecificQueryable<TSource> JoinAllHint<TSource>(this IClickHouseSpecificQueryable<TSource> query)
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
		internal static IClickHouseSpecificTable<TSource> JoinAllHint<TSource>(this IClickHouseSpecificTable<TSource> table)
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
		internal static IClickHouseSpecificQueryable<TSource> JoinAllOuterHint<TSource>(this IClickHouseSpecificQueryable<TSource> query)
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
		internal static IClickHouseSpecificTable<TSource> JoinAllOuterHint<TSource>(this IClickHouseSpecificTable<TSource> table)
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
		internal static IClickHouseSpecificQueryable<TSource> JoinAllSemiHint<TSource>(this IClickHouseSpecificQueryable<TSource> query)
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
		internal static IClickHouseSpecificTable<TSource> JoinAllSemiHint<TSource>(this IClickHouseSpecificTable<TSource> table)
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
		internal static IClickHouseSpecificQueryable<TSource> JoinAllAntiHint<TSource>(this IClickHouseSpecificQueryable<TSource> query)
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
		internal static IClickHouseSpecificTable<TSource> JoinAllAntiHint<TSource>(this IClickHouseSpecificTable<TSource> table)
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
		internal static IClickHouseSpecificQueryable<TSource> JoinAllAnyHint<TSource>(this IClickHouseSpecificQueryable<TSource> query)
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
		internal static IClickHouseSpecificTable<TSource> JoinAllAnyHint<TSource>(this IClickHouseSpecificTable<TSource> table)
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
		internal static IClickHouseSpecificQueryable<TSource> JoinAllAsOfHint<TSource>(this IClickHouseSpecificQueryable<TSource> query)
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
		internal static IClickHouseSpecificTable<TSource> JoinAllAsOfHint<TSource>(this IClickHouseSpecificTable<TSource> table)
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
