#nullable enable
// Generated.
//
using System;
using System.Linq.Expressions;

using LinqToDB.Mapping;

namespace LinqToDB.DataProvider.PostgreSQL
{
	public static partial class PostgreSQLHints
	{
		/// <summary>
		/// Adds a PostgreSQL <c>FOR UPDATE</c> subquery hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=SubQuery; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
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

		/// <summary>
		/// Adds a PostgreSQL <c>FOR UPDATE NOWAIT</c> subquery hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=SubQuery; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
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

		/// <summary>
		/// Adds a PostgreSQL <c>FOR UPDATE SKIP LOCKED</c> subquery hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=SubQuery; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
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

		/// <summary>
		/// Adds a PostgreSQL <c>FOR NO KEY UPDATE</c> subquery hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=SubQuery; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
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

		/// <summary>
		/// Adds a PostgreSQL <c>FOR NO KEY UPDATE NOWAIT</c> subquery hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=SubQuery; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
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

		/// <summary>
		/// Adds a PostgreSQL <c>FOR NO KEY UPDATE SKIP LOCKED</c> subquery hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=SubQuery; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
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

		/// <summary>
		/// Adds a PostgreSQL <c>FOR SHARE</c> subquery hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=SubQuery; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
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

		/// <summary>
		/// Adds a PostgreSQL <c>FOR SHARE NOWAIT</c> subquery hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=SubQuery; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
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

		/// <summary>
		/// Adds a PostgreSQL <c>FOR SHARE SKIP LOCKED</c> subquery hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=SubQuery; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
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

		/// <summary>
		/// Adds a PostgreSQL <c>FOR KEY SHARE</c> subquery hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=SubQuery; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
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

		/// <summary>
		/// Adds a PostgreSQL <c>FOR KEY SHARE NOWAIT</c> subquery hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=SubQuery; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
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

		/// <summary>
		/// Adds a PostgreSQL <c>FOR KEY SHARE SKIP LOCKED</c> subquery hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=SubQuery; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
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
