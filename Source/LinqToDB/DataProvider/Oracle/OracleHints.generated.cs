#nullable enable
// Generated.
//
using System;
using System.Linq.Expressions;

using LinqToDB.Mapping;

namespace LinqToDB.DataProvider.Oracle
{
	public static partial class OracleHints
	{
		/// <summary>
		/// Adds an Oracle query hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Query; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(nameof(AllRowsHintImpl))]
		public static IOracleSpecificQueryable<TSource> AllRowsHint<TSource>(this IOracleSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return OracleHints.QueryHint(query, Hint.AllRows);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,IOracleSpecificQueryable<TSource>>> AllRowsHintImpl<TSource>()
			where TSource : notnull
		{
			return query => OracleHints.QueryHint(query, Hint.AllRows);
		}

		/// <summary>
		/// Adds an Oracle query hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Query; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(nameof(FirstRowsHintImpl2))]
		public static IOracleSpecificQueryable<TSource> FirstRowsHint<TSource>(this IOracleSpecificQueryable<TSource> query, int value)
			where TSource : notnull
		{
			return OracleHints.QueryHint(query, Hint.FirstRows(value));
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,int,IOracleSpecificQueryable<TSource>>> FirstRowsHintImpl2<TSource>()
			where TSource : notnull
		{
			return (query, value) => OracleHints.QueryHint(query, Hint.FirstRows(value));
		}

		/// <summary>
		/// Adds an Oracle table hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Table; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.Oracle, nameof(ClusterTableHintImpl))]
		public static IOracleSpecificTable<TSource> ClusterHint<TSource>(this IOracleSpecificTable<TSource> table)
			where TSource : notnull
		{
			return OracleHints.TableHint(table, Hint.Cluster);
		}
		static Expression<Func<IOracleSpecificTable<TSource>,IOracleSpecificTable<TSource>>> ClusterTableHintImpl<TSource>()
			where TSource : notnull
		{
			return table => OracleHints.TableHint(table, Hint.Cluster);
		}

		/// <summary>
		/// Adds an Oracle table hint to tables in the current query scope.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=TablesInScope; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.Oracle, nameof(ClusterInScopeHintImpl))]
		public static IOracleSpecificQueryable<TSource> ClusterInScopeHint<TSource>(this IOracleSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return OracleHints.TablesInScopeHint(query, Hint.Cluster);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,IOracleSpecificQueryable<TSource>>> ClusterInScopeHintImpl<TSource>()
			where TSource : notnull
		{
			return query => OracleHints.TablesInScopeHint(query, Hint.Cluster);
		}

		/// <summary>
		/// Adds an Oracle query hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Query; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(nameof(ClusteringHintImpl))]
		public static IOracleSpecificQueryable<TSource> ClusteringHint<TSource>(this IOracleSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return OracleHints.QueryHint(query, Hint.Clustering);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,IOracleSpecificQueryable<TSource>>> ClusteringHintImpl<TSource>()
			where TSource : notnull
		{
			return query => OracleHints.QueryHint(query, Hint.Clustering);
		}

		/// <summary>
		/// Adds an Oracle query hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Query; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(nameof(NoClusteringHintImpl))]
		public static IOracleSpecificQueryable<TSource> NoClusteringHint<TSource>(this IOracleSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return OracleHints.QueryHint(query, Hint.NoClustering);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,IOracleSpecificQueryable<TSource>>> NoClusteringHintImpl<TSource>()
			where TSource : notnull
		{
			return query => OracleHints.QueryHint(query, Hint.NoClustering);
		}

		/// <summary>
		/// Adds an Oracle table hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Table; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.Oracle, nameof(FullTableHintImpl))]
		public static IOracleSpecificTable<TSource> FullHint<TSource>(this IOracleSpecificTable<TSource> table)
			where TSource : notnull
		{
			return OracleHints.TableHint(table, Hint.Full);
		}
		static Expression<Func<IOracleSpecificTable<TSource>,IOracleSpecificTable<TSource>>> FullTableHintImpl<TSource>()
			where TSource : notnull
		{
			return table => OracleHints.TableHint(table, Hint.Full);
		}

		/// <summary>
		/// Adds an Oracle table hint to tables in the current query scope.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=TablesInScope; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.Oracle, nameof(FullInScopeHintImpl))]
		public static IOracleSpecificQueryable<TSource> FullInScopeHint<TSource>(this IOracleSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return OracleHints.TablesInScopeHint(query, Hint.Full);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,IOracleSpecificQueryable<TSource>>> FullInScopeHintImpl<TSource>()
			where TSource : notnull
		{
			return query => OracleHints.TablesInScopeHint(query, Hint.Full);
		}

		/// <summary>
		/// Adds an Oracle table hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Table; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.Oracle, nameof(HashTableHintImpl))]
		public static IOracleSpecificTable<TSource> HashHint<TSource>(this IOracleSpecificTable<TSource> table)
			where TSource : notnull
		{
			return OracleHints.TableHint(table, Hint.Hash);
		}
		static Expression<Func<IOracleSpecificTable<TSource>,IOracleSpecificTable<TSource>>> HashTableHintImpl<TSource>()
			where TSource : notnull
		{
			return table => OracleHints.TableHint(table, Hint.Hash);
		}

		/// <summary>
		/// Adds an Oracle table hint to tables in the current query scope.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=TablesInScope; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.Oracle, nameof(HashInScopeHintImpl))]
		public static IOracleSpecificQueryable<TSource> HashInScopeHint<TSource>(this IOracleSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return OracleHints.TablesInScopeHint(query, Hint.Hash);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,IOracleSpecificQueryable<TSource>>> HashInScopeHintImpl<TSource>()
			where TSource : notnull
		{
			return query => OracleHints.TablesInScopeHint(query, Hint.Hash);
		}

		/// <summary>
		/// Adds an Oracle index hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Index; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.Oracle, nameof(IndexIndexHintImpl))]
		public static IOracleSpecificTable<TSource> IndexHint<TSource>(this IOracleSpecificTable<TSource> table, params string[] indexNames)
			where TSource : notnull
		{
			return OracleHints.TableHint(table, Hint.Index, indexNames);
		}
		static Expression<Func<IOracleSpecificTable<TSource>,string[],IOracleSpecificTable<TSource>>> IndexIndexHintImpl<TSource>()
			where TSource : notnull
		{
			return (table, indexNames) => OracleHints.TableHint(table, Hint.Index, indexNames);
		}

		/// <summary>
		/// Adds an Oracle index hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Index; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.Oracle, nameof(IndexAscIndexHintImpl))]
		public static IOracleSpecificTable<TSource> IndexAscHint<TSource>(this IOracleSpecificTable<TSource> table, params string[] indexNames)
			where TSource : notnull
		{
			return OracleHints.TableHint(table, Hint.IndexAsc, indexNames);
		}
		static Expression<Func<IOracleSpecificTable<TSource>,string[],IOracleSpecificTable<TSource>>> IndexAscIndexHintImpl<TSource>()
			where TSource : notnull
		{
			return (table, indexNames) => OracleHints.TableHint(table, Hint.IndexAsc, indexNames);
		}

		/// <summary>
		/// Adds an Oracle index hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Index; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.Oracle, nameof(IndexCombineIndexHintImpl))]
		public static IOracleSpecificTable<TSource> IndexCombineHint<TSource>(this IOracleSpecificTable<TSource> table, params string[] indexNames)
			where TSource : notnull
		{
			return OracleHints.TableHint(table, Hint.IndexCombine, indexNames);
		}
		static Expression<Func<IOracleSpecificTable<TSource>,string[],IOracleSpecificTable<TSource>>> IndexCombineIndexHintImpl<TSource>()
			where TSource : notnull
		{
			return (table, indexNames) => OracleHints.TableHint(table, Hint.IndexCombine, indexNames);
		}

		/// <summary>
		/// Adds an Oracle index hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Index; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.Oracle, nameof(IndexJoinIndexHintImpl))]
		public static IOracleSpecificTable<TSource> IndexJoinHint<TSource>(this IOracleSpecificTable<TSource> table, params string[] indexNames)
			where TSource : notnull
		{
			return OracleHints.TableHint(table, Hint.IndexJoin, indexNames);
		}
		static Expression<Func<IOracleSpecificTable<TSource>,string[],IOracleSpecificTable<TSource>>> IndexJoinIndexHintImpl<TSource>()
			where TSource : notnull
		{
			return (table, indexNames) => OracleHints.TableHint(table, Hint.IndexJoin, indexNames);
		}

		/// <summary>
		/// Adds an Oracle index hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Index; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.Oracle, nameof(IndexDescIndexHintImpl))]
		public static IOracleSpecificTable<TSource> IndexDescHint<TSource>(this IOracleSpecificTable<TSource> table, params string[] indexNames)
			where TSource : notnull
		{
			return OracleHints.TableHint(table, Hint.IndexDesc, indexNames);
		}
		static Expression<Func<IOracleSpecificTable<TSource>,string[],IOracleSpecificTable<TSource>>> IndexDescIndexHintImpl<TSource>()
			where TSource : notnull
		{
			return (table, indexNames) => OracleHints.TableHint(table, Hint.IndexDesc, indexNames);
		}

		/// <summary>
		/// Adds an Oracle index hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Index; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.Oracle, nameof(IndexFFSIndexHintImpl))]
		public static IOracleSpecificTable<TSource> IndexFFSHint<TSource>(this IOracleSpecificTable<TSource> table, params string[] indexNames)
			where TSource : notnull
		{
			return OracleHints.TableHint(table, Hint.IndexFFS, indexNames);
		}
		static Expression<Func<IOracleSpecificTable<TSource>,string[],IOracleSpecificTable<TSource>>> IndexFFSIndexHintImpl<TSource>()
			where TSource : notnull
		{
			return (table, indexNames) => OracleHints.TableHint(table, Hint.IndexFFS, indexNames);
		}

		/// <summary>
		/// Adds an Oracle index hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Index; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.Oracle, nameof(IndexFastFullScanIndexHintImpl))]
		public static IOracleSpecificTable<TSource> IndexFastFullScanHint<TSource>(this IOracleSpecificTable<TSource> table, params string[] indexNames)
			where TSource : notnull
		{
			return OracleHints.TableHint(table, Hint.IndexFastFullScan, indexNames);
		}
		static Expression<Func<IOracleSpecificTable<TSource>,string[],IOracleSpecificTable<TSource>>> IndexFastFullScanIndexHintImpl<TSource>()
			where TSource : notnull
		{
			return (table, indexNames) => OracleHints.TableHint(table, Hint.IndexFastFullScan, indexNames);
		}

		/// <summary>
		/// Adds an Oracle index hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Index; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.Oracle, nameof(IndexSSIndexHintImpl))]
		public static IOracleSpecificTable<TSource> IndexSSHint<TSource>(this IOracleSpecificTable<TSource> table, params string[] indexNames)
			where TSource : notnull
		{
			return OracleHints.TableHint(table, Hint.IndexSS, indexNames);
		}
		static Expression<Func<IOracleSpecificTable<TSource>,string[],IOracleSpecificTable<TSource>>> IndexSSIndexHintImpl<TSource>()
			where TSource : notnull
		{
			return (table, indexNames) => OracleHints.TableHint(table, Hint.IndexSS, indexNames);
		}

		/// <summary>
		/// Adds an Oracle index hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Index; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.Oracle, nameof(IndexSkipScanIndexHintImpl))]
		public static IOracleSpecificTable<TSource> IndexSkipScanHint<TSource>(this IOracleSpecificTable<TSource> table, params string[] indexNames)
			where TSource : notnull
		{
			return OracleHints.TableHint(table, Hint.IndexSkipScan, indexNames);
		}
		static Expression<Func<IOracleSpecificTable<TSource>,string[],IOracleSpecificTable<TSource>>> IndexSkipScanIndexHintImpl<TSource>()
			where TSource : notnull
		{
			return (table, indexNames) => OracleHints.TableHint(table, Hint.IndexSkipScan, indexNames);
		}

		/// <summary>
		/// Adds an Oracle index hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Index; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.Oracle, nameof(IndexSSAscIndexHintImpl))]
		public static IOracleSpecificTable<TSource> IndexSSAscHint<TSource>(this IOracleSpecificTable<TSource> table, params string[] indexNames)
			where TSource : notnull
		{
			return OracleHints.TableHint(table, Hint.IndexSSAsc, indexNames);
		}
		static Expression<Func<IOracleSpecificTable<TSource>,string[],IOracleSpecificTable<TSource>>> IndexSSAscIndexHintImpl<TSource>()
			where TSource : notnull
		{
			return (table, indexNames) => OracleHints.TableHint(table, Hint.IndexSSAsc, indexNames);
		}

		/// <summary>
		/// Adds an Oracle index hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Index; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.Oracle, nameof(IndexSkipScanAscIndexHintImpl))]
		public static IOracleSpecificTable<TSource> IndexSkipScanAscHint<TSource>(this IOracleSpecificTable<TSource> table, params string[] indexNames)
			where TSource : notnull
		{
			return OracleHints.TableHint(table, Hint.IndexSkipScanAsc, indexNames);
		}
		static Expression<Func<IOracleSpecificTable<TSource>,string[],IOracleSpecificTable<TSource>>> IndexSkipScanAscIndexHintImpl<TSource>()
			where TSource : notnull
		{
			return (table, indexNames) => OracleHints.TableHint(table, Hint.IndexSkipScanAsc, indexNames);
		}

		/// <summary>
		/// Adds an Oracle index hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Index; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.Oracle, nameof(IndexSSDescIndexHintImpl))]
		public static IOracleSpecificTable<TSource> IndexSSDescHint<TSource>(this IOracleSpecificTable<TSource> table, params string[] indexNames)
			where TSource : notnull
		{
			return OracleHints.TableHint(table, Hint.IndexSSDesc, indexNames);
		}
		static Expression<Func<IOracleSpecificTable<TSource>,string[],IOracleSpecificTable<TSource>>> IndexSSDescIndexHintImpl<TSource>()
			where TSource : notnull
		{
			return (table, indexNames) => OracleHints.TableHint(table, Hint.IndexSSDesc, indexNames);
		}

		/// <summary>
		/// Adds an Oracle index hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Index; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.Oracle, nameof(IndexSkipScanDescIndexHintImpl))]
		public static IOracleSpecificTable<TSource> IndexSkipScanDescHint<TSource>(this IOracleSpecificTable<TSource> table, params string[] indexNames)
			where TSource : notnull
		{
			return OracleHints.TableHint(table, Hint.IndexSkipScanDesc, indexNames);
		}
		static Expression<Func<IOracleSpecificTable<TSource>,string[],IOracleSpecificTable<TSource>>> IndexSkipScanDescIndexHintImpl<TSource>()
			where TSource : notnull
		{
			return (table, indexNames) => OracleHints.TableHint(table, Hint.IndexSkipScanDesc, indexNames);
		}

		/// <summary>
		/// Adds an Oracle query hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Query; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(nameof(NativeFullOuterJoinHintImpl))]
		public static IOracleSpecificQueryable<TSource> NativeFullOuterJoinHint<TSource>(this IOracleSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return OracleHints.QueryHint(query, Hint.NativeFullOuterJoin);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,IOracleSpecificQueryable<TSource>>> NativeFullOuterJoinHintImpl<TSource>()
			where TSource : notnull
		{
			return query => OracleHints.QueryHint(query, Hint.NativeFullOuterJoin);
		}

		/// <summary>
		/// Adds an Oracle query hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Query; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(nameof(NoNativeFullOuterJoinHintImpl))]
		public static IOracleSpecificQueryable<TSource> NoNativeFullOuterJoinHint<TSource>(this IOracleSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return OracleHints.QueryHint(query, Hint.NoNativeFullOuterJoin);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,IOracleSpecificQueryable<TSource>>> NoNativeFullOuterJoinHintImpl<TSource>()
			where TSource : notnull
		{
			return query => OracleHints.QueryHint(query, Hint.NoNativeFullOuterJoin);
		}

		/// <summary>
		/// Adds an Oracle index hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Index; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.Oracle, nameof(NoIndexIndexHintImpl))]
		public static IOracleSpecificTable<TSource> NoIndexHint<TSource>(this IOracleSpecificTable<TSource> table, params string[] indexNames)
			where TSource : notnull
		{
			return OracleHints.TableHint(table, Hint.NoIndex, indexNames);
		}
		static Expression<Func<IOracleSpecificTable<TSource>,string[],IOracleSpecificTable<TSource>>> NoIndexIndexHintImpl<TSource>()
			where TSource : notnull
		{
			return (table, indexNames) => OracleHints.TableHint(table, Hint.NoIndex, indexNames);
		}

		/// <summary>
		/// Adds an Oracle index hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Index; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.Oracle, nameof(NoIndexFFSIndexHintImpl))]
		public static IOracleSpecificTable<TSource> NoIndexFFSHint<TSource>(this IOracleSpecificTable<TSource> table, params string[] indexNames)
			where TSource : notnull
		{
			return OracleHints.TableHint(table, Hint.NoIndexFFS, indexNames);
		}
		static Expression<Func<IOracleSpecificTable<TSource>,string[],IOracleSpecificTable<TSource>>> NoIndexFFSIndexHintImpl<TSource>()
			where TSource : notnull
		{
			return (table, indexNames) => OracleHints.TableHint(table, Hint.NoIndexFFS, indexNames);
		}

		/// <summary>
		/// Adds an Oracle index hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Index; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.Oracle, nameof(NoIndexFastFullScanIndexHintImpl))]
		public static IOracleSpecificTable<TSource> NoIndexFastFullScanHint<TSource>(this IOracleSpecificTable<TSource> table, params string[] indexNames)
			where TSource : notnull
		{
			return OracleHints.TableHint(table, Hint.NoIndexFastFullScan, indexNames);
		}
		static Expression<Func<IOracleSpecificTable<TSource>,string[],IOracleSpecificTable<TSource>>> NoIndexFastFullScanIndexHintImpl<TSource>()
			where TSource : notnull
		{
			return (table, indexNames) => OracleHints.TableHint(table, Hint.NoIndexFastFullScan, indexNames);
		}

		/// <summary>
		/// Adds an Oracle index hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Index; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.Oracle, nameof(NoIndexSSIndexHintImpl))]
		public static IOracleSpecificTable<TSource> NoIndexSSHint<TSource>(this IOracleSpecificTable<TSource> table, params string[] indexNames)
			where TSource : notnull
		{
			return OracleHints.TableHint(table, Hint.NoIndexSS, indexNames);
		}
		static Expression<Func<IOracleSpecificTable<TSource>,string[],IOracleSpecificTable<TSource>>> NoIndexSSIndexHintImpl<TSource>()
			where TSource : notnull
		{
			return (table, indexNames) => OracleHints.TableHint(table, Hint.NoIndexSS, indexNames);
		}

		/// <summary>
		/// Adds an Oracle index hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Index; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.Oracle, nameof(NoIndexSkipScanIndexHintImpl))]
		public static IOracleSpecificTable<TSource> NoIndexSkipScanHint<TSource>(this IOracleSpecificTable<TSource> table, params string[] indexNames)
			where TSource : notnull
		{
			return OracleHints.TableHint(table, Hint.NoIndexSkipScan, indexNames);
		}
		static Expression<Func<IOracleSpecificTable<TSource>,string[],IOracleSpecificTable<TSource>>> NoIndexSkipScanIndexHintImpl<TSource>()
			where TSource : notnull
		{
			return (table, indexNames) => OracleHints.TableHint(table, Hint.NoIndexSkipScan, indexNames);
		}

		/// <summary>
		/// Adds an Oracle table hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Table; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.Oracle, nameof(InMemoryTableHintImpl))]
		public static IOracleSpecificTable<TSource> InMemoryHint<TSource>(this IOracleSpecificTable<TSource> table)
			where TSource : notnull
		{
			return OracleHints.TableHint(table, Hint.InMemory);
		}
		static Expression<Func<IOracleSpecificTable<TSource>,IOracleSpecificTable<TSource>>> InMemoryTableHintImpl<TSource>()
			where TSource : notnull
		{
			return table => OracleHints.TableHint(table, Hint.InMemory);
		}

		/// <summary>
		/// Adds an Oracle table hint to tables in the current query scope.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=TablesInScope; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.Oracle, nameof(InMemoryInScopeHintImpl))]
		public static IOracleSpecificQueryable<TSource> InMemoryInScopeHint<TSource>(this IOracleSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return OracleHints.TablesInScopeHint(query, Hint.InMemory);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,IOracleSpecificQueryable<TSource>>> InMemoryInScopeHintImpl<TSource>()
			where TSource : notnull
		{
			return query => OracleHints.TablesInScopeHint(query, Hint.InMemory);
		}

		/// <summary>
		/// Adds an Oracle table hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Table; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.Oracle, nameof(NoInMemoryTableHintImpl))]
		public static IOracleSpecificTable<TSource> NoInMemoryHint<TSource>(this IOracleSpecificTable<TSource> table)
			where TSource : notnull
		{
			return OracleHints.TableHint(table, Hint.NoInMemory);
		}
		static Expression<Func<IOracleSpecificTable<TSource>,IOracleSpecificTable<TSource>>> NoInMemoryTableHintImpl<TSource>()
			where TSource : notnull
		{
			return table => OracleHints.TableHint(table, Hint.NoInMemory);
		}

		/// <summary>
		/// Adds an Oracle table hint to tables in the current query scope.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=TablesInScope; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.Oracle, nameof(NoInMemoryInScopeHintImpl))]
		public static IOracleSpecificQueryable<TSource> NoInMemoryInScopeHint<TSource>(this IOracleSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return OracleHints.TablesInScopeHint(query, Hint.NoInMemory);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,IOracleSpecificQueryable<TSource>>> NoInMemoryInScopeHintImpl<TSource>()
			where TSource : notnull
		{
			return query => OracleHints.TablesInScopeHint(query, Hint.NoInMemory);
		}

		/// <summary>
		/// Adds an Oracle table hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Table; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.Oracle, nameof(InMemoryPruningTableHintImpl))]
		public static IOracleSpecificTable<TSource> InMemoryPruningHint<TSource>(this IOracleSpecificTable<TSource> table)
			where TSource : notnull
		{
			return OracleHints.TableHint(table, Hint.InMemoryPruning);
		}
		static Expression<Func<IOracleSpecificTable<TSource>,IOracleSpecificTable<TSource>>> InMemoryPruningTableHintImpl<TSource>()
			where TSource : notnull
		{
			return table => OracleHints.TableHint(table, Hint.InMemoryPruning);
		}

		/// <summary>
		/// Adds an Oracle table hint to tables in the current query scope.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=TablesInScope; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.Oracle, nameof(InMemoryPruningInScopeHintImpl))]
		public static IOracleSpecificQueryable<TSource> InMemoryPruningInScopeHint<TSource>(this IOracleSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return OracleHints.TablesInScopeHint(query, Hint.InMemoryPruning);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,IOracleSpecificQueryable<TSource>>> InMemoryPruningInScopeHintImpl<TSource>()
			where TSource : notnull
		{
			return query => OracleHints.TablesInScopeHint(query, Hint.InMemoryPruning);
		}

		/// <summary>
		/// Adds an Oracle table hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Table; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.Oracle, nameof(NoInMemoryPruningTableHintImpl))]
		public static IOracleSpecificTable<TSource> NoInMemoryPruningHint<TSource>(this IOracleSpecificTable<TSource> table)
			where TSource : notnull
		{
			return OracleHints.TableHint(table, Hint.NoInMemoryPruning);
		}
		static Expression<Func<IOracleSpecificTable<TSource>,IOracleSpecificTable<TSource>>> NoInMemoryPruningTableHintImpl<TSource>()
			where TSource : notnull
		{
			return table => OracleHints.TableHint(table, Hint.NoInMemoryPruning);
		}

		/// <summary>
		/// Adds an Oracle table hint to tables in the current query scope.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=TablesInScope; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.Oracle, nameof(NoInMemoryPruningInScopeHintImpl))]
		public static IOracleSpecificQueryable<TSource> NoInMemoryPruningInScopeHint<TSource>(this IOracleSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return OracleHints.TablesInScopeHint(query, Hint.NoInMemoryPruning);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,IOracleSpecificQueryable<TSource>>> NoInMemoryPruningInScopeHintImpl<TSource>()
			where TSource : notnull
		{
			return query => OracleHints.TablesInScopeHint(query, Hint.NoInMemoryPruning);
		}

		/// <summary>
		/// Adds an Oracle query hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Query; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(nameof(UseBandHintImpl4))]
		public static IOracleSpecificQueryable<TSource> UseBandHint<TSource>(this IOracleSpecificQueryable<TSource> query, params Sql.SqlID[] tableIDs)
			where TSource : notnull
		{
			return OracleHints.QueryHint(query, Hint.UseBand, tableIDs);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,Sql.SqlID[],IOracleSpecificQueryable<TSource>>> UseBandHintImpl4<TSource>()
			where TSource : notnull
		{
			return (query, tableIDs) => OracleHints.QueryHint(query, Hint.UseBand, tableIDs);
		}

		/// <summary>
		/// Adds an Oracle query hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Query; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(nameof(NoUseBandHintImpl4))]
		public static IOracleSpecificQueryable<TSource> NoUseBandHint<TSource>(this IOracleSpecificQueryable<TSource> query, params Sql.SqlID[] tableIDs)
			where TSource : notnull
		{
			return OracleHints.QueryHint(query, Hint.NoUseBand, tableIDs);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,Sql.SqlID[],IOracleSpecificQueryable<TSource>>> NoUseBandHintImpl4<TSource>()
			where TSource : notnull
		{
			return (query, tableIDs) => OracleHints.QueryHint(query, Hint.NoUseBand, tableIDs);
		}

		/// <summary>
		/// Adds an Oracle query hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Query; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(nameof(UseCubeHintImpl4))]
		public static IOracleSpecificQueryable<TSource> UseCubeHint<TSource>(this IOracleSpecificQueryable<TSource> query, params Sql.SqlID[] tableIDs)
			where TSource : notnull
		{
			return OracleHints.QueryHint(query, Hint.UseCube, tableIDs);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,Sql.SqlID[],IOracleSpecificQueryable<TSource>>> UseCubeHintImpl4<TSource>()
			where TSource : notnull
		{
			return (query, tableIDs) => OracleHints.QueryHint(query, Hint.UseCube, tableIDs);
		}

		/// <summary>
		/// Adds an Oracle query hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Query; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(nameof(NoUseCubeHintImpl4))]
		public static IOracleSpecificQueryable<TSource> NoUseCubeHint<TSource>(this IOracleSpecificQueryable<TSource> query, params Sql.SqlID[] tableIDs)
			where TSource : notnull
		{
			return OracleHints.QueryHint(query, Hint.NoUseCube, tableIDs);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,Sql.SqlID[],IOracleSpecificQueryable<TSource>>> NoUseCubeHintImpl4<TSource>()
			where TSource : notnull
		{
			return (query, tableIDs) => OracleHints.QueryHint(query, Hint.NoUseCube, tableIDs);
		}

		/// <summary>
		/// Adds an Oracle query hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Query; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(nameof(UseHashHintImpl4))]
		public static IOracleSpecificQueryable<TSource> UseHashHint<TSource>(this IOracleSpecificQueryable<TSource> query, params Sql.SqlID[] tableIDs)
			where TSource : notnull
		{
			return OracleHints.QueryHint(query, Hint.UseHash, tableIDs);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,Sql.SqlID[],IOracleSpecificQueryable<TSource>>> UseHashHintImpl4<TSource>()
			where TSource : notnull
		{
			return (query, tableIDs) => OracleHints.QueryHint(query, Hint.UseHash, tableIDs);
		}

		/// <summary>
		/// Adds an Oracle query hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Query; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(nameof(NoUseHashHintImpl4))]
		public static IOracleSpecificQueryable<TSource> NoUseHashHint<TSource>(this IOracleSpecificQueryable<TSource> query, params Sql.SqlID[] tableIDs)
			where TSource : notnull
		{
			return OracleHints.QueryHint(query, Hint.NoUseHash, tableIDs);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,Sql.SqlID[],IOracleSpecificQueryable<TSource>>> NoUseHashHintImpl4<TSource>()
			where TSource : notnull
		{
			return (query, tableIDs) => OracleHints.QueryHint(query, Hint.NoUseHash, tableIDs);
		}

		/// <summary>
		/// Adds an Oracle query hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Query; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(nameof(UseMergeHintImpl4))]
		public static IOracleSpecificQueryable<TSource> UseMergeHint<TSource>(this IOracleSpecificQueryable<TSource> query, params Sql.SqlID[] tableIDs)
			where TSource : notnull
		{
			return OracleHints.QueryHint(query, Hint.UseMerge, tableIDs);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,Sql.SqlID[],IOracleSpecificQueryable<TSource>>> UseMergeHintImpl4<TSource>()
			where TSource : notnull
		{
			return (query, tableIDs) => OracleHints.QueryHint(query, Hint.UseMerge, tableIDs);
		}

		/// <summary>
		/// Adds an Oracle query hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Query; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(nameof(NoUseMergeHintImpl4))]
		public static IOracleSpecificQueryable<TSource> NoUseMergeHint<TSource>(this IOracleSpecificQueryable<TSource> query, params Sql.SqlID[] tableIDs)
			where TSource : notnull
		{
			return OracleHints.QueryHint(query, Hint.NoUseMerge, tableIDs);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,Sql.SqlID[],IOracleSpecificQueryable<TSource>>> NoUseMergeHintImpl4<TSource>()
			where TSource : notnull
		{
			return (query, tableIDs) => OracleHints.QueryHint(query, Hint.NoUseMerge, tableIDs);
		}

		/// <summary>
		/// Adds an Oracle query hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Query; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(nameof(UseNLHintImpl4))]
		public static IOracleSpecificQueryable<TSource> UseNLHint<TSource>(this IOracleSpecificQueryable<TSource> query, params Sql.SqlID[] tableIDs)
			where TSource : notnull
		{
			return OracleHints.QueryHint(query, Hint.UseNL, tableIDs);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,Sql.SqlID[],IOracleSpecificQueryable<TSource>>> UseNLHintImpl4<TSource>()
			where TSource : notnull
		{
			return (query, tableIDs) => OracleHints.QueryHint(query, Hint.UseNL, tableIDs);
		}

		/// <summary>
		/// Adds an Oracle query hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Query; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(nameof(UseNestedLoopHintImpl4))]
		public static IOracleSpecificQueryable<TSource> UseNestedLoopHint<TSource>(this IOracleSpecificQueryable<TSource> query, params Sql.SqlID[] tableIDs)
			where TSource : notnull
		{
			return OracleHints.QueryHint(query, Hint.UseNestedLoop, tableIDs);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,Sql.SqlID[],IOracleSpecificQueryable<TSource>>> UseNestedLoopHintImpl4<TSource>()
			where TSource : notnull
		{
			return (query, tableIDs) => OracleHints.QueryHint(query, Hint.UseNestedLoop, tableIDs);
		}

		/// <summary>
		/// Adds an Oracle query hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Query; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(nameof(NoUseNLHintImpl4))]
		public static IOracleSpecificQueryable<TSource> NoUseNLHint<TSource>(this IOracleSpecificQueryable<TSource> query, params Sql.SqlID[] tableIDs)
			where TSource : notnull
		{
			return OracleHints.QueryHint(query, Hint.NoUseNL, tableIDs);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,Sql.SqlID[],IOracleSpecificQueryable<TSource>>> NoUseNLHintImpl4<TSource>()
			where TSource : notnull
		{
			return (query, tableIDs) => OracleHints.QueryHint(query, Hint.NoUseNL, tableIDs);
		}

		/// <summary>
		/// Adds an Oracle query hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Query; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(nameof(NoUseNestedLoopHintImpl4))]
		public static IOracleSpecificQueryable<TSource> NoUseNestedLoopHint<TSource>(this IOracleSpecificQueryable<TSource> query, params Sql.SqlID[] tableIDs)
			where TSource : notnull
		{
			return OracleHints.QueryHint(query, Hint.NoUseNestedLoop, tableIDs);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,Sql.SqlID[],IOracleSpecificQueryable<TSource>>> NoUseNestedLoopHintImpl4<TSource>()
			where TSource : notnull
		{
			return (query, tableIDs) => OracleHints.QueryHint(query, Hint.NoUseNestedLoop, tableIDs);
		}

		/// <summary>
		/// Adds an Oracle index hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Index; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.Oracle, nameof(UseNLWithIndexIndexHintImpl))]
		public static IOracleSpecificTable<TSource> UseNLWithIndexHint<TSource>(this IOracleSpecificTable<TSource> table, params string[] indexNames)
			where TSource : notnull
		{
			return OracleHints.TableHint(table, Hint.UseNLWithIndex, indexNames);
		}
		static Expression<Func<IOracleSpecificTable<TSource>,string[],IOracleSpecificTable<TSource>>> UseNLWithIndexIndexHintImpl<TSource>()
			where TSource : notnull
		{
			return (table, indexNames) => OracleHints.TableHint(table, Hint.UseNLWithIndex, indexNames);
		}

		/// <summary>
		/// Adds an Oracle index hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Index; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.Oracle, nameof(UseNestedLoopWithIndexIndexHintImpl))]
		public static IOracleSpecificTable<TSource> UseNestedLoopWithIndexHint<TSource>(this IOracleSpecificTable<TSource> table, params string[] indexNames)
			where TSource : notnull
		{
			return OracleHints.TableHint(table, Hint.UseNestedLoopWithIndex, indexNames);
		}
		static Expression<Func<IOracleSpecificTable<TSource>,string[],IOracleSpecificTable<TSource>>> UseNestedLoopWithIndexIndexHintImpl<TSource>()
			where TSource : notnull
		{
			return (table, indexNames) => OracleHints.TableHint(table, Hint.UseNestedLoopWithIndex, indexNames);
		}

		/// <summary>
		/// Adds an Oracle query hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Query; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(nameof(EnableParallelDmlHintImpl))]
		public static IOracleSpecificQueryable<TSource> EnableParallelDmlHint<TSource>(this IOracleSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return OracleHints.QueryHint(query, Hint.EnableParallelDml);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,IOracleSpecificQueryable<TSource>>> EnableParallelDmlHintImpl<TSource>()
			where TSource : notnull
		{
			return query => OracleHints.QueryHint(query, Hint.EnableParallelDml);
		}

		/// <summary>
		/// Adds an Oracle query hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Query; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(nameof(DisableParallelDmlHintImpl))]
		public static IOracleSpecificQueryable<TSource> DisableParallelDmlHint<TSource>(this IOracleSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return OracleHints.QueryHint(query, Hint.DisableParallelDml);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,IOracleSpecificQueryable<TSource>>> DisableParallelDmlHintImpl<TSource>()
			where TSource : notnull
		{
			return query => OracleHints.QueryHint(query, Hint.DisableParallelDml);
		}

		/// <summary>
		/// Adds an Oracle query hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Query; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(nameof(PQConcurrentUnionHintImpl))]
		public static IOracleSpecificQueryable<TSource> PQConcurrentUnionHint<TSource>(this IOracleSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return OracleHints.QueryHint(query, Hint.PQConcurrentUnion);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,IOracleSpecificQueryable<TSource>>> PQConcurrentUnionHintImpl<TSource>()
			where TSource : notnull
		{
			return query => OracleHints.QueryHint(query, Hint.PQConcurrentUnion);
		}

		/// <summary>
		/// Adds an Oracle query hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Query; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(nameof(PQConcurrentUnionHintImpl3))]
		public static IOracleSpecificQueryable<TSource> PQConcurrentUnionHint<TSource>(this IOracleSpecificQueryable<TSource> query, string queryBlock)
			where TSource : notnull
		{
			return OracleHints.QueryHint(query, Hint.PQConcurrentUnion, queryBlock);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,string,IOracleSpecificQueryable<TSource>>> PQConcurrentUnionHintImpl3<TSource>()
			where TSource : notnull
		{
			return (query, queryBlock) => OracleHints.QueryHint(query, Hint.PQConcurrentUnion, queryBlock);
		}

		/// <summary>
		/// Adds an Oracle query hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Query; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(nameof(NoPQConcurrentUnionHintImpl))]
		public static IOracleSpecificQueryable<TSource> NoPQConcurrentUnionHint<TSource>(this IOracleSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return OracleHints.QueryHint(query, Hint.NoPQConcurrentUnion);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,IOracleSpecificQueryable<TSource>>> NoPQConcurrentUnionHintImpl<TSource>()
			where TSource : notnull
		{
			return query => OracleHints.QueryHint(query, Hint.NoPQConcurrentUnion);
		}

		/// <summary>
		/// Adds an Oracle query hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Query; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(nameof(NoPQConcurrentUnionHintImpl3))]
		public static IOracleSpecificQueryable<TSource> NoPQConcurrentUnionHint<TSource>(this IOracleSpecificQueryable<TSource> query, string queryBlock)
			where TSource : notnull
		{
			return OracleHints.QueryHint(query, Hint.NoPQConcurrentUnion, queryBlock);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,string,IOracleSpecificQueryable<TSource>>> NoPQConcurrentUnionHintImpl3<TSource>()
			where TSource : notnull
		{
			return (query, queryBlock) => OracleHints.QueryHint(query, Hint.NoPQConcurrentUnion, queryBlock);
		}

		/// <summary>
		/// Adds an Oracle query hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Query; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(nameof(PQFilterSerialHintImpl))]
		public static IOracleSpecificQueryable<TSource> PQFilterSerialHint<TSource>(this IOracleSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return OracleHints.QueryHint(query, Hint.PQFilterSerial);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,IOracleSpecificQueryable<TSource>>> PQFilterSerialHintImpl<TSource>()
			where TSource : notnull
		{
			return query => OracleHints.QueryHint(query, Hint.PQFilterSerial);
		}

		/// <summary>
		/// Adds an Oracle query hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Query; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(nameof(PQFilterNoneHintImpl))]
		public static IOracleSpecificQueryable<TSource> PQFilterNoneHint<TSource>(this IOracleSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return OracleHints.QueryHint(query, Hint.PQFilterNone);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,IOracleSpecificQueryable<TSource>>> PQFilterNoneHintImpl<TSource>()
			where TSource : notnull
		{
			return query => OracleHints.QueryHint(query, Hint.PQFilterNone);
		}

		/// <summary>
		/// Adds an Oracle query hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Query; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(nameof(PQFilterHashHintImpl))]
		public static IOracleSpecificQueryable<TSource> PQFilterHashHint<TSource>(this IOracleSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return OracleHints.QueryHint(query, Hint.PQFilterHash);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,IOracleSpecificQueryable<TSource>>> PQFilterHashHintImpl<TSource>()
			where TSource : notnull
		{
			return query => OracleHints.QueryHint(query, Hint.PQFilterHash);
		}

		/// <summary>
		/// Adds an Oracle query hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Query; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(nameof(PQFilterRandomHintImpl))]
		public static IOracleSpecificQueryable<TSource> PQFilterRandomHint<TSource>(this IOracleSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return OracleHints.QueryHint(query, Hint.PQFilterRandom);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,IOracleSpecificQueryable<TSource>>> PQFilterRandomHintImpl<TSource>()
			where TSource : notnull
		{
			return query => OracleHints.QueryHint(query, Hint.PQFilterRandom);
		}

		/// <summary>
		/// Adds an Oracle table hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Table; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.Oracle, nameof(PQSkewTableHintImpl))]
		public static IOracleSpecificTable<TSource> PQSkewHint<TSource>(this IOracleSpecificTable<TSource> table)
			where TSource : notnull
		{
			return OracleHints.TableHint(table, Hint.PQSkew);
		}
		static Expression<Func<IOracleSpecificTable<TSource>,IOracleSpecificTable<TSource>>> PQSkewTableHintImpl<TSource>()
			where TSource : notnull
		{
			return table => OracleHints.TableHint(table, Hint.PQSkew);
		}

		/// <summary>
		/// Adds an Oracle table hint to tables in the current query scope.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=TablesInScope; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.Oracle, nameof(PQSkewInScopeHintImpl))]
		public static IOracleSpecificQueryable<TSource> PQSkewInScopeHint<TSource>(this IOracleSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return OracleHints.TablesInScopeHint(query, Hint.PQSkew);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,IOracleSpecificQueryable<TSource>>> PQSkewInScopeHintImpl<TSource>()
			where TSource : notnull
		{
			return query => OracleHints.TablesInScopeHint(query, Hint.PQSkew);
		}

		/// <summary>
		/// Adds an Oracle table hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Table; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.Oracle, nameof(NoPQSkewTableHintImpl))]
		public static IOracleSpecificTable<TSource> NoPQSkewHint<TSource>(this IOracleSpecificTable<TSource> table)
			where TSource : notnull
		{
			return OracleHints.TableHint(table, Hint.NoPQSkew);
		}
		static Expression<Func<IOracleSpecificTable<TSource>,IOracleSpecificTable<TSource>>> NoPQSkewTableHintImpl<TSource>()
			where TSource : notnull
		{
			return table => OracleHints.TableHint(table, Hint.NoPQSkew);
		}

		/// <summary>
		/// Adds an Oracle table hint to tables in the current query scope.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=TablesInScope; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.Oracle, nameof(NoPQSkewInScopeHintImpl))]
		public static IOracleSpecificQueryable<TSource> NoPQSkewInScopeHint<TSource>(this IOracleSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return OracleHints.TablesInScopeHint(query, Hint.NoPQSkew);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,IOracleSpecificQueryable<TSource>>> NoPQSkewInScopeHintImpl<TSource>()
			where TSource : notnull
		{
			return query => OracleHints.TablesInScopeHint(query, Hint.NoPQSkew);
		}

		/// <summary>
		/// Adds an Oracle query hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Query; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(nameof(NoQueryTransformationHintImpl))]
		public static IOracleSpecificQueryable<TSource> NoQueryTransformationHint<TSource>(this IOracleSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return OracleHints.QueryHint(query, Hint.NoQueryTransformation);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,IOracleSpecificQueryable<TSource>>> NoQueryTransformationHintImpl<TSource>()
			where TSource : notnull
		{
			return query => OracleHints.QueryHint(query, Hint.NoQueryTransformation);
		}

		/// <summary>
		/// Adds an Oracle query hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Query; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(nameof(UseConcatHintImpl))]
		public static IOracleSpecificQueryable<TSource> UseConcatHint<TSource>(this IOracleSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return OracleHints.QueryHint(query, Hint.UseConcat);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,IOracleSpecificQueryable<TSource>>> UseConcatHintImpl<TSource>()
			where TSource : notnull
		{
			return query => OracleHints.QueryHint(query, Hint.UseConcat);
		}

		/// <summary>
		/// Adds an Oracle query hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Query; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(nameof(UseConcatHintImpl3))]
		public static IOracleSpecificQueryable<TSource> UseConcatHint<TSource>(this IOracleSpecificQueryable<TSource> query, string queryBlock)
			where TSource : notnull
		{
			return OracleHints.QueryHint(query, Hint.UseConcat, queryBlock);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,string,IOracleSpecificQueryable<TSource>>> UseConcatHintImpl3<TSource>()
			where TSource : notnull
		{
			return (query, queryBlock) => OracleHints.QueryHint(query, Hint.UseConcat, queryBlock);
		}

		/// <summary>
		/// Adds an Oracle query hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Query; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(nameof(NoExpandHintImpl))]
		public static IOracleSpecificQueryable<TSource> NoExpandHint<TSource>(this IOracleSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return OracleHints.QueryHint(query, Hint.NoExpand);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,IOracleSpecificQueryable<TSource>>> NoExpandHintImpl<TSource>()
			where TSource : notnull
		{
			return query => OracleHints.QueryHint(query, Hint.NoExpand);
		}

		/// <summary>
		/// Adds an Oracle query hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Query; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(nameof(NoExpandHintImpl3))]
		public static IOracleSpecificQueryable<TSource> NoExpandHint<TSource>(this IOracleSpecificQueryable<TSource> query, string queryBlock)
			where TSource : notnull
		{
			return OracleHints.QueryHint(query, Hint.NoExpand, queryBlock);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,string,IOracleSpecificQueryable<TSource>>> NoExpandHintImpl3<TSource>()
			where TSource : notnull
		{
			return (query, queryBlock) => OracleHints.QueryHint(query, Hint.NoExpand, queryBlock);
		}

		/// <summary>
		/// Adds an Oracle query hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Query; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(nameof(RewriteHintImpl))]
		public static IOracleSpecificQueryable<TSource> RewriteHint<TSource>(this IOracleSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return OracleHints.QueryHint(query, Hint.Rewrite);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,IOracleSpecificQueryable<TSource>>> RewriteHintImpl<TSource>()
			where TSource : notnull
		{
			return query => OracleHints.QueryHint(query, Hint.Rewrite);
		}

		/// <summary>
		/// Adds an Oracle query hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Query; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(nameof(RewriteHintImpl3))]
		public static IOracleSpecificQueryable<TSource> RewriteHint<TSource>(this IOracleSpecificQueryable<TSource> query, string queryBlock)
			where TSource : notnull
		{
			return OracleHints.QueryHint(query, Hint.Rewrite, queryBlock);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,string,IOracleSpecificQueryable<TSource>>> RewriteHintImpl3<TSource>()
			where TSource : notnull
		{
			return (query, queryBlock) => OracleHints.QueryHint(query, Hint.Rewrite, queryBlock);
		}

		/// <summary>
		/// Adds an Oracle query hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Query; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(nameof(NoRewriteHintImpl))]
		public static IOracleSpecificQueryable<TSource> NoRewriteHint<TSource>(this IOracleSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return OracleHints.QueryHint(query, Hint.NoRewrite);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,IOracleSpecificQueryable<TSource>>> NoRewriteHintImpl<TSource>()
			where TSource : notnull
		{
			return query => OracleHints.QueryHint(query, Hint.NoRewrite);
		}

		/// <summary>
		/// Adds an Oracle query hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Query; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(nameof(NoRewriteHintImpl3))]
		public static IOracleSpecificQueryable<TSource> NoRewriteHint<TSource>(this IOracleSpecificQueryable<TSource> query, string queryBlock)
			where TSource : notnull
		{
			return OracleHints.QueryHint(query, Hint.NoRewrite, queryBlock);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,string,IOracleSpecificQueryable<TSource>>> NoRewriteHintImpl3<TSource>()
			where TSource : notnull
		{
			return (query, queryBlock) => OracleHints.QueryHint(query, Hint.NoRewrite, queryBlock);
		}

		/// <summary>
		/// Adds an Oracle query hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Query; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(nameof(MergeHintImpl))]
		public static IOracleSpecificQueryable<TSource> MergeHint<TSource>(this IOracleSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return OracleHints.QueryHint(query, Hint.Merge);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,IOracleSpecificQueryable<TSource>>> MergeHintImpl<TSource>()
			where TSource : notnull
		{
			return query => OracleHints.QueryHint(query, Hint.Merge);
		}

		/// <summary>
		/// Adds an Oracle query hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Query; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(nameof(MergeHintImpl3))]
		public static IOracleSpecificQueryable<TSource> MergeHint<TSource>(this IOracleSpecificQueryable<TSource> query, string queryBlock)
			where TSource : notnull
		{
			return OracleHints.QueryHint(query, Hint.Merge, queryBlock);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,string,IOracleSpecificQueryable<TSource>>> MergeHintImpl3<TSource>()
			where TSource : notnull
		{
			return (query, queryBlock) => OracleHints.QueryHint(query, Hint.Merge, queryBlock);
		}

		/// <summary>
		/// Adds an Oracle table hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Table; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.Oracle, nameof(MergeTableHintImpl))]
		public static IOracleSpecificTable<TSource> MergeHint<TSource>(this IOracleSpecificTable<TSource> table)
			where TSource : notnull
		{
			return OracleHints.TableHint(table, Hint.Merge);
		}
		static Expression<Func<IOracleSpecificTable<TSource>,IOracleSpecificTable<TSource>>> MergeTableHintImpl<TSource>()
			where TSource : notnull
		{
			return table => OracleHints.TableHint(table, Hint.Merge);
		}

		/// <summary>
		/// Adds an Oracle table hint to tables in the current query scope.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=TablesInScope; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.Oracle, nameof(MergeInScopeHintImpl))]
		public static IOracleSpecificQueryable<TSource> MergeInScopeHint<TSource>(this IOracleSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return OracleHints.TablesInScopeHint(query, Hint.Merge);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,IOracleSpecificQueryable<TSource>>> MergeInScopeHintImpl<TSource>()
			where TSource : notnull
		{
			return query => OracleHints.TablesInScopeHint(query, Hint.Merge);
		}

		/// <summary>
		/// Adds an Oracle query hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Query; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(nameof(NoMergeHintImpl))]
		public static IOracleSpecificQueryable<TSource> NoMergeHint<TSource>(this IOracleSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return OracleHints.QueryHint(query, Hint.NoMerge);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,IOracleSpecificQueryable<TSource>>> NoMergeHintImpl<TSource>()
			where TSource : notnull
		{
			return query => OracleHints.QueryHint(query, Hint.NoMerge);
		}

		/// <summary>
		/// Adds an Oracle query hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Query; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(nameof(NoMergeHintImpl3))]
		public static IOracleSpecificQueryable<TSource> NoMergeHint<TSource>(this IOracleSpecificQueryable<TSource> query, string queryBlock)
			where TSource : notnull
		{
			return OracleHints.QueryHint(query, Hint.NoMerge, queryBlock);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,string,IOracleSpecificQueryable<TSource>>> NoMergeHintImpl3<TSource>()
			where TSource : notnull
		{
			return (query, queryBlock) => OracleHints.QueryHint(query, Hint.NoMerge, queryBlock);
		}

		/// <summary>
		/// Adds an Oracle table hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Table; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.Oracle, nameof(NoMergeTableHintImpl))]
		public static IOracleSpecificTable<TSource> NoMergeHint<TSource>(this IOracleSpecificTable<TSource> table)
			where TSource : notnull
		{
			return OracleHints.TableHint(table, Hint.NoMerge);
		}
		static Expression<Func<IOracleSpecificTable<TSource>,IOracleSpecificTable<TSource>>> NoMergeTableHintImpl<TSource>()
			where TSource : notnull
		{
			return table => OracleHints.TableHint(table, Hint.NoMerge);
		}

		/// <summary>
		/// Adds an Oracle table hint to tables in the current query scope.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=TablesInScope; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.Oracle, nameof(NoMergeInScopeHintImpl))]
		public static IOracleSpecificQueryable<TSource> NoMergeInScopeHint<TSource>(this IOracleSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return OracleHints.TablesInScopeHint(query, Hint.NoMerge);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,IOracleSpecificQueryable<TSource>>> NoMergeInScopeHintImpl<TSource>()
			where TSource : notnull
		{
			return query => OracleHints.TablesInScopeHint(query, Hint.NoMerge);
		}

		/// <summary>
		/// Adds an Oracle query hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Query; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(nameof(StarTransformationHintImpl))]
		public static IOracleSpecificQueryable<TSource> StarTransformationHint<TSource>(this IOracleSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return OracleHints.QueryHint(query, Hint.StarTransformation);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,IOracleSpecificQueryable<TSource>>> StarTransformationHintImpl<TSource>()
			where TSource : notnull
		{
			return query => OracleHints.QueryHint(query, Hint.StarTransformation);
		}

		/// <summary>
		/// Adds an Oracle query hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Query; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(nameof(StarTransformationHintImpl3))]
		public static IOracleSpecificQueryable<TSource> StarTransformationHint<TSource>(this IOracleSpecificQueryable<TSource> query, string queryBlock)
			where TSource : notnull
		{
			return OracleHints.QueryHint(query, Hint.StarTransformation, queryBlock);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,string,IOracleSpecificQueryable<TSource>>> StarTransformationHintImpl3<TSource>()
			where TSource : notnull
		{
			return (query, queryBlock) => OracleHints.QueryHint(query, Hint.StarTransformation, queryBlock);
		}

		/// <summary>
		/// Adds an Oracle query hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Query; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(nameof(NoStarTransformationHintImpl))]
		public static IOracleSpecificQueryable<TSource> NoStarTransformationHint<TSource>(this IOracleSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return OracleHints.QueryHint(query, Hint.NoStarTransformation);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,IOracleSpecificQueryable<TSource>>> NoStarTransformationHintImpl<TSource>()
			where TSource : notnull
		{
			return query => OracleHints.QueryHint(query, Hint.NoStarTransformation);
		}

		/// <summary>
		/// Adds an Oracle query hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Query; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(nameof(NoStarTransformationHintImpl3))]
		public static IOracleSpecificQueryable<TSource> NoStarTransformationHint<TSource>(this IOracleSpecificQueryable<TSource> query, string queryBlock)
			where TSource : notnull
		{
			return OracleHints.QueryHint(query, Hint.NoStarTransformation, queryBlock);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,string,IOracleSpecificQueryable<TSource>>> NoStarTransformationHintImpl3<TSource>()
			where TSource : notnull
		{
			return (query, queryBlock) => OracleHints.QueryHint(query, Hint.NoStarTransformation, queryBlock);
		}

		/// <summary>
		/// Adds an Oracle table hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Table; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.Oracle, nameof(FactTableHintImpl))]
		public static IOracleSpecificTable<TSource> FactHint<TSource>(this IOracleSpecificTable<TSource> table)
			where TSource : notnull
		{
			return OracleHints.TableHint(table, Hint.Fact);
		}
		static Expression<Func<IOracleSpecificTable<TSource>,IOracleSpecificTable<TSource>>> FactTableHintImpl<TSource>()
			where TSource : notnull
		{
			return table => OracleHints.TableHint(table, Hint.Fact);
		}

		/// <summary>
		/// Adds an Oracle table hint to tables in the current query scope.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=TablesInScope; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.Oracle, nameof(FactInScopeHintImpl))]
		public static IOracleSpecificQueryable<TSource> FactInScopeHint<TSource>(this IOracleSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return OracleHints.TablesInScopeHint(query, Hint.Fact);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,IOracleSpecificQueryable<TSource>>> FactInScopeHintImpl<TSource>()
			where TSource : notnull
		{
			return query => OracleHints.TablesInScopeHint(query, Hint.Fact);
		}

		/// <summary>
		/// Adds an Oracle table hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Table; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.Oracle, nameof(NoFactTableHintImpl))]
		public static IOracleSpecificTable<TSource> NoFactHint<TSource>(this IOracleSpecificTable<TSource> table)
			where TSource : notnull
		{
			return OracleHints.TableHint(table, Hint.NoFact);
		}
		static Expression<Func<IOracleSpecificTable<TSource>,IOracleSpecificTable<TSource>>> NoFactTableHintImpl<TSource>()
			where TSource : notnull
		{
			return table => OracleHints.TableHint(table, Hint.NoFact);
		}

		/// <summary>
		/// Adds an Oracle table hint to tables in the current query scope.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=TablesInScope; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.Oracle, nameof(NoFactInScopeHintImpl))]
		public static IOracleSpecificQueryable<TSource> NoFactInScopeHint<TSource>(this IOracleSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return OracleHints.TablesInScopeHint(query, Hint.NoFact);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,IOracleSpecificQueryable<TSource>>> NoFactInScopeHintImpl<TSource>()
			where TSource : notnull
		{
			return query => OracleHints.TablesInScopeHint(query, Hint.NoFact);
		}

		/// <summary>
		/// Adds an Oracle query hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Query; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(nameof(UnnestHintImpl))]
		public static IOracleSpecificQueryable<TSource> UnnestHint<TSource>(this IOracleSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return OracleHints.QueryHint(query, Hint.Unnest);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,IOracleSpecificQueryable<TSource>>> UnnestHintImpl<TSource>()
			where TSource : notnull
		{
			return query => OracleHints.QueryHint(query, Hint.Unnest);
		}

		/// <summary>
		/// Adds an Oracle query hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Query; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(nameof(UnnestHintImpl3))]
		public static IOracleSpecificQueryable<TSource> UnnestHint<TSource>(this IOracleSpecificQueryable<TSource> query, string queryBlock)
			where TSource : notnull
		{
			return OracleHints.QueryHint(query, Hint.Unnest, queryBlock);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,string,IOracleSpecificQueryable<TSource>>> UnnestHintImpl3<TSource>()
			where TSource : notnull
		{
			return (query, queryBlock) => OracleHints.QueryHint(query, Hint.Unnest, queryBlock);
		}

		/// <summary>
		/// Adds an Oracle query hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Query; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(nameof(NoUnnestHintImpl))]
		public static IOracleSpecificQueryable<TSource> NoUnnestHint<TSource>(this IOracleSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return OracleHints.QueryHint(query, Hint.NoUnnest);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,IOracleSpecificQueryable<TSource>>> NoUnnestHintImpl<TSource>()
			where TSource : notnull
		{
			return query => OracleHints.QueryHint(query, Hint.NoUnnest);
		}

		/// <summary>
		/// Adds an Oracle query hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Query; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(nameof(NoUnnestHintImpl3))]
		public static IOracleSpecificQueryable<TSource> NoUnnestHint<TSource>(this IOracleSpecificQueryable<TSource> query, string queryBlock)
			where TSource : notnull
		{
			return OracleHints.QueryHint(query, Hint.NoUnnest, queryBlock);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,string,IOracleSpecificQueryable<TSource>>> NoUnnestHintImpl3<TSource>()
			where TSource : notnull
		{
			return (query, queryBlock) => OracleHints.QueryHint(query, Hint.NoUnnest, queryBlock);
		}

		/// <summary>
		/// Adds an Oracle query hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Query; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(nameof(LeadingHintImpl4))]
		public static IOracleSpecificQueryable<TSource> LeadingHint<TSource>(this IOracleSpecificQueryable<TSource> query, params Sql.SqlID[] tableIDs)
			where TSource : notnull
		{
			return OracleHints.QueryHint(query, Hint.Leading, tableIDs);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,Sql.SqlID[],IOracleSpecificQueryable<TSource>>> LeadingHintImpl4<TSource>()
			where TSource : notnull
		{
			return (query, tableIDs) => OracleHints.QueryHint(query, Hint.Leading, tableIDs);
		}

		/// <summary>
		/// Adds an Oracle query hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Query; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(nameof(OrderedHintImpl))]
		public static IOracleSpecificQueryable<TSource> OrderedHint<TSource>(this IOracleSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return OracleHints.QueryHint(query, Hint.Ordered);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,IOracleSpecificQueryable<TSource>>> OrderedHintImpl<TSource>()
			where TSource : notnull
		{
			return query => OracleHints.QueryHint(query, Hint.Ordered);
		}

		/// <summary>
		/// Adds an Oracle query hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Query; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(nameof(ParallelHintImpl))]
		public static IOracleSpecificQueryable<TSource> ParallelHint<TSource>(this IOracleSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return OracleHints.QueryHint(query, Hint.Parallel);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,IOracleSpecificQueryable<TSource>>> ParallelHintImpl<TSource>()
			where TSource : notnull
		{
			return query => OracleHints.QueryHint(query, Hint.Parallel);
		}

		/// <summary>
		/// Adds an Oracle table hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Table; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.Oracle, nameof(NoParallelTableHintImpl))]
		public static IOracleSpecificTable<TSource> NoParallelHint<TSource>(this IOracleSpecificTable<TSource> table)
			where TSource : notnull
		{
			return OracleHints.TableHint(table, Hint.NoParallel);
		}
		static Expression<Func<IOracleSpecificTable<TSource>,IOracleSpecificTable<TSource>>> NoParallelTableHintImpl<TSource>()
			where TSource : notnull
		{
			return table => OracleHints.TableHint(table, Hint.NoParallel);
		}

		/// <summary>
		/// Adds an Oracle table hint to tables in the current query scope.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=TablesInScope; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.Oracle, nameof(NoParallelInScopeHintImpl))]
		public static IOracleSpecificQueryable<TSource> NoParallelInScopeHint<TSource>(this IOracleSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return OracleHints.TablesInScopeHint(query, Hint.NoParallel);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,IOracleSpecificQueryable<TSource>>> NoParallelInScopeHintImpl<TSource>()
			where TSource : notnull
		{
			return query => OracleHints.TablesInScopeHint(query, Hint.NoParallel);
		}

		/// <summary>
		/// Adds an Oracle query hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Query; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(nameof(AppendHintImpl))]
		public static IOracleSpecificQueryable<TSource> AppendHint<TSource>(this IOracleSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return OracleHints.QueryHint(query, Hint.Append);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,IOracleSpecificQueryable<TSource>>> AppendHintImpl<TSource>()
			where TSource : notnull
		{
			return query => OracleHints.QueryHint(query, Hint.Append);
		}

		/// <summary>
		/// Adds an Oracle query hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Query; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(nameof(AppendValuesHintImpl))]
		public static IOracleSpecificQueryable<TSource> AppendValuesHint<TSource>(this IOracleSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return OracleHints.QueryHint(query, Hint.AppendValues);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,IOracleSpecificQueryable<TSource>>> AppendValuesHintImpl<TSource>()
			where TSource : notnull
		{
			return query => OracleHints.QueryHint(query, Hint.AppendValues);
		}

		/// <summary>
		/// Adds an Oracle query hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Query; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(nameof(NoAppendHintImpl))]
		public static IOracleSpecificQueryable<TSource> NoAppendHint<TSource>(this IOracleSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return OracleHints.QueryHint(query, Hint.NoAppend);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,IOracleSpecificQueryable<TSource>>> NoAppendHintImpl<TSource>()
			where TSource : notnull
		{
			return query => OracleHints.QueryHint(query, Hint.NoAppend);
		}

		/// <summary>
		/// Adds an Oracle table hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Table; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.Oracle, nameof(CacheTableHintImpl))]
		public static IOracleSpecificTable<TSource> CacheHint<TSource>(this IOracleSpecificTable<TSource> table)
			where TSource : notnull
		{
			return OracleHints.TableHint(table, Hint.Cache);
		}
		static Expression<Func<IOracleSpecificTable<TSource>,IOracleSpecificTable<TSource>>> CacheTableHintImpl<TSource>()
			where TSource : notnull
		{
			return table => OracleHints.TableHint(table, Hint.Cache);
		}

		/// <summary>
		/// Adds an Oracle table hint to tables in the current query scope.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=TablesInScope; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.Oracle, nameof(CacheInScopeHintImpl))]
		public static IOracleSpecificQueryable<TSource> CacheInScopeHint<TSource>(this IOracleSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return OracleHints.TablesInScopeHint(query, Hint.Cache);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,IOracleSpecificQueryable<TSource>>> CacheInScopeHintImpl<TSource>()
			where TSource : notnull
		{
			return query => OracleHints.TablesInScopeHint(query, Hint.Cache);
		}

		/// <summary>
		/// Adds an Oracle table hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Table; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.Oracle, nameof(NoCacheTableHintImpl))]
		public static IOracleSpecificTable<TSource> NoCacheHint<TSource>(this IOracleSpecificTable<TSource> table)
			where TSource : notnull
		{
			return OracleHints.TableHint(table, Hint.NoCache);
		}
		static Expression<Func<IOracleSpecificTable<TSource>,IOracleSpecificTable<TSource>>> NoCacheTableHintImpl<TSource>()
			where TSource : notnull
		{
			return table => OracleHints.TableHint(table, Hint.NoCache);
		}

		/// <summary>
		/// Adds an Oracle table hint to tables in the current query scope.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=TablesInScope; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.Oracle, nameof(NoCacheInScopeHintImpl))]
		public static IOracleSpecificQueryable<TSource> NoCacheInScopeHint<TSource>(this IOracleSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return OracleHints.TablesInScopeHint(query, Hint.NoCache);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,IOracleSpecificQueryable<TSource>>> NoCacheInScopeHintImpl<TSource>()
			where TSource : notnull
		{
			return query => OracleHints.TablesInScopeHint(query, Hint.NoCache);
		}

		/// <summary>
		/// Adds an Oracle query hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Query; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(nameof(PushPredicateHintImpl))]
		public static IOracleSpecificQueryable<TSource> PushPredicateHint<TSource>(this IOracleSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return OracleHints.QueryHint(query, Hint.PushPredicate);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,IOracleSpecificQueryable<TSource>>> PushPredicateHintImpl<TSource>()
			where TSource : notnull
		{
			return query => OracleHints.QueryHint(query, Hint.PushPredicate);
		}

		/// <summary>
		/// Adds an Oracle query hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Query; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(nameof(PushPredicateHintImpl3))]
		public static IOracleSpecificQueryable<TSource> PushPredicateHint<TSource>(this IOracleSpecificQueryable<TSource> query, string queryBlock)
			where TSource : notnull
		{
			return OracleHints.QueryHint(query, Hint.PushPredicate, queryBlock);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,string,IOracleSpecificQueryable<TSource>>> PushPredicateHintImpl3<TSource>()
			where TSource : notnull
		{
			return (query, queryBlock) => OracleHints.QueryHint(query, Hint.PushPredicate, queryBlock);
		}

		/// <summary>
		/// Adds an Oracle table hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Table; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.Oracle, nameof(PushPredicateTableHintImpl))]
		public static IOracleSpecificTable<TSource> PushPredicateHint<TSource>(this IOracleSpecificTable<TSource> table)
			where TSource : notnull
		{
			return OracleHints.TableHint(table, Hint.PushPredicate);
		}
		static Expression<Func<IOracleSpecificTable<TSource>,IOracleSpecificTable<TSource>>> PushPredicateTableHintImpl<TSource>()
			where TSource : notnull
		{
			return table => OracleHints.TableHint(table, Hint.PushPredicate);
		}

		/// <summary>
		/// Adds an Oracle table hint to tables in the current query scope.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=TablesInScope; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.Oracle, nameof(PushPredicateInScopeHintImpl))]
		public static IOracleSpecificQueryable<TSource> PushPredicateInScopeHint<TSource>(this IOracleSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return OracleHints.TablesInScopeHint(query, Hint.PushPredicate);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,IOracleSpecificQueryable<TSource>>> PushPredicateInScopeHintImpl<TSource>()
			where TSource : notnull
		{
			return query => OracleHints.TablesInScopeHint(query, Hint.PushPredicate);
		}

		/// <summary>
		/// Adds an Oracle query hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Query; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(nameof(NoPushPredicateHintImpl))]
		public static IOracleSpecificQueryable<TSource> NoPushPredicateHint<TSource>(this IOracleSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return OracleHints.QueryHint(query, Hint.NoPushPredicate);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,IOracleSpecificQueryable<TSource>>> NoPushPredicateHintImpl<TSource>()
			where TSource : notnull
		{
			return query => OracleHints.QueryHint(query, Hint.NoPushPredicate);
		}

		/// <summary>
		/// Adds an Oracle query hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Query; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(nameof(NoPushPredicateHintImpl3))]
		public static IOracleSpecificQueryable<TSource> NoPushPredicateHint<TSource>(this IOracleSpecificQueryable<TSource> query, string queryBlock)
			where TSource : notnull
		{
			return OracleHints.QueryHint(query, Hint.NoPushPredicate, queryBlock);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,string,IOracleSpecificQueryable<TSource>>> NoPushPredicateHintImpl3<TSource>()
			where TSource : notnull
		{
			return (query, queryBlock) => OracleHints.QueryHint(query, Hint.NoPushPredicate, queryBlock);
		}

		/// <summary>
		/// Adds an Oracle table hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Table; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.Oracle, nameof(NoPushPredicateTableHintImpl))]
		public static IOracleSpecificTable<TSource> NoPushPredicateHint<TSource>(this IOracleSpecificTable<TSource> table)
			where TSource : notnull
		{
			return OracleHints.TableHint(table, Hint.NoPushPredicate);
		}
		static Expression<Func<IOracleSpecificTable<TSource>,IOracleSpecificTable<TSource>>> NoPushPredicateTableHintImpl<TSource>()
			where TSource : notnull
		{
			return table => OracleHints.TableHint(table, Hint.NoPushPredicate);
		}

		/// <summary>
		/// Adds an Oracle table hint to tables in the current query scope.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=TablesInScope; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.Oracle, nameof(NoPushPredicateInScopeHintImpl))]
		public static IOracleSpecificQueryable<TSource> NoPushPredicateInScopeHint<TSource>(this IOracleSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return OracleHints.TablesInScopeHint(query, Hint.NoPushPredicate);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,IOracleSpecificQueryable<TSource>>> NoPushPredicateInScopeHintImpl<TSource>()
			where TSource : notnull
		{
			return query => OracleHints.TablesInScopeHint(query, Hint.NoPushPredicate);
		}

		/// <summary>
		/// Adds an Oracle query hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Query; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(nameof(PushSubQueriesHintImpl3))]
		public static IOracleSpecificQueryable<TSource> PushSubQueriesHint<TSource>(this IOracleSpecificQueryable<TSource> query, string queryBlock)
			where TSource : notnull
		{
			return OracleHints.QueryHint(query, Hint.PushSubQueries, queryBlock);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,string,IOracleSpecificQueryable<TSource>>> PushSubQueriesHintImpl3<TSource>()
			where TSource : notnull
		{
			return (query, queryBlock) => OracleHints.QueryHint(query, Hint.PushSubQueries, queryBlock);
		}

		/// <summary>
		/// Adds an Oracle query hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Query; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(nameof(NoPushSubQueriesHintImpl3))]
		public static IOracleSpecificQueryable<TSource> NoPushSubQueriesHint<TSource>(this IOracleSpecificQueryable<TSource> query, string queryBlock)
			where TSource : notnull
		{
			return OracleHints.QueryHint(query, Hint.NoPushSubQueries, queryBlock);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,string,IOracleSpecificQueryable<TSource>>> NoPushSubQueriesHintImpl3<TSource>()
			where TSource : notnull
		{
			return (query, queryBlock) => OracleHints.QueryHint(query, Hint.NoPushSubQueries, queryBlock);
		}

		/// <summary>
		/// Adds an Oracle query hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Query; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(nameof(CursorSharingExactHintImpl))]
		public static IOracleSpecificQueryable<TSource> CursorSharingExactHint<TSource>(this IOracleSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return OracleHints.QueryHint(query, Hint.CursorSharingExact);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,IOracleSpecificQueryable<TSource>>> CursorSharingExactHintImpl<TSource>()
			where TSource : notnull
		{
			return query => OracleHints.QueryHint(query, Hint.CursorSharingExact);
		}

		/// <summary>
		/// Adds an Oracle table hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Table; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.Oracle, nameof(DrivingSiteTableHintImpl))]
		public static IOracleSpecificTable<TSource> DrivingSiteHint<TSource>(this IOracleSpecificTable<TSource> table)
			where TSource : notnull
		{
			return OracleHints.TableHint(table, Hint.DrivingSite);
		}
		static Expression<Func<IOracleSpecificTable<TSource>,IOracleSpecificTable<TSource>>> DrivingSiteTableHintImpl<TSource>()
			where TSource : notnull
		{
			return table => OracleHints.TableHint(table, Hint.DrivingSite);
		}

		/// <summary>
		/// Adds an Oracle table hint to tables in the current query scope.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=TablesInScope; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.Oracle, nameof(DrivingSiteInScopeHintImpl))]
		public static IOracleSpecificQueryable<TSource> DrivingSiteInScopeHint<TSource>(this IOracleSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return OracleHints.TablesInScopeHint(query, Hint.DrivingSite);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,IOracleSpecificQueryable<TSource>>> DrivingSiteInScopeHintImpl<TSource>()
			where TSource : notnull
		{
			return query => OracleHints.TablesInScopeHint(query, Hint.DrivingSite);
		}

		/// <summary>
		/// Adds an Oracle query hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Query; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(nameof(ModelMinAnalysisHintImpl))]
		public static IOracleSpecificQueryable<TSource> ModelMinAnalysisHint<TSource>(this IOracleSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return OracleHints.QueryHint(query, Hint.ModelMinAnalysis);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,IOracleSpecificQueryable<TSource>>> ModelMinAnalysisHintImpl<TSource>()
			where TSource : notnull
		{
			return query => OracleHints.QueryHint(query, Hint.ModelMinAnalysis);
		}

		/// <summary>
		/// Adds an Oracle table hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Table; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.Oracle, nameof(PxJoinFilterTableHintImpl))]
		public static IOracleSpecificTable<TSource> PxJoinFilterHint<TSource>(this IOracleSpecificTable<TSource> table)
			where TSource : notnull
		{
			return OracleHints.TableHint(table, Hint.PxJoinFilter);
		}
		static Expression<Func<IOracleSpecificTable<TSource>,IOracleSpecificTable<TSource>>> PxJoinFilterTableHintImpl<TSource>()
			where TSource : notnull
		{
			return table => OracleHints.TableHint(table, Hint.PxJoinFilter);
		}

		/// <summary>
		/// Adds an Oracle table hint to tables in the current query scope.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=TablesInScope; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.Oracle, nameof(PxJoinFilterInScopeHintImpl))]
		public static IOracleSpecificQueryable<TSource> PxJoinFilterInScopeHint<TSource>(this IOracleSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return OracleHints.TablesInScopeHint(query, Hint.PxJoinFilter);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,IOracleSpecificQueryable<TSource>>> PxJoinFilterInScopeHintImpl<TSource>()
			where TSource : notnull
		{
			return query => OracleHints.TablesInScopeHint(query, Hint.PxJoinFilter);
		}

		/// <summary>
		/// Adds an Oracle table hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Table; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.Oracle, nameof(NoPxJoinFilterTableHintImpl))]
		public static IOracleSpecificTable<TSource> NoPxJoinFilterHint<TSource>(this IOracleSpecificTable<TSource> table)
			where TSource : notnull
		{
			return OracleHints.TableHint(table, Hint.NoPxJoinFilter);
		}
		static Expression<Func<IOracleSpecificTable<TSource>,IOracleSpecificTable<TSource>>> NoPxJoinFilterTableHintImpl<TSource>()
			where TSource : notnull
		{
			return table => OracleHints.TableHint(table, Hint.NoPxJoinFilter);
		}

		/// <summary>
		/// Adds an Oracle table hint to tables in the current query scope.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=TablesInScope; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.Oracle, nameof(NoPxJoinFilterInScopeHintImpl))]
		public static IOracleSpecificQueryable<TSource> NoPxJoinFilterInScopeHint<TSource>(this IOracleSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return OracleHints.TablesInScopeHint(query, Hint.NoPxJoinFilter);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,IOracleSpecificQueryable<TSource>>> NoPxJoinFilterInScopeHintImpl<TSource>()
			where TSource : notnull
		{
			return query => OracleHints.TablesInScopeHint(query, Hint.NoPxJoinFilter);
		}

		/// <summary>
		/// Adds an Oracle query hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Query; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(nameof(NoXmlQueryRewriteHintImpl))]
		public static IOracleSpecificQueryable<TSource> NoXmlQueryRewriteHint<TSource>(this IOracleSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return OracleHints.QueryHint(query, Hint.NoXmlQueryRewrite);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,IOracleSpecificQueryable<TSource>>> NoXmlQueryRewriteHintImpl<TSource>()
			where TSource : notnull
		{
			return query => OracleHints.QueryHint(query, Hint.NoXmlQueryRewrite);
		}

		/// <summary>
		/// Adds an Oracle query hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Query; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(nameof(NoXmlIndexRewriteHintImpl))]
		public static IOracleSpecificQueryable<TSource> NoXmlIndexRewriteHint<TSource>(this IOracleSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return OracleHints.QueryHint(query, Hint.NoXmlIndexRewrite);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,IOracleSpecificQueryable<TSource>>> NoXmlIndexRewriteHintImpl<TSource>()
			where TSource : notnull
		{
			return query => OracleHints.QueryHint(query, Hint.NoXmlIndexRewrite);
		}

		/// <summary>
		/// Adds an Oracle query hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Query; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(nameof(FreshMaterializedViewHintImpl))]
		public static IOracleSpecificQueryable<TSource> FreshMaterializedViewHint<TSource>(this IOracleSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return OracleHints.QueryHint(query, Hint.FreshMaterializedView);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,IOracleSpecificQueryable<TSource>>> FreshMaterializedViewHintImpl<TSource>()
			where TSource : notnull
		{
			return query => OracleHints.QueryHint(query, Hint.FreshMaterializedView);
		}

		/// <summary>
		/// Adds an Oracle query hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Query; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(nameof(FreshMVHintImpl))]
		public static IOracleSpecificQueryable<TSource> FreshMVHint<TSource>(this IOracleSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return OracleHints.QueryHint(query, Hint.FreshMV);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,IOracleSpecificQueryable<TSource>>> FreshMVHintImpl<TSource>()
			where TSource : notnull
		{
			return query => OracleHints.QueryHint(query, Hint.FreshMV);
		}

		/// <summary>
		/// Adds an Oracle query hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Query; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(nameof(GroupingHintImpl))]
		public static IOracleSpecificQueryable<TSource> GroupingHint<TSource>(this IOracleSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return OracleHints.QueryHint(query, Hint.Grouping);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,IOracleSpecificQueryable<TSource>>> GroupingHintImpl<TSource>()
			where TSource : notnull
		{
			return query => OracleHints.QueryHint(query, Hint.Grouping);
		}

		/// <summary>
		/// Adds an Oracle query hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Query; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(nameof(MonitorHintImpl))]
		public static IOracleSpecificQueryable<TSource> MonitorHint<TSource>(this IOracleSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return OracleHints.QueryHint(query, Hint.Monitor);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,IOracleSpecificQueryable<TSource>>> MonitorHintImpl<TSource>()
			where TSource : notnull
		{
			return query => OracleHints.QueryHint(query, Hint.Monitor);
		}

		/// <summary>
		/// Adds an Oracle query hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Query; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(nameof(NoMonitorHintImpl))]
		public static IOracleSpecificQueryable<TSource> NoMonitorHint<TSource>(this IOracleSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return OracleHints.QueryHint(query, Hint.NoMonitor);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,IOracleSpecificQueryable<TSource>>> NoMonitorHintImpl<TSource>()
			where TSource : notnull
		{
			return query => OracleHints.QueryHint(query, Hint.NoMonitor);
		}

	}
}
