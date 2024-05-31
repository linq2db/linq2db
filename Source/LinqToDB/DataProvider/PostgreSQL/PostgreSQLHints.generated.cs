#nullable enable
// Generated.
//
using System;
using System.Linq.Expressions;

namespace LinqToDB.DataProvider.PostgreSQL
{
	public static partial class PostgreSQLHints
	{
		[ExpressionMethod(nameof(ForUpdateHintImpl))]
		public static IPostgreSQLSpecificQueryable<TSource> ForUpdateHint<TSource>(
			this IPostgreSQLSpecificQueryable<TSource> query,
			params Sql.SqlID[]                         tableIDs)
			where TSource : notnull
		{
			return SubQueryTableHint(query, ForUpdate, tableIDs);
		}
		static Expression<Func<IPostgreSQLSpecificQueryable<TSource>,Sql.SqlID[],IPostgreSQLSpecificQueryable<TSource>>> ForUpdateHintImpl<TSource>()
			where TSource : notnull
		{
			return (query, tableIDs) => SubQueryTableHint(query, ForUpdate, tableIDs);
		}

		[ExpressionMethod(nameof(ForUpdateNoWaitHintImpl))]
		public static IPostgreSQLSpecificQueryable<TSource> ForUpdateNoWaitHint<TSource>(
			this IPostgreSQLSpecificQueryable<TSource> query,
			params Sql.SqlID[]                         tableIDs)
			where TSource : notnull
		{
			return SubQueryTableHint(query, ForUpdate, NoWait, tableIDs);
		}
		static Expression<Func<IPostgreSQLSpecificQueryable<TSource>,Sql.SqlID[],IPostgreSQLSpecificQueryable<TSource>>> ForUpdateNoWaitHintImpl<TSource>()
			where TSource : notnull
		{
			return (query, tableIDs) => SubQueryTableHint(query, ForUpdate, NoWait, tableIDs);
		}

		[ExpressionMethod(nameof(ForUpdateSkipLockedHintImpl))]
		public static IPostgreSQLSpecificQueryable<TSource> ForUpdateSkipLockedHint<TSource>(
			this IPostgreSQLSpecificQueryable<TSource> query,
			params Sql.SqlID[]                         tableIDs)
			where TSource : notnull
		{
			return SubQueryTableHint(query, ForUpdate, SkipLocked, tableIDs);
		}
		static Expression<Func<IPostgreSQLSpecificQueryable<TSource>,Sql.SqlID[],IPostgreSQLSpecificQueryable<TSource>>> ForUpdateSkipLockedHintImpl<TSource>()
			where TSource : notnull
		{
			return (query, tableIDs) => SubQueryTableHint(query, ForUpdate, SkipLocked, tableIDs);
		}

		[ExpressionMethod(nameof(ForNoKeyUpdateHintImpl))]
		public static IPostgreSQLSpecificQueryable<TSource> ForNoKeyUpdateHint<TSource>(
			this IPostgreSQLSpecificQueryable<TSource> query,
			params Sql.SqlID[]                         tableIDs)
			where TSource : notnull
		{
			return SubQueryTableHint(query, ForNoKeyUpdate, tableIDs);
		}
		static Expression<Func<IPostgreSQLSpecificQueryable<TSource>,Sql.SqlID[],IPostgreSQLSpecificQueryable<TSource>>> ForNoKeyUpdateHintImpl<TSource>()
			where TSource : notnull
		{
			return (query, tableIDs) => SubQueryTableHint(query, ForNoKeyUpdate, tableIDs);
		}

		[ExpressionMethod(nameof(ForNoKeyUpdateNoWaitHintImpl))]
		public static IPostgreSQLSpecificQueryable<TSource> ForNoKeyUpdateNoWaitHint<TSource>(
			this IPostgreSQLSpecificQueryable<TSource> query,
			params Sql.SqlID[]                         tableIDs)
			where TSource : notnull
		{
			return SubQueryTableHint(query, ForNoKeyUpdate, NoWait, tableIDs);
		}
		static Expression<Func<IPostgreSQLSpecificQueryable<TSource>,Sql.SqlID[],IPostgreSQLSpecificQueryable<TSource>>> ForNoKeyUpdateNoWaitHintImpl<TSource>()
			where TSource : notnull
		{
			return (query, tableIDs) => SubQueryTableHint(query, ForNoKeyUpdate, NoWait, tableIDs);
		}

		[ExpressionMethod(nameof(ForNoKeyUpdateSkipLockedHintImpl))]
		public static IPostgreSQLSpecificQueryable<TSource> ForNoKeyUpdateSkipLockedHint<TSource>(
			this IPostgreSQLSpecificQueryable<TSource> query,
			params Sql.SqlID[]                         tableIDs)
			where TSource : notnull
		{
			return SubQueryTableHint(query, ForNoKeyUpdate, SkipLocked, tableIDs);
		}
		static Expression<Func<IPostgreSQLSpecificQueryable<TSource>,Sql.SqlID[],IPostgreSQLSpecificQueryable<TSource>>> ForNoKeyUpdateSkipLockedHintImpl<TSource>()
			where TSource : notnull
		{
			return (query, tableIDs) => SubQueryTableHint(query, ForNoKeyUpdate, SkipLocked, tableIDs);
		}

		[ExpressionMethod(nameof(ForShareHintImpl))]
		public static IPostgreSQLSpecificQueryable<TSource> ForShareHint<TSource>(
			this IPostgreSQLSpecificQueryable<TSource> query,
			params Sql.SqlID[]                         tableIDs)
			where TSource : notnull
		{
			return SubQueryTableHint(query, ForShare, tableIDs);
		}
		static Expression<Func<IPostgreSQLSpecificQueryable<TSource>,Sql.SqlID[],IPostgreSQLSpecificQueryable<TSource>>> ForShareHintImpl<TSource>()
			where TSource : notnull
		{
			return (query, tableIDs) => SubQueryTableHint(query, ForShare, tableIDs);
		}

		[ExpressionMethod(nameof(ForShareNoWaitHintImpl))]
		public static IPostgreSQLSpecificQueryable<TSource> ForShareNoWaitHint<TSource>(
			this IPostgreSQLSpecificQueryable<TSource> query,
			params Sql.SqlID[]                         tableIDs)
			where TSource : notnull
		{
			return SubQueryTableHint(query, ForShare, NoWait, tableIDs);
		}
		static Expression<Func<IPostgreSQLSpecificQueryable<TSource>,Sql.SqlID[],IPostgreSQLSpecificQueryable<TSource>>> ForShareNoWaitHintImpl<TSource>()
			where TSource : notnull
		{
			return (query, tableIDs) => SubQueryTableHint(query, ForShare, NoWait, tableIDs);
		}

		[ExpressionMethod(nameof(ForShareSkipLockedHintImpl))]
		public static IPostgreSQLSpecificQueryable<TSource> ForShareSkipLockedHint<TSource>(
			this IPostgreSQLSpecificQueryable<TSource> query,
			params Sql.SqlID[]                         tableIDs)
			where TSource : notnull
		{
			return SubQueryTableHint(query, ForShare, SkipLocked, tableIDs);
		}
		static Expression<Func<IPostgreSQLSpecificQueryable<TSource>,Sql.SqlID[],IPostgreSQLSpecificQueryable<TSource>>> ForShareSkipLockedHintImpl<TSource>()
			where TSource : notnull
		{
			return (query, tableIDs) => SubQueryTableHint(query, ForShare, SkipLocked, tableIDs);
		}

		[ExpressionMethod(nameof(ForKeyShareHintImpl))]
		public static IPostgreSQLSpecificQueryable<TSource> ForKeyShareHint<TSource>(
			this IPostgreSQLSpecificQueryable<TSource> query,
			params Sql.SqlID[]                         tableIDs)
			where TSource : notnull
		{
			return SubQueryTableHint(query, ForKeyShare, tableIDs);
		}
		static Expression<Func<IPostgreSQLSpecificQueryable<TSource>,Sql.SqlID[],IPostgreSQLSpecificQueryable<TSource>>> ForKeyShareHintImpl<TSource>()
			where TSource : notnull
		{
			return (query, tableIDs) => SubQueryTableHint(query, ForKeyShare, tableIDs);
		}

		[ExpressionMethod(nameof(ForKeyShareNoWaitHintImpl))]
		public static IPostgreSQLSpecificQueryable<TSource> ForKeyShareNoWaitHint<TSource>(
			this IPostgreSQLSpecificQueryable<TSource> query,
			params Sql.SqlID[]                         tableIDs)
			where TSource : notnull
		{
			return SubQueryTableHint(query, ForKeyShare, NoWait, tableIDs);
		}
		static Expression<Func<IPostgreSQLSpecificQueryable<TSource>,Sql.SqlID[],IPostgreSQLSpecificQueryable<TSource>>> ForKeyShareNoWaitHintImpl<TSource>()
			where TSource : notnull
		{
			return (query, tableIDs) => SubQueryTableHint(query, ForKeyShare, NoWait, tableIDs);
		}

		[ExpressionMethod(nameof(ForKeyShareSkipLockedHintImpl))]
		public static IPostgreSQLSpecificQueryable<TSource> ForKeyShareSkipLockedHint<TSource>(
			this IPostgreSQLSpecificQueryable<TSource> query,
			params Sql.SqlID[]                         tableIDs)
			where TSource : notnull
		{
			return SubQueryTableHint(query, ForKeyShare, SkipLocked, tableIDs);
		}
		static Expression<Func<IPostgreSQLSpecificQueryable<TSource>,Sql.SqlID[],IPostgreSQLSpecificQueryable<TSource>>> ForKeyShareSkipLockedHintImpl<TSource>()
			where TSource : notnull
		{
			return (query, tableIDs) => SubQueryTableHint(query, ForKeyShare, SkipLocked, tableIDs);
		}

	}
}
