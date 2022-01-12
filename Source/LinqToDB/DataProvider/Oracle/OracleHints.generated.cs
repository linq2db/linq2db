// Generated.
//
using System;
using System.Linq.Expressions;

namespace LinqToDB.DataProvider.Oracle
{
	public static partial class OracleHints
	{
		[ExpressionMethod(ProviderName.Oracle, nameof(FullTableHintImpl))]
		public static IOracleSpecificTable<TSource> FullHint<TSource>(this IOracleSpecificTable<TSource> table)
			where TSource : notnull
		{
			return OracleHints.TableHint(table, Table.Full);
		}
		static Expression<Func<IOracleSpecificTable<TSource>,IOracleSpecificTable<TSource>>> FullTableHintImpl<TSource>()
			where TSource : notnull
		{
			return table => OracleHints.TableHint(table, Table.Full);
		}

		[ExpressionMethod(ProviderName.Oracle, nameof(FullInScopeHintImpl))]
		public static IOracleSpecificQueryable<TSource> FullInScopeHint<TSource>(this IOracleSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return OracleHints.TablesInScopeHint(query, Table.Full);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,IOracleSpecificQueryable<TSource>>> FullInScopeHintImpl<TSource>()
			where TSource : notnull
		{
			return query => OracleHints.TablesInScopeHint(query, Table.Full);
		}

		[ExpressionMethod(ProviderName.Oracle, nameof(ClusterTableHintImpl))]
		public static IOracleSpecificTable<TSource> ClusterHint<TSource>(this IOracleSpecificTable<TSource> table)
			where TSource : notnull
		{
			return OracleHints.TableHint(table, Table.Cluster);
		}
		static Expression<Func<IOracleSpecificTable<TSource>,IOracleSpecificTable<TSource>>> ClusterTableHintImpl<TSource>()
			where TSource : notnull
		{
			return table => OracleHints.TableHint(table, Table.Cluster);
		}

		[ExpressionMethod(ProviderName.Oracle, nameof(ClusterInScopeHintImpl))]
		public static IOracleSpecificQueryable<TSource> ClusterInScopeHint<TSource>(this IOracleSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return OracleHints.TablesInScopeHint(query, Table.Cluster);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,IOracleSpecificQueryable<TSource>>> ClusterInScopeHintImpl<TSource>()
			where TSource : notnull
		{
			return query => OracleHints.TablesInScopeHint(query, Table.Cluster);
		}

		[ExpressionMethod(ProviderName.Oracle, nameof(HashTableHintImpl))]
		public static IOracleSpecificTable<TSource> HashHint<TSource>(this IOracleSpecificTable<TSource> table)
			where TSource : notnull
		{
			return OracleHints.TableHint(table, Table.Hash);
		}
		static Expression<Func<IOracleSpecificTable<TSource>,IOracleSpecificTable<TSource>>> HashTableHintImpl<TSource>()
			where TSource : notnull
		{
			return table => OracleHints.TableHint(table, Table.Hash);
		}

		[ExpressionMethod(ProviderName.Oracle, nameof(HashInScopeHintImpl))]
		public static IOracleSpecificQueryable<TSource> HashInScopeHint<TSource>(this IOracleSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return OracleHints.TablesInScopeHint(query, Table.Hash);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,IOracleSpecificQueryable<TSource>>> HashInScopeHintImpl<TSource>()
			where TSource : notnull
		{
			return query => OracleHints.TablesInScopeHint(query, Table.Hash);
		}

		[ExpressionMethod(ProviderName.Oracle, nameof(IndexIndexHintImpl))]
		public static IOracleSpecificTable<TSource> IndexHint<TSource>(this IOracleSpecificTable<TSource> table, params string[] indexNames)
			where TSource : notnull
		{
			return OracleHints.TableHint(table, Table.Index, indexNames);
		}
		static Expression<Func<IOracleSpecificTable<TSource>,string[],IOracleSpecificTable<TSource>>> IndexIndexHintImpl<TSource>()
			where TSource : notnull
		{
			return (table, indexNames) => OracleHints.TableHint(table, Table.Index, indexNames);
		}

		[ExpressionMethod(ProviderName.Oracle, nameof(IndexAscIndexHintImpl))]
		public static IOracleSpecificTable<TSource> IndexAscHint<TSource>(this IOracleSpecificTable<TSource> table, params string[] indexNames)
			where TSource : notnull
		{
			return OracleHints.TableHint(table, Table.IndexAsc, indexNames);
		}
		static Expression<Func<IOracleSpecificTable<TSource>,string[],IOracleSpecificTable<TSource>>> IndexAscIndexHintImpl<TSource>()
			where TSource : notnull
		{
			return (table, indexNames) => OracleHints.TableHint(table, Table.IndexAsc, indexNames);
		}

		[ExpressionMethod(ProviderName.Oracle, nameof(IndexCombineIndexHintImpl))]
		public static IOracleSpecificTable<TSource> IndexCombineHint<TSource>(this IOracleSpecificTable<TSource> table, params string[] indexNames)
			where TSource : notnull
		{
			return OracleHints.TableHint(table, Table.IndexCombine, indexNames);
		}
		static Expression<Func<IOracleSpecificTable<TSource>,string[],IOracleSpecificTable<TSource>>> IndexCombineIndexHintImpl<TSource>()
			where TSource : notnull
		{
			return (table, indexNames) => OracleHints.TableHint(table, Table.IndexCombine, indexNames);
		}

		[ExpressionMethod(ProviderName.Oracle, nameof(IndexJoinIndexHintImpl))]
		public static IOracleSpecificTable<TSource> IndexJoinHint<TSource>(this IOracleSpecificTable<TSource> table, params string[] indexNames)
			where TSource : notnull
		{
			return OracleHints.TableHint(table, Table.IndexJoin, indexNames);
		}
		static Expression<Func<IOracleSpecificTable<TSource>,string[],IOracleSpecificTable<TSource>>> IndexJoinIndexHintImpl<TSource>()
			where TSource : notnull
		{
			return (table, indexNames) => OracleHints.TableHint(table, Table.IndexJoin, indexNames);
		}

		[ExpressionMethod(ProviderName.Oracle, nameof(IndexDescIndexHintImpl))]
		public static IOracleSpecificTable<TSource> IndexDescHint<TSource>(this IOracleSpecificTable<TSource> table, params string[] indexNames)
			where TSource : notnull
		{
			return OracleHints.TableHint(table, Table.IndexDesc, indexNames);
		}
		static Expression<Func<IOracleSpecificTable<TSource>,string[],IOracleSpecificTable<TSource>>> IndexDescIndexHintImpl<TSource>()
			where TSource : notnull
		{
			return (table, indexNames) => OracleHints.TableHint(table, Table.IndexDesc, indexNames);
		}

		[ExpressionMethod(ProviderName.Oracle, nameof(IndexFFSIndexHintImpl))]
		public static IOracleSpecificTable<TSource> IndexFFSHint<TSource>(this IOracleSpecificTable<TSource> table, params string[] indexNames)
			where TSource : notnull
		{
			return OracleHints.TableHint(table, Table.IndexFFS, indexNames);
		}
		static Expression<Func<IOracleSpecificTable<TSource>,string[],IOracleSpecificTable<TSource>>> IndexFFSIndexHintImpl<TSource>()
			where TSource : notnull
		{
			return (table, indexNames) => OracleHints.TableHint(table, Table.IndexFFS, indexNames);
		}

		[ExpressionMethod(ProviderName.Oracle, nameof(IndexFastFullScanIndexHintImpl))]
		public static IOracleSpecificTable<TSource> IndexFastFullScanHint<TSource>(this IOracleSpecificTable<TSource> table, params string[] indexNames)
			where TSource : notnull
		{
			return OracleHints.TableHint(table, Table.IndexFastFullScan, indexNames);
		}
		static Expression<Func<IOracleSpecificTable<TSource>,string[],IOracleSpecificTable<TSource>>> IndexFastFullScanIndexHintImpl<TSource>()
			where TSource : notnull
		{
			return (table, indexNames) => OracleHints.TableHint(table, Table.IndexFastFullScan, indexNames);
		}

		[ExpressionMethod(ProviderName.Oracle, nameof(IndexSSIndexHintImpl))]
		public static IOracleSpecificTable<TSource> IndexSSHint<TSource>(this IOracleSpecificTable<TSource> table, params string[] indexNames)
			where TSource : notnull
		{
			return OracleHints.TableHint(table, Table.IndexSS, indexNames);
		}
		static Expression<Func<IOracleSpecificTable<TSource>,string[],IOracleSpecificTable<TSource>>> IndexSSIndexHintImpl<TSource>()
			where TSource : notnull
		{
			return (table, indexNames) => OracleHints.TableHint(table, Table.IndexSS, indexNames);
		}

		[ExpressionMethod(ProviderName.Oracle, nameof(IndexSkipScanIndexHintImpl))]
		public static IOracleSpecificTable<TSource> IndexSkipScanHint<TSource>(this IOracleSpecificTable<TSource> table, params string[] indexNames)
			where TSource : notnull
		{
			return OracleHints.TableHint(table, Table.IndexSkipScan, indexNames);
		}
		static Expression<Func<IOracleSpecificTable<TSource>,string[],IOracleSpecificTable<TSource>>> IndexSkipScanIndexHintImpl<TSource>()
			where TSource : notnull
		{
			return (table, indexNames) => OracleHints.TableHint(table, Table.IndexSkipScan, indexNames);
		}

		[ExpressionMethod(ProviderName.Oracle, nameof(IndexSSAscIndexHintImpl))]
		public static IOracleSpecificTable<TSource> IndexSSAscHint<TSource>(this IOracleSpecificTable<TSource> table, params string[] indexNames)
			where TSource : notnull
		{
			return OracleHints.TableHint(table, Table.IndexSSAsc, indexNames);
		}
		static Expression<Func<IOracleSpecificTable<TSource>,string[],IOracleSpecificTable<TSource>>> IndexSSAscIndexHintImpl<TSource>()
			where TSource : notnull
		{
			return (table, indexNames) => OracleHints.TableHint(table, Table.IndexSSAsc, indexNames);
		}

		[ExpressionMethod(ProviderName.Oracle, nameof(IndexSkipScanAscIndexHintImpl))]
		public static IOracleSpecificTable<TSource> IndexSkipScanAscHint<TSource>(this IOracleSpecificTable<TSource> table, params string[] indexNames)
			where TSource : notnull
		{
			return OracleHints.TableHint(table, Table.IndexSkipScanAsc, indexNames);
		}
		static Expression<Func<IOracleSpecificTable<TSource>,string[],IOracleSpecificTable<TSource>>> IndexSkipScanAscIndexHintImpl<TSource>()
			where TSource : notnull
		{
			return (table, indexNames) => OracleHints.TableHint(table, Table.IndexSkipScanAsc, indexNames);
		}

		[ExpressionMethod(ProviderName.Oracle, nameof(IndexSSDescIndexHintImpl))]
		public static IOracleSpecificTable<TSource> IndexSSDescHint<TSource>(this IOracleSpecificTable<TSource> table, params string[] indexNames)
			where TSource : notnull
		{
			return OracleHints.TableHint(table, Table.IndexSSDesc, indexNames);
		}
		static Expression<Func<IOracleSpecificTable<TSource>,string[],IOracleSpecificTable<TSource>>> IndexSSDescIndexHintImpl<TSource>()
			where TSource : notnull
		{
			return (table, indexNames) => OracleHints.TableHint(table, Table.IndexSSDesc, indexNames);
		}

		[ExpressionMethod(ProviderName.Oracle, nameof(IndexSkipScanDescIndexHintImpl))]
		public static IOracleSpecificTable<TSource> IndexSkipScanDescHint<TSource>(this IOracleSpecificTable<TSource> table, params string[] indexNames)
			where TSource : notnull
		{
			return OracleHints.TableHint(table, Table.IndexSkipScanDesc, indexNames);
		}
		static Expression<Func<IOracleSpecificTable<TSource>,string[],IOracleSpecificTable<TSource>>> IndexSkipScanDescIndexHintImpl<TSource>()
			where TSource : notnull
		{
			return (table, indexNames) => OracleHints.TableHint(table, Table.IndexSkipScanDesc, indexNames);
		}

		[ExpressionMethod(ProviderName.Oracle, nameof(NoIndexIndexHintImpl))]
		public static IOracleSpecificTable<TSource> NoIndexHint<TSource>(this IOracleSpecificTable<TSource> table, params string[] indexNames)
			where TSource : notnull
		{
			return OracleHints.TableHint(table, Table.NoIndex, indexNames);
		}
		static Expression<Func<IOracleSpecificTable<TSource>,string[],IOracleSpecificTable<TSource>>> NoIndexIndexHintImpl<TSource>()
			where TSource : notnull
		{
			return (table, indexNames) => OracleHints.TableHint(table, Table.NoIndex, indexNames);
		}

		[ExpressionMethod(ProviderName.Oracle, nameof(NoIndexFFSIndexHintImpl))]
		public static IOracleSpecificTable<TSource> NoIndexFFSHint<TSource>(this IOracleSpecificTable<TSource> table, params string[] indexNames)
			where TSource : notnull
		{
			return OracleHints.TableHint(table, Table.NoIndexFFS, indexNames);
		}
		static Expression<Func<IOracleSpecificTable<TSource>,string[],IOracleSpecificTable<TSource>>> NoIndexFFSIndexHintImpl<TSource>()
			where TSource : notnull
		{
			return (table, indexNames) => OracleHints.TableHint(table, Table.NoIndexFFS, indexNames);
		}

		[ExpressionMethod(ProviderName.Oracle, nameof(NoIndexFastFullScanIndexHintImpl))]
		public static IOracleSpecificTable<TSource> NoIndexFastFullScanHint<TSource>(this IOracleSpecificTable<TSource> table, params string[] indexNames)
			where TSource : notnull
		{
			return OracleHints.TableHint(table, Table.NoIndexFastFullScan, indexNames);
		}
		static Expression<Func<IOracleSpecificTable<TSource>,string[],IOracleSpecificTable<TSource>>> NoIndexFastFullScanIndexHintImpl<TSource>()
			where TSource : notnull
		{
			return (table, indexNames) => OracleHints.TableHint(table, Table.NoIndexFastFullScan, indexNames);
		}

		[ExpressionMethod(ProviderName.Oracle, nameof(NoIndexSSIndexHintImpl))]
		public static IOracleSpecificTable<TSource> NoIndexSSHint<TSource>(this IOracleSpecificTable<TSource> table, params string[] indexNames)
			where TSource : notnull
		{
			return OracleHints.TableHint(table, Table.NoIndexSS, indexNames);
		}
		static Expression<Func<IOracleSpecificTable<TSource>,string[],IOracleSpecificTable<TSource>>> NoIndexSSIndexHintImpl<TSource>()
			where TSource : notnull
		{
			return (table, indexNames) => OracleHints.TableHint(table, Table.NoIndexSS, indexNames);
		}

		[ExpressionMethod(ProviderName.Oracle, nameof(NoIndexSkipScanIndexHintImpl))]
		public static IOracleSpecificTable<TSource> NoIndexSkipScanHint<TSource>(this IOracleSpecificTable<TSource> table, params string[] indexNames)
			where TSource : notnull
		{
			return OracleHints.TableHint(table, Table.NoIndexSkipScan, indexNames);
		}
		static Expression<Func<IOracleSpecificTable<TSource>,string[],IOracleSpecificTable<TSource>>> NoIndexSkipScanIndexHintImpl<TSource>()
			where TSource : notnull
		{
			return (table, indexNames) => OracleHints.TableHint(table, Table.NoIndexSkipScan, indexNames);
		}

		[ExpressionMethod(nameof(AllRowsHintImpl))]
		public static IOracleSpecificQueryable<TSource> AllRowsHint<TSource>(this IOracleSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return OracleHints.QueryHint(query, Query.AllRows);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,IOracleSpecificQueryable<TSource>>> AllRowsHintImpl<TSource>()
			where TSource : notnull
		{
			return query => OracleHints.QueryHint(query, Query.AllRows);
		}

		[ExpressionMethod(nameof(FirstRowsHintImpl2))]
		public static IOracleSpecificQueryable<TSource> FirstRowsHint<TSource>(this IOracleSpecificQueryable<TSource> query, int value)
			where TSource : notnull
		{
			return OracleHints.QueryHint(query, Query.FirstRows(value));
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,int,IOracleSpecificQueryable<TSource>>> FirstRowsHintImpl2<TSource>()
			where TSource : notnull
		{
			return (query, value) => OracleHints.QueryHint(query, Query.FirstRows(value));
		}

		[ExpressionMethod(nameof(NoQueryTransformationHintImpl))]
		public static IOracleSpecificQueryable<TSource> NoQueryTransformationHint<TSource>(this IOracleSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return OracleHints.QueryHint(query, Query.NoQueryTransformation);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,IOracleSpecificQueryable<TSource>>> NoQueryTransformationHintImpl<TSource>()
			where TSource : notnull
		{
			return query => OracleHints.QueryHint(query, Query.NoQueryTransformation);
		}

		[ExpressionMethod(nameof(UseConcatHintImpl))]
		public static IOracleSpecificQueryable<TSource> UseConcatHint<TSource>(this IOracleSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return OracleHints.QueryHint(query, Query.UseConcat);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,IOracleSpecificQueryable<TSource>>> UseConcatHintImpl<TSource>()
			where TSource : notnull
		{
			return query => OracleHints.QueryHint(query, Query.UseConcat);
		}

		[ExpressionMethod(nameof(UseConcatHintImpl3))]
		public static IOracleSpecificQueryable<TSource> UseConcatHint<TSource>(this IOracleSpecificQueryable<TSource> query, string queryBlock)
			where TSource : notnull
		{
			return OracleHints.QueryHint(query, Query.UseConcat, queryBlock);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,string,IOracleSpecificQueryable<TSource>>> UseConcatHintImpl3<TSource>()
			where TSource : notnull
		{
			return (query, queryBlock) => OracleHints.QueryHint(query, Query.UseConcat, queryBlock);
		}

		[ExpressionMethod(nameof(NoExpandHintImpl))]
		public static IOracleSpecificQueryable<TSource> NoExpandHint<TSource>(this IOracleSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return OracleHints.QueryHint(query, Query.NoExpand);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,IOracleSpecificQueryable<TSource>>> NoExpandHintImpl<TSource>()
			where TSource : notnull
		{
			return query => OracleHints.QueryHint(query, Query.NoExpand);
		}

		[ExpressionMethod(nameof(NoExpandHintImpl3))]
		public static IOracleSpecificQueryable<TSource> NoExpandHint<TSource>(this IOracleSpecificQueryable<TSource> query, string queryBlock)
			where TSource : notnull
		{
			return OracleHints.QueryHint(query, Query.NoExpand, queryBlock);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,string,IOracleSpecificQueryable<TSource>>> NoExpandHintImpl3<TSource>()
			where TSource : notnull
		{
			return (query, queryBlock) => OracleHints.QueryHint(query, Query.NoExpand, queryBlock);
		}

		[ExpressionMethod(nameof(RewriteHintImpl))]
		public static IOracleSpecificQueryable<TSource> RewriteHint<TSource>(this IOracleSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return OracleHints.QueryHint(query, Query.Rewrite);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,IOracleSpecificQueryable<TSource>>> RewriteHintImpl<TSource>()
			where TSource : notnull
		{
			return query => OracleHints.QueryHint(query, Query.Rewrite);
		}

		[ExpressionMethod(nameof(RewriteHintImpl3))]
		public static IOracleSpecificQueryable<TSource> RewriteHint<TSource>(this IOracleSpecificQueryable<TSource> query, string queryBlock)
			where TSource : notnull
		{
			return OracleHints.QueryHint(query, Query.Rewrite, queryBlock);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,string,IOracleSpecificQueryable<TSource>>> RewriteHintImpl3<TSource>()
			where TSource : notnull
		{
			return (query, queryBlock) => OracleHints.QueryHint(query, Query.Rewrite, queryBlock);
		}

		[ExpressionMethod(nameof(NoRewriteHintImpl))]
		public static IOracleSpecificQueryable<TSource> NoRewriteHint<TSource>(this IOracleSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return OracleHints.QueryHint(query, Query.NoRewrite);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,IOracleSpecificQueryable<TSource>>> NoRewriteHintImpl<TSource>()
			where TSource : notnull
		{
			return query => OracleHints.QueryHint(query, Query.NoRewrite);
		}

		[ExpressionMethod(nameof(NoRewriteHintImpl3))]
		public static IOracleSpecificQueryable<TSource> NoRewriteHint<TSource>(this IOracleSpecificQueryable<TSource> query, string queryBlock)
			where TSource : notnull
		{
			return OracleHints.QueryHint(query, Query.NoRewrite, queryBlock);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,string,IOracleSpecificQueryable<TSource>>> NoRewriteHintImpl3<TSource>()
			where TSource : notnull
		{
			return (query, queryBlock) => OracleHints.QueryHint(query, Query.NoRewrite, queryBlock);
		}

		[ExpressionMethod(nameof(MergeHintImpl))]
		public static IOracleSpecificQueryable<TSource> MergeHint<TSource>(this IOracleSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return OracleHints.QueryHint(query, Query.Merge);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,IOracleSpecificQueryable<TSource>>> MergeHintImpl<TSource>()
			where TSource : notnull
		{
			return query => OracleHints.QueryHint(query, Query.Merge);
		}

		[ExpressionMethod(nameof(MergeHintImpl3))]
		public static IOracleSpecificQueryable<TSource> MergeHint<TSource>(this IOracleSpecificQueryable<TSource> query, string queryBlock)
			where TSource : notnull
		{
			return OracleHints.QueryHint(query, Query.Merge, queryBlock);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,string,IOracleSpecificQueryable<TSource>>> MergeHintImpl3<TSource>()
			where TSource : notnull
		{
			return (query, queryBlock) => OracleHints.QueryHint(query, Query.Merge, queryBlock);
		}

		[ExpressionMethod(ProviderName.Oracle, nameof(MergeTableHintImpl))]
		public static IOracleSpecificTable<TSource> MergeHint<TSource>(this IOracleSpecificTable<TSource> table)
			where TSource : notnull
		{
			return OracleHints.TableHint(table, Table.Merge);
		}
		static Expression<Func<IOracleSpecificTable<TSource>,IOracleSpecificTable<TSource>>> MergeTableHintImpl<TSource>()
			where TSource : notnull
		{
			return table => OracleHints.TableHint(table, Table.Merge);
		}

		[ExpressionMethod(ProviderName.Oracle, nameof(MergeInScopeHintImpl))]
		public static IOracleSpecificQueryable<TSource> MergeInScopeHint<TSource>(this IOracleSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return OracleHints.TablesInScopeHint(query, Table.Merge);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,IOracleSpecificQueryable<TSource>>> MergeInScopeHintImpl<TSource>()
			where TSource : notnull
		{
			return query => OracleHints.TablesInScopeHint(query, Table.Merge);
		}

		[ExpressionMethod(nameof(NoMergeHintImpl))]
		public static IOracleSpecificQueryable<TSource> NoMergeHint<TSource>(this IOracleSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return OracleHints.QueryHint(query, Query.NoMerge);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,IOracleSpecificQueryable<TSource>>> NoMergeHintImpl<TSource>()
			where TSource : notnull
		{
			return query => OracleHints.QueryHint(query, Query.NoMerge);
		}

		[ExpressionMethod(nameof(NoMergeHintImpl3))]
		public static IOracleSpecificQueryable<TSource> NoMergeHint<TSource>(this IOracleSpecificQueryable<TSource> query, string queryBlock)
			where TSource : notnull
		{
			return OracleHints.QueryHint(query, Query.NoMerge, queryBlock);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,string,IOracleSpecificQueryable<TSource>>> NoMergeHintImpl3<TSource>()
			where TSource : notnull
		{
			return (query, queryBlock) => OracleHints.QueryHint(query, Query.NoMerge, queryBlock);
		}

		[ExpressionMethod(ProviderName.Oracle, nameof(NoMergeTableHintImpl))]
		public static IOracleSpecificTable<TSource> NoMergeHint<TSource>(this IOracleSpecificTable<TSource> table)
			where TSource : notnull
		{
			return OracleHints.TableHint(table, Table.NoMerge);
		}
		static Expression<Func<IOracleSpecificTable<TSource>,IOracleSpecificTable<TSource>>> NoMergeTableHintImpl<TSource>()
			where TSource : notnull
		{
			return table => OracleHints.TableHint(table, Table.NoMerge);
		}

		[ExpressionMethod(ProviderName.Oracle, nameof(NoMergeInScopeHintImpl))]
		public static IOracleSpecificQueryable<TSource> NoMergeInScopeHint<TSource>(this IOracleSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return OracleHints.TablesInScopeHint(query, Table.NoMerge);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,IOracleSpecificQueryable<TSource>>> NoMergeInScopeHintImpl<TSource>()
			where TSource : notnull
		{
			return query => OracleHints.TablesInScopeHint(query, Table.NoMerge);
		}

		[ExpressionMethod(nameof(StarTransformationHintImpl))]
		public static IOracleSpecificQueryable<TSource> StarTransformationHint<TSource>(this IOracleSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return OracleHints.QueryHint(query, Query.StarTransformation);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,IOracleSpecificQueryable<TSource>>> StarTransformationHintImpl<TSource>()
			where TSource : notnull
		{
			return query => OracleHints.QueryHint(query, Query.StarTransformation);
		}

		[ExpressionMethod(nameof(StarTransformationHintImpl3))]
		public static IOracleSpecificQueryable<TSource> StarTransformationHint<TSource>(this IOracleSpecificQueryable<TSource> query, string queryBlock)
			where TSource : notnull
		{
			return OracleHints.QueryHint(query, Query.StarTransformation, queryBlock);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,string,IOracleSpecificQueryable<TSource>>> StarTransformationHintImpl3<TSource>()
			where TSource : notnull
		{
			return (query, queryBlock) => OracleHints.QueryHint(query, Query.StarTransformation, queryBlock);
		}

		[ExpressionMethod(nameof(NoStarTransformationHintImpl))]
		public static IOracleSpecificQueryable<TSource> NoStarTransformationHint<TSource>(this IOracleSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return OracleHints.QueryHint(query, Query.NoStarTransformation);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,IOracleSpecificQueryable<TSource>>> NoStarTransformationHintImpl<TSource>()
			where TSource : notnull
		{
			return query => OracleHints.QueryHint(query, Query.NoStarTransformation);
		}

		[ExpressionMethod(nameof(NoStarTransformationHintImpl3))]
		public static IOracleSpecificQueryable<TSource> NoStarTransformationHint<TSource>(this IOracleSpecificQueryable<TSource> query, string queryBlock)
			where TSource : notnull
		{
			return OracleHints.QueryHint(query, Query.NoStarTransformation, queryBlock);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,string,IOracleSpecificQueryable<TSource>>> NoStarTransformationHintImpl3<TSource>()
			where TSource : notnull
		{
			return (query, queryBlock) => OracleHints.QueryHint(query, Query.NoStarTransformation, queryBlock);
		}

		[ExpressionMethod(ProviderName.Oracle, nameof(FactTableHintImpl))]
		public static IOracleSpecificTable<TSource> FactHint<TSource>(this IOracleSpecificTable<TSource> table)
			where TSource : notnull
		{
			return OracleHints.TableHint(table, Table.Fact);
		}
		static Expression<Func<IOracleSpecificTable<TSource>,IOracleSpecificTable<TSource>>> FactTableHintImpl<TSource>()
			where TSource : notnull
		{
			return table => OracleHints.TableHint(table, Table.Fact);
		}

		[ExpressionMethod(ProviderName.Oracle, nameof(FactInScopeHintImpl))]
		public static IOracleSpecificQueryable<TSource> FactInScopeHint<TSource>(this IOracleSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return OracleHints.TablesInScopeHint(query, Table.Fact);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,IOracleSpecificQueryable<TSource>>> FactInScopeHintImpl<TSource>()
			where TSource : notnull
		{
			return query => OracleHints.TablesInScopeHint(query, Table.Fact);
		}

		[ExpressionMethod(ProviderName.Oracle, nameof(NoFactTableHintImpl))]
		public static IOracleSpecificTable<TSource> NoFactHint<TSource>(this IOracleSpecificTable<TSource> table)
			where TSource : notnull
		{
			return OracleHints.TableHint(table, Table.NoFact);
		}
		static Expression<Func<IOracleSpecificTable<TSource>,IOracleSpecificTable<TSource>>> NoFactTableHintImpl<TSource>()
			where TSource : notnull
		{
			return table => OracleHints.TableHint(table, Table.NoFact);
		}

		[ExpressionMethod(ProviderName.Oracle, nameof(NoFactInScopeHintImpl))]
		public static IOracleSpecificQueryable<TSource> NoFactInScopeHint<TSource>(this IOracleSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return OracleHints.TablesInScopeHint(query, Table.NoFact);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,IOracleSpecificQueryable<TSource>>> NoFactInScopeHintImpl<TSource>()
			where TSource : notnull
		{
			return query => OracleHints.TablesInScopeHint(query, Table.NoFact);
		}

		[ExpressionMethod(nameof(UnnestHintImpl))]
		public static IOracleSpecificQueryable<TSource> UnnestHint<TSource>(this IOracleSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return OracleHints.QueryHint(query, Query.Unnest);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,IOracleSpecificQueryable<TSource>>> UnnestHintImpl<TSource>()
			where TSource : notnull
		{
			return query => OracleHints.QueryHint(query, Query.Unnest);
		}

		[ExpressionMethod(nameof(UnnestHintImpl3))]
		public static IOracleSpecificQueryable<TSource> UnnestHint<TSource>(this IOracleSpecificQueryable<TSource> query, string queryBlock)
			where TSource : notnull
		{
			return OracleHints.QueryHint(query, Query.Unnest, queryBlock);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,string,IOracleSpecificQueryable<TSource>>> UnnestHintImpl3<TSource>()
			where TSource : notnull
		{
			return (query, queryBlock) => OracleHints.QueryHint(query, Query.Unnest, queryBlock);
		}

		[ExpressionMethod(nameof(NoUnnestHintImpl))]
		public static IOracleSpecificQueryable<TSource> NoUnnestHint<TSource>(this IOracleSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return OracleHints.QueryHint(query, Query.NoUnnest);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,IOracleSpecificQueryable<TSource>>> NoUnnestHintImpl<TSource>()
			where TSource : notnull
		{
			return query => OracleHints.QueryHint(query, Query.NoUnnest);
		}

		[ExpressionMethod(nameof(NoUnnestHintImpl3))]
		public static IOracleSpecificQueryable<TSource> NoUnnestHint<TSource>(this IOracleSpecificQueryable<TSource> query, string queryBlock)
			where TSource : notnull
		{
			return OracleHints.QueryHint(query, Query.NoUnnest, queryBlock);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,string,IOracleSpecificQueryable<TSource>>> NoUnnestHintImpl3<TSource>()
			where TSource : notnull
		{
			return (query, queryBlock) => OracleHints.QueryHint(query, Query.NoUnnest, queryBlock);
		}

		[ExpressionMethod(nameof(LeadingHintImpl4))]
		public static IOracleSpecificQueryable<TSource> LeadingHint<TSource>(this IOracleSpecificQueryable<TSource> query, params Sql.SqlID[] tableIDs)
			where TSource : notnull
		{
			return OracleHints.QueryHint(query, Query.Leading, tableIDs);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,Sql.SqlID[],IOracleSpecificQueryable<TSource>>> LeadingHintImpl4<TSource>()
			where TSource : notnull
		{
			return (query, tableIDs) => OracleHints.QueryHint(query, Query.Leading, tableIDs);
		}

		[ExpressionMethod(nameof(OrderedHintImpl))]
		public static IOracleSpecificQueryable<TSource> OrderedHint<TSource>(this IOracleSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return OracleHints.QueryHint(query, Query.Ordered);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,IOracleSpecificQueryable<TSource>>> OrderedHintImpl<TSource>()
			where TSource : notnull
		{
			return query => OracleHints.QueryHint(query, Query.Ordered);
		}

		[ExpressionMethod(nameof(UseNLHintImpl4))]
		public static IOracleSpecificQueryable<TSource> UseNLHint<TSource>(this IOracleSpecificQueryable<TSource> query, params Sql.SqlID[] tableIDs)
			where TSource : notnull
		{
			return OracleHints.QueryHint(query, Query.UseNL, tableIDs);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,Sql.SqlID[],IOracleSpecificQueryable<TSource>>> UseNLHintImpl4<TSource>()
			where TSource : notnull
		{
			return (query, tableIDs) => OracleHints.QueryHint(query, Query.UseNL, tableIDs);
		}

		[ExpressionMethod(nameof(UseNestedLoopHintImpl4))]
		public static IOracleSpecificQueryable<TSource> UseNestedLoopHint<TSource>(this IOracleSpecificQueryable<TSource> query, params Sql.SqlID[] tableIDs)
			where TSource : notnull
		{
			return OracleHints.QueryHint(query, Query.UseNestedLoop, tableIDs);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,Sql.SqlID[],IOracleSpecificQueryable<TSource>>> UseNestedLoopHintImpl4<TSource>()
			where TSource : notnull
		{
			return (query, tableIDs) => OracleHints.QueryHint(query, Query.UseNestedLoop, tableIDs);
		}

		[ExpressionMethod(nameof(NoUseNLHintImpl4))]
		public static IOracleSpecificQueryable<TSource> NoUseNLHint<TSource>(this IOracleSpecificQueryable<TSource> query, params Sql.SqlID[] tableIDs)
			where TSource : notnull
		{
			return OracleHints.QueryHint(query, Query.NoUseNL, tableIDs);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,Sql.SqlID[],IOracleSpecificQueryable<TSource>>> NoUseNLHintImpl4<TSource>()
			where TSource : notnull
		{
			return (query, tableIDs) => OracleHints.QueryHint(query, Query.NoUseNL, tableIDs);
		}

		[ExpressionMethod(nameof(NoUseNestedLoopHintImpl4))]
		public static IOracleSpecificQueryable<TSource> NoUseNestedLoopHint<TSource>(this IOracleSpecificQueryable<TSource> query, params Sql.SqlID[] tableIDs)
			where TSource : notnull
		{
			return OracleHints.QueryHint(query, Query.NoUseNestedLoop, tableIDs);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,Sql.SqlID[],IOracleSpecificQueryable<TSource>>> NoUseNestedLoopHintImpl4<TSource>()
			where TSource : notnull
		{
			return (query, tableIDs) => OracleHints.QueryHint(query, Query.NoUseNestedLoop, tableIDs);
		}

		[ExpressionMethod(nameof(UseMergeHintImpl4))]
		public static IOracleSpecificQueryable<TSource> UseMergeHint<TSource>(this IOracleSpecificQueryable<TSource> query, params Sql.SqlID[] tableIDs)
			where TSource : notnull
		{
			return OracleHints.QueryHint(query, Query.UseMerge, tableIDs);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,Sql.SqlID[],IOracleSpecificQueryable<TSource>>> UseMergeHintImpl4<TSource>()
			where TSource : notnull
		{
			return (query, tableIDs) => OracleHints.QueryHint(query, Query.UseMerge, tableIDs);
		}

		[ExpressionMethod(nameof(NoUseMergeHintImpl4))]
		public static IOracleSpecificQueryable<TSource> NoUseMergeHint<TSource>(this IOracleSpecificQueryable<TSource> query, params Sql.SqlID[] tableIDs)
			where TSource : notnull
		{
			return OracleHints.QueryHint(query, Query.NoUseMerge, tableIDs);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,Sql.SqlID[],IOracleSpecificQueryable<TSource>>> NoUseMergeHintImpl4<TSource>()
			where TSource : notnull
		{
			return (query, tableIDs) => OracleHints.QueryHint(query, Query.NoUseMerge, tableIDs);
		}

		[ExpressionMethod(nameof(UseHashHintImpl4))]
		public static IOracleSpecificQueryable<TSource> UseHashHint<TSource>(this IOracleSpecificQueryable<TSource> query, params Sql.SqlID[] tableIDs)
			where TSource : notnull
		{
			return OracleHints.QueryHint(query, Query.UseHash, tableIDs);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,Sql.SqlID[],IOracleSpecificQueryable<TSource>>> UseHashHintImpl4<TSource>()
			where TSource : notnull
		{
			return (query, tableIDs) => OracleHints.QueryHint(query, Query.UseHash, tableIDs);
		}

		[ExpressionMethod(nameof(NoUseHashHintImpl4))]
		public static IOracleSpecificQueryable<TSource> NoUseHashHint<TSource>(this IOracleSpecificQueryable<TSource> query, params Sql.SqlID[] tableIDs)
			where TSource : notnull
		{
			return OracleHints.QueryHint(query, Query.NoUseHash, tableIDs);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,Sql.SqlID[],IOracleSpecificQueryable<TSource>>> NoUseHashHintImpl4<TSource>()
			where TSource : notnull
		{
			return (query, tableIDs) => OracleHints.QueryHint(query, Query.NoUseHash, tableIDs);
		}

		[ExpressionMethod(ProviderName.Oracle, nameof(UseNestedLoopWithIndexIndexHintImpl))]
		public static IOracleSpecificTable<TSource> UseNestedLoopWithIndexHint<TSource>(this IOracleSpecificTable<TSource> table, params string[] indexNames)
			where TSource : notnull
		{
			return OracleHints.TableHint(table, Table.UseNestedLoopWithIndex, indexNames);
		}
		static Expression<Func<IOracleSpecificTable<TSource>,string[],IOracleSpecificTable<TSource>>> UseNestedLoopWithIndexIndexHintImpl<TSource>()
			where TSource : notnull
		{
			return (table, indexNames) => OracleHints.TableHint(table, Table.UseNestedLoopWithIndex, indexNames);
		}

		[ExpressionMethod(nameof(ParallelHintImpl))]
		public static IOracleSpecificQueryable<TSource> ParallelHint<TSource>(this IOracleSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return OracleHints.QueryHint(query, Query.Parallel);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,IOracleSpecificQueryable<TSource>>> ParallelHintImpl<TSource>()
			where TSource : notnull
		{
			return query => OracleHints.QueryHint(query, Query.Parallel);
		}

		[ExpressionMethod(ProviderName.Oracle, nameof(NoParallelTableHintImpl))]
		public static IOracleSpecificTable<TSource> NoParallelHint<TSource>(this IOracleSpecificTable<TSource> table)
			where TSource : notnull
		{
			return OracleHints.TableHint(table, Table.NoParallel);
		}
		static Expression<Func<IOracleSpecificTable<TSource>,IOracleSpecificTable<TSource>>> NoParallelTableHintImpl<TSource>()
			where TSource : notnull
		{
			return table => OracleHints.TableHint(table, Table.NoParallel);
		}

		[ExpressionMethod(ProviderName.Oracle, nameof(NoParallelInScopeHintImpl))]
		public static IOracleSpecificQueryable<TSource> NoParallelInScopeHint<TSource>(this IOracleSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return OracleHints.TablesInScopeHint(query, Table.NoParallel);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,IOracleSpecificQueryable<TSource>>> NoParallelInScopeHintImpl<TSource>()
			where TSource : notnull
		{
			return query => OracleHints.TablesInScopeHint(query, Table.NoParallel);
		}

		[ExpressionMethod(nameof(AppendHintImpl))]
		public static IOracleSpecificQueryable<TSource> AppendHint<TSource>(this IOracleSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return OracleHints.QueryHint(query, Query.Append);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,IOracleSpecificQueryable<TSource>>> AppendHintImpl<TSource>()
			where TSource : notnull
		{
			return query => OracleHints.QueryHint(query, Query.Append);
		}

		[ExpressionMethod(nameof(NoAppendHintImpl))]
		public static IOracleSpecificQueryable<TSource> NoAppendHint<TSource>(this IOracleSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return OracleHints.QueryHint(query, Query.NoAppend);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,IOracleSpecificQueryable<TSource>>> NoAppendHintImpl<TSource>()
			where TSource : notnull
		{
			return query => OracleHints.QueryHint(query, Query.NoAppend);
		}

		[ExpressionMethod(ProviderName.Oracle, nameof(CacheTableHintImpl))]
		public static IOracleSpecificTable<TSource> CacheHint<TSource>(this IOracleSpecificTable<TSource> table)
			where TSource : notnull
		{
			return OracleHints.TableHint(table, Table.Cache);
		}
		static Expression<Func<IOracleSpecificTable<TSource>,IOracleSpecificTable<TSource>>> CacheTableHintImpl<TSource>()
			where TSource : notnull
		{
			return table => OracleHints.TableHint(table, Table.Cache);
		}

		[ExpressionMethod(ProviderName.Oracle, nameof(CacheInScopeHintImpl))]
		public static IOracleSpecificQueryable<TSource> CacheInScopeHint<TSource>(this IOracleSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return OracleHints.TablesInScopeHint(query, Table.Cache);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,IOracleSpecificQueryable<TSource>>> CacheInScopeHintImpl<TSource>()
			where TSource : notnull
		{
			return query => OracleHints.TablesInScopeHint(query, Table.Cache);
		}

		[ExpressionMethod(ProviderName.Oracle, nameof(NoCacheTableHintImpl))]
		public static IOracleSpecificTable<TSource> NoCacheHint<TSource>(this IOracleSpecificTable<TSource> table)
			where TSource : notnull
		{
			return OracleHints.TableHint(table, Table.NoCache);
		}
		static Expression<Func<IOracleSpecificTable<TSource>,IOracleSpecificTable<TSource>>> NoCacheTableHintImpl<TSource>()
			where TSource : notnull
		{
			return table => OracleHints.TableHint(table, Table.NoCache);
		}

		[ExpressionMethod(ProviderName.Oracle, nameof(NoCacheInScopeHintImpl))]
		public static IOracleSpecificQueryable<TSource> NoCacheInScopeHint<TSource>(this IOracleSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return OracleHints.TablesInScopeHint(query, Table.NoCache);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,IOracleSpecificQueryable<TSource>>> NoCacheInScopeHintImpl<TSource>()
			where TSource : notnull
		{
			return query => OracleHints.TablesInScopeHint(query, Table.NoCache);
		}

		[ExpressionMethod(nameof(PushPredicateHintImpl))]
		public static IOracleSpecificQueryable<TSource> PushPredicateHint<TSource>(this IOracleSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return OracleHints.QueryHint(query, Query.PushPredicate);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,IOracleSpecificQueryable<TSource>>> PushPredicateHintImpl<TSource>()
			where TSource : notnull
		{
			return query => OracleHints.QueryHint(query, Query.PushPredicate);
		}

		[ExpressionMethod(nameof(PushPredicateHintImpl3))]
		public static IOracleSpecificQueryable<TSource> PushPredicateHint<TSource>(this IOracleSpecificQueryable<TSource> query, string queryBlock)
			where TSource : notnull
		{
			return OracleHints.QueryHint(query, Query.PushPredicate, queryBlock);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,string,IOracleSpecificQueryable<TSource>>> PushPredicateHintImpl3<TSource>()
			where TSource : notnull
		{
			return (query, queryBlock) => OracleHints.QueryHint(query, Query.PushPredicate, queryBlock);
		}

		[ExpressionMethod(ProviderName.Oracle, nameof(PushPredicateTableHintImpl))]
		public static IOracleSpecificTable<TSource> PushPredicateHint<TSource>(this IOracleSpecificTable<TSource> table)
			where TSource : notnull
		{
			return OracleHints.TableHint(table, Table.PushPredicate);
		}
		static Expression<Func<IOracleSpecificTable<TSource>,IOracleSpecificTable<TSource>>> PushPredicateTableHintImpl<TSource>()
			where TSource : notnull
		{
			return table => OracleHints.TableHint(table, Table.PushPredicate);
		}

		[ExpressionMethod(ProviderName.Oracle, nameof(PushPredicateInScopeHintImpl))]
		public static IOracleSpecificQueryable<TSource> PushPredicateInScopeHint<TSource>(this IOracleSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return OracleHints.TablesInScopeHint(query, Table.PushPredicate);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,IOracleSpecificQueryable<TSource>>> PushPredicateInScopeHintImpl<TSource>()
			where TSource : notnull
		{
			return query => OracleHints.TablesInScopeHint(query, Table.PushPredicate);
		}

		[ExpressionMethod(nameof(NoPushPredicateHintImpl))]
		public static IOracleSpecificQueryable<TSource> NoPushPredicateHint<TSource>(this IOracleSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return OracleHints.QueryHint(query, Query.NoPushPredicate);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,IOracleSpecificQueryable<TSource>>> NoPushPredicateHintImpl<TSource>()
			where TSource : notnull
		{
			return query => OracleHints.QueryHint(query, Query.NoPushPredicate);
		}

		[ExpressionMethod(nameof(NoPushPredicateHintImpl3))]
		public static IOracleSpecificQueryable<TSource> NoPushPredicateHint<TSource>(this IOracleSpecificQueryable<TSource> query, string queryBlock)
			where TSource : notnull
		{
			return OracleHints.QueryHint(query, Query.NoPushPredicate, queryBlock);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,string,IOracleSpecificQueryable<TSource>>> NoPushPredicateHintImpl3<TSource>()
			where TSource : notnull
		{
			return (query, queryBlock) => OracleHints.QueryHint(query, Query.NoPushPredicate, queryBlock);
		}

		[ExpressionMethod(ProviderName.Oracle, nameof(NoPushPredicateTableHintImpl))]
		public static IOracleSpecificTable<TSource> NoPushPredicateHint<TSource>(this IOracleSpecificTable<TSource> table)
			where TSource : notnull
		{
			return OracleHints.TableHint(table, Table.NoPushPredicate);
		}
		static Expression<Func<IOracleSpecificTable<TSource>,IOracleSpecificTable<TSource>>> NoPushPredicateTableHintImpl<TSource>()
			where TSource : notnull
		{
			return table => OracleHints.TableHint(table, Table.NoPushPredicate);
		}

		[ExpressionMethod(ProviderName.Oracle, nameof(NoPushPredicateInScopeHintImpl))]
		public static IOracleSpecificQueryable<TSource> NoPushPredicateInScopeHint<TSource>(this IOracleSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return OracleHints.TablesInScopeHint(query, Table.NoPushPredicate);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,IOracleSpecificQueryable<TSource>>> NoPushPredicateInScopeHintImpl<TSource>()
			where TSource : notnull
		{
			return query => OracleHints.TablesInScopeHint(query, Table.NoPushPredicate);
		}

		[ExpressionMethod(nameof(PushSubQueriesHintImpl3))]
		public static IOracleSpecificQueryable<TSource> PushSubQueriesHint<TSource>(this IOracleSpecificQueryable<TSource> query, string queryBlock)
			where TSource : notnull
		{
			return OracleHints.QueryHint(query, Query.PushSubQueries, queryBlock);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,string,IOracleSpecificQueryable<TSource>>> PushSubQueriesHintImpl3<TSource>()
			where TSource : notnull
		{
			return (query, queryBlock) => OracleHints.QueryHint(query, Query.PushSubQueries, queryBlock);
		}

		[ExpressionMethod(nameof(NoPushSubQueriesHintImpl3))]
		public static IOracleSpecificQueryable<TSource> NoPushSubQueriesHint<TSource>(this IOracleSpecificQueryable<TSource> query, string queryBlock)
			where TSource : notnull
		{
			return OracleHints.QueryHint(query, Query.NoPushSubQueries, queryBlock);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,string,IOracleSpecificQueryable<TSource>>> NoPushSubQueriesHintImpl3<TSource>()
			where TSource : notnull
		{
			return (query, queryBlock) => OracleHints.QueryHint(query, Query.NoPushSubQueries, queryBlock);
		}

		[ExpressionMethod(nameof(CursorSharingExactHintImpl))]
		public static IOracleSpecificQueryable<TSource> CursorSharingExactHint<TSource>(this IOracleSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return OracleHints.QueryHint(query, Query.CursorSharingExact);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,IOracleSpecificQueryable<TSource>>> CursorSharingExactHintImpl<TSource>()
			where TSource : notnull
		{
			return query => OracleHints.QueryHint(query, Query.CursorSharingExact);
		}

		[ExpressionMethod(ProviderName.Oracle, nameof(DrivingSiteTableHintImpl))]
		public static IOracleSpecificTable<TSource> DrivingSiteHint<TSource>(this IOracleSpecificTable<TSource> table)
			where TSource : notnull
		{
			return OracleHints.TableHint(table, Table.DrivingSite);
		}
		static Expression<Func<IOracleSpecificTable<TSource>,IOracleSpecificTable<TSource>>> DrivingSiteTableHintImpl<TSource>()
			where TSource : notnull
		{
			return table => OracleHints.TableHint(table, Table.DrivingSite);
		}

		[ExpressionMethod(ProviderName.Oracle, nameof(DrivingSiteInScopeHintImpl))]
		public static IOracleSpecificQueryable<TSource> DrivingSiteInScopeHint<TSource>(this IOracleSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return OracleHints.TablesInScopeHint(query, Table.DrivingSite);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,IOracleSpecificQueryable<TSource>>> DrivingSiteInScopeHintImpl<TSource>()
			where TSource : notnull
		{
			return query => OracleHints.TablesInScopeHint(query, Table.DrivingSite);
		}

		[ExpressionMethod(nameof(ModelMinAnalysisHintImpl))]
		public static IOracleSpecificQueryable<TSource> ModelMinAnalysisHint<TSource>(this IOracleSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return OracleHints.QueryHint(query, Query.ModelMinAnalysis);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,IOracleSpecificQueryable<TSource>>> ModelMinAnalysisHintImpl<TSource>()
			where TSource : notnull
		{
			return query => OracleHints.QueryHint(query, Query.ModelMinAnalysis);
		}

		[ExpressionMethod(ProviderName.Oracle, nameof(PxJoinFilterTableHintImpl))]
		public static IOracleSpecificTable<TSource> PxJoinFilterHint<TSource>(this IOracleSpecificTable<TSource> table)
			where TSource : notnull
		{
			return OracleHints.TableHint(table, Table.PxJoinFilter);
		}
		static Expression<Func<IOracleSpecificTable<TSource>,IOracleSpecificTable<TSource>>> PxJoinFilterTableHintImpl<TSource>()
			where TSource : notnull
		{
			return table => OracleHints.TableHint(table, Table.PxJoinFilter);
		}

		[ExpressionMethod(ProviderName.Oracle, nameof(PxJoinFilterInScopeHintImpl))]
		public static IOracleSpecificQueryable<TSource> PxJoinFilterInScopeHint<TSource>(this IOracleSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return OracleHints.TablesInScopeHint(query, Table.PxJoinFilter);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,IOracleSpecificQueryable<TSource>>> PxJoinFilterInScopeHintImpl<TSource>()
			where TSource : notnull
		{
			return query => OracleHints.TablesInScopeHint(query, Table.PxJoinFilter);
		}

		[ExpressionMethod(ProviderName.Oracle, nameof(NoPxJoinFilterTableHintImpl))]
		public static IOracleSpecificTable<TSource> NoPxJoinFilterHint<TSource>(this IOracleSpecificTable<TSource> table)
			where TSource : notnull
		{
			return OracleHints.TableHint(table, Table.NoPxJoinFilter);
		}
		static Expression<Func<IOracleSpecificTable<TSource>,IOracleSpecificTable<TSource>>> NoPxJoinFilterTableHintImpl<TSource>()
			where TSource : notnull
		{
			return table => OracleHints.TableHint(table, Table.NoPxJoinFilter);
		}

		[ExpressionMethod(ProviderName.Oracle, nameof(NoPxJoinFilterInScopeHintImpl))]
		public static IOracleSpecificQueryable<TSource> NoPxJoinFilterInScopeHint<TSource>(this IOracleSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return OracleHints.TablesInScopeHint(query, Table.NoPxJoinFilter);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,IOracleSpecificQueryable<TSource>>> NoPxJoinFilterInScopeHintImpl<TSource>()
			where TSource : notnull
		{
			return query => OracleHints.TablesInScopeHint(query, Table.NoPxJoinFilter);
		}

		[ExpressionMethod(nameof(NoXmlQueryRewriteHintImpl))]
		public static IOracleSpecificQueryable<TSource> NoXmlQueryRewriteHint<TSource>(this IOracleSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return OracleHints.QueryHint(query, Query.NoXmlQueryRewrite);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,IOracleSpecificQueryable<TSource>>> NoXmlQueryRewriteHintImpl<TSource>()
			where TSource : notnull
		{
			return query => OracleHints.QueryHint(query, Query.NoXmlQueryRewrite);
		}

		[ExpressionMethod(nameof(NoXmlIndexRewriteHintImpl))]
		public static IOracleSpecificQueryable<TSource> NoXmlIndexRewriteHint<TSource>(this IOracleSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return OracleHints.QueryHint(query, Query.NoXmlIndexRewrite);
		}
		static Expression<Func<IOracleSpecificQueryable<TSource>,IOracleSpecificQueryable<TSource>>> NoXmlIndexRewriteHintImpl<TSource>()
			where TSource : notnull
		{
			return query => OracleHints.QueryHint(query, Query.NoXmlIndexRewrite);
		}

	}
}
