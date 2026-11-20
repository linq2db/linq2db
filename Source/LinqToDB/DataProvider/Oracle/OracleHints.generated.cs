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
