#nullable enable
// Generated.
//
using System;
using System.Linq.Expressions;

using LinqToDB.Mapping;

namespace LinqToDB.DataProvider.ClickHouse
{
	public static partial class ClickHouseHints
	{
		/// <summary>
		/// Adds a ClickHouse <c>OUTER</c> join hint.
		/// </summary>
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

		/// <summary>
		/// Adds a ClickHouse <c>OUTER</c> join hint.
		/// </summary>
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

		/// <summary>
		/// Adds a ClickHouse <c>SEMI</c> join hint.
		/// </summary>
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

		/// <summary>
		/// Adds a ClickHouse <c>SEMI</c> join hint.
		/// </summary>
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

		/// <summary>
		/// Adds a ClickHouse <c>ANTI</c> join hint.
		/// </summary>
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

		/// <summary>
		/// Adds a ClickHouse <c>ANTI</c> join hint.
		/// </summary>
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

		/// <summary>
		/// Adds a ClickHouse <c>ANY</c> join hint.
		/// </summary>
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

		/// <summary>
		/// Adds a ClickHouse <c>ANY</c> join hint.
		/// </summary>
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

		/// <summary>
		/// Adds a ClickHouse <c>ALL</c> join hint.
		/// </summary>
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

		/// <summary>
		/// Adds a ClickHouse <c>ALL</c> join hint.
		/// </summary>
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

		/// <summary>
		/// Adds a ClickHouse <c>ASOF</c> join hint.
		/// </summary>
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

		/// <summary>
		/// Adds a ClickHouse <c>ASOF</c> join hint.
		/// </summary>
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

		/// <summary>
		/// Adds a ClickHouse <c>GLOBAL</c> join hint.
		/// </summary>
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

		/// <summary>
		/// Adds a ClickHouse <c>GLOBAL</c> join hint.
		/// </summary>
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

		/// <summary>
		/// Adds a ClickHouse <c>GLOBAL OUTER</c> join hint.
		/// </summary>
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

		/// <summary>
		/// Adds a ClickHouse <c>GLOBAL OUTER</c> join hint.
		/// </summary>
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

		/// <summary>
		/// Adds a ClickHouse <c>GLOBAL SEMI</c> join hint.
		/// </summary>
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

		/// <summary>
		/// Adds a ClickHouse <c>GLOBAL SEMI</c> join hint.
		/// </summary>
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

		/// <summary>
		/// Adds a ClickHouse <c>GLOBAL ANTI</c> join hint.
		/// </summary>
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

		/// <summary>
		/// Adds a ClickHouse <c>GLOBAL ANTI</c> join hint.
		/// </summary>
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

		/// <summary>
		/// Adds a ClickHouse <c>GLOBAL ANY</c> join hint.
		/// </summary>
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

		/// <summary>
		/// Adds a ClickHouse <c>GLOBAL ANY</c> join hint.
		/// </summary>
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

		/// <summary>
		/// Adds a ClickHouse <c>GLOBAL ALL</c> join hint.
		/// </summary>
		[ExpressionMethod(nameof(JoinGlobalAllHintImpl))]
		public static IClickHouseSpecificQueryable<TSource> JoinGlobalAllHint<TSource>(this IClickHouseSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return query.JoinHint(Join.GlobalAll);
		}

		static Expression<Func<IClickHouseSpecificQueryable<TSource>,IClickHouseSpecificQueryable<TSource>>> JoinGlobalAllHintImpl<TSource>()
			where TSource : notnull
		{
			return query => query.JoinHint(Join.GlobalAll);
		}

		/// <summary>
		/// Adds a ClickHouse <c>GLOBAL ALL</c> join hint.
		/// </summary>
		[ExpressionMethod(nameof(JoinGlobalAllTableHintImpl))]
		public static IClickHouseSpecificTable<TSource> JoinGlobalAllHint<TSource>(this IClickHouseSpecificTable<TSource> table)
			where TSource : notnull
		{
			return table.JoinHint(Join.GlobalAll);
		}

		static Expression<Func<IClickHouseSpecificTable<TSource>,IClickHouseSpecificTable<TSource>>> JoinGlobalAllTableHintImpl<TSource>()
			where TSource : notnull
		{
			return table => table.JoinHint(Join.GlobalAll);
		}

		/// <summary>
		/// Adds a ClickHouse <c>GLOBAL ASOF</c> join hint.
		/// </summary>
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

		/// <summary>
		/// Adds a ClickHouse <c>GLOBAL ASOF</c> join hint.
		/// </summary>
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

		/// <summary>
		/// Adds the same ClickHouse join hint as <c>JoinOuterHint</c>. This method is deprecated.
		/// </summary>
		[Obsolete("TODO: remove in v7. This API is based on an incorrect ClickHouse join hint model and works incorrectly. Use JoinOuterHint instead.")]
		[ExpressionMethod(nameof(JoinAllOuterHintImpl))]
		public static IClickHouseSpecificQueryable<TSource> JoinAllOuterHint<TSource>(this IClickHouseSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return query.JoinHint(Join.Outer);
		}

		static Expression<Func<IClickHouseSpecificQueryable<TSource>,IClickHouseSpecificQueryable<TSource>>> JoinAllOuterHintImpl<TSource>()
			where TSource : notnull
		{
			return query => query.JoinHint(Join.Outer);
		}

		/// <summary>
		/// Adds the same ClickHouse join hint as <c>JoinOuterHint</c>. This method is deprecated.
		/// </summary>
		[Obsolete("TODO: remove in v7. This API is based on an incorrect ClickHouse join hint model and works incorrectly. Use JoinOuterHint instead.")]
		[ExpressionMethod(nameof(JoinAllOuterTableHintImpl))]
		public static IClickHouseSpecificTable<TSource> JoinAllOuterHint<TSource>(this IClickHouseSpecificTable<TSource> table)
			where TSource : notnull
		{
			return table.JoinHint(Join.Outer);
		}

		static Expression<Func<IClickHouseSpecificTable<TSource>,IClickHouseSpecificTable<TSource>>> JoinAllOuterTableHintImpl<TSource>()
			where TSource : notnull
		{
			return table => table.JoinHint(Join.Outer);
		}

		/// <summary>
		/// Adds the same ClickHouse join hint as <c>JoinSemiHint</c>. This method is deprecated.
		/// </summary>
		[Obsolete("TODO: remove in v7. This API is based on an incorrect ClickHouse join hint model and works incorrectly. Use JoinSemiHint instead.")]
		[ExpressionMethod(nameof(JoinAllSemiHintImpl))]
		public static IClickHouseSpecificQueryable<TSource> JoinAllSemiHint<TSource>(this IClickHouseSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return query.JoinHint(Join.Semi);
		}

		static Expression<Func<IClickHouseSpecificQueryable<TSource>,IClickHouseSpecificQueryable<TSource>>> JoinAllSemiHintImpl<TSource>()
			where TSource : notnull
		{
			return query => query.JoinHint(Join.Semi);
		}

		/// <summary>
		/// Adds the same ClickHouse join hint as <c>JoinSemiHint</c>. This method is deprecated.
		/// </summary>
		[Obsolete("TODO: remove in v7. This API is based on an incorrect ClickHouse join hint model and works incorrectly. Use JoinSemiHint instead.")]
		[ExpressionMethod(nameof(JoinAllSemiTableHintImpl))]
		public static IClickHouseSpecificTable<TSource> JoinAllSemiHint<TSource>(this IClickHouseSpecificTable<TSource> table)
			where TSource : notnull
		{
			return table.JoinHint(Join.Semi);
		}

		static Expression<Func<IClickHouseSpecificTable<TSource>,IClickHouseSpecificTable<TSource>>> JoinAllSemiTableHintImpl<TSource>()
			where TSource : notnull
		{
			return table => table.JoinHint(Join.Semi);
		}

		/// <summary>
		/// Adds the same ClickHouse join hint as <c>JoinAntiHint</c>. This method is deprecated.
		/// </summary>
		[Obsolete("TODO: remove in v7. This API is based on an incorrect ClickHouse join hint model and works incorrectly. Use JoinAntiHint instead.")]
		[ExpressionMethod(nameof(JoinAllAntiHintImpl))]
		public static IClickHouseSpecificQueryable<TSource> JoinAllAntiHint<TSource>(this IClickHouseSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return query.JoinHint(Join.Anti);
		}

		static Expression<Func<IClickHouseSpecificQueryable<TSource>,IClickHouseSpecificQueryable<TSource>>> JoinAllAntiHintImpl<TSource>()
			where TSource : notnull
		{
			return query => query.JoinHint(Join.Anti);
		}

		/// <summary>
		/// Adds the same ClickHouse join hint as <c>JoinAntiHint</c>. This method is deprecated.
		/// </summary>
		[Obsolete("TODO: remove in v7. This API is based on an incorrect ClickHouse join hint model and works incorrectly. Use JoinAntiHint instead.")]
		[ExpressionMethod(nameof(JoinAllAntiTableHintImpl))]
		public static IClickHouseSpecificTable<TSource> JoinAllAntiHint<TSource>(this IClickHouseSpecificTable<TSource> table)
			where TSource : notnull
		{
			return table.JoinHint(Join.Anti);
		}

		static Expression<Func<IClickHouseSpecificTable<TSource>,IClickHouseSpecificTable<TSource>>> JoinAllAntiTableHintImpl<TSource>()
			where TSource : notnull
		{
			return table => table.JoinHint(Join.Anti);
		}

		/// <summary>
		/// Adds the same ClickHouse join hint as <c>JoinAnyHint</c>. This method is deprecated.
		/// </summary>
		[Obsolete("TODO: remove in v7. This API is based on an incorrect ClickHouse join hint model and works incorrectly. Use JoinAnyHint instead.")]
		[ExpressionMethod(nameof(JoinAllAnyHintImpl))]
		public static IClickHouseSpecificQueryable<TSource> JoinAllAnyHint<TSource>(this IClickHouseSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return query.JoinHint(Join.Any);
		}

		static Expression<Func<IClickHouseSpecificQueryable<TSource>,IClickHouseSpecificQueryable<TSource>>> JoinAllAnyHintImpl<TSource>()
			where TSource : notnull
		{
			return query => query.JoinHint(Join.Any);
		}

		/// <summary>
		/// Adds the same ClickHouse join hint as <c>JoinAnyHint</c>. This method is deprecated.
		/// </summary>
		[Obsolete("TODO: remove in v7. This API is based on an incorrect ClickHouse join hint model and works incorrectly. Use JoinAnyHint instead.")]
		[ExpressionMethod(nameof(JoinAllAnyTableHintImpl))]
		public static IClickHouseSpecificTable<TSource> JoinAllAnyHint<TSource>(this IClickHouseSpecificTable<TSource> table)
			where TSource : notnull
		{
			return table.JoinHint(Join.Any);
		}

		static Expression<Func<IClickHouseSpecificTable<TSource>,IClickHouseSpecificTable<TSource>>> JoinAllAnyTableHintImpl<TSource>()
			where TSource : notnull
		{
			return table => table.JoinHint(Join.Any);
		}

		/// <summary>
		/// Adds the same ClickHouse join hint as <c>JoinAsOfHint</c>. This method is deprecated.
		/// </summary>
		[Obsolete("TODO: remove in v7. This API is based on an incorrect ClickHouse join hint model and works incorrectly. Use JoinAsOfHint instead.")]
		[ExpressionMethod(nameof(JoinAllAsOfHintImpl))]
		public static IClickHouseSpecificQueryable<TSource> JoinAllAsOfHint<TSource>(this IClickHouseSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return query.JoinHint(Join.AsOf);
		}

		static Expression<Func<IClickHouseSpecificQueryable<TSource>,IClickHouseSpecificQueryable<TSource>>> JoinAllAsOfHintImpl<TSource>()
			where TSource : notnull
		{
			return query => query.JoinHint(Join.AsOf);
		}

		/// <summary>
		/// Adds the same ClickHouse join hint as <c>JoinAsOfHint</c>. This method is deprecated.
		/// </summary>
		[Obsolete("TODO: remove in v7. This API is based on an incorrect ClickHouse join hint model and works incorrectly. Use JoinAsOfHint instead.")]
		[ExpressionMethod(nameof(JoinAllAsOfTableHintImpl))]
		public static IClickHouseSpecificTable<TSource> JoinAllAsOfHint<TSource>(this IClickHouseSpecificTable<TSource> table)
			where TSource : notnull
		{
			return table.JoinHint(Join.AsOf);
		}

		static Expression<Func<IClickHouseSpecificTable<TSource>,IClickHouseSpecificTable<TSource>>> JoinAllAsOfTableHintImpl<TSource>()
			where TSource : notnull
		{
			return table => table.JoinHint(Join.AsOf);
		}

	}
}
