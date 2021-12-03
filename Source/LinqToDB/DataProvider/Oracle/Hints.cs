using System;

namespace LinqToDB.DataProvider.Oracle
{
	public static class Hints
	{
		public static class TableHint
		{
			public const string Cache           = "CACHE";
			public const string Cluster         = "CLUSTER";
			public const string DrivingSite     = "DRIVING_SITE";
			public const string DynamicSampling = "DYNAMIC_SAMPLING"; // 0..10
			public const string Fact            = "FACT";
			public const string Full            = "FULL";
			public const string Hash            = "HASH";
			public const string NoCache         = "NOCACHE";
			public const string NoFact          = "NO_FACT";
			public const string NoParallel      = "NO_PARALLEL";
			public const string NoPxJoinFilter  = "NO_PX_JOIN_FILTER";
			public const string NoUseHash       = "NO_USE_HASH";
			public const string Parallel        = "PARALLEL";

//			[Sql.Expression("SPATIAL_WINDOW_MAX_CELLS={0}")]
//			public static string SpatialWindowMaxCells(int value)
//			{
//				return "SPATIAL_WINDOW_MAX_CELLS=" + value;
//			}
//
//			[Sql.Function]
//			public static string Index(string value)
//			{
//				return "Index(" + value + ")";
//			}
		}

		public static class IndexHint
		{
			public const string Index               = "INDEX";
			public const string IndexAsc            = "INDEX_ASC";
			public const string IndexCombine        = "INDEX_COMBINE";
			public const string IndexDesc           = "INDEX_DESC";
			public const string IndexFFS            = "INDEX_FFS";
			public const string IndexFastFullScan   = "INDEX_FFS";
			public const string IndexJoin           = "INDEX_JOIN";
			public const string IndexSS             = "INDEX_SS";
			public const string IndexSkipScan       = "INDEX_SS";
			public const string IndexSSAsc          = "INDEX_SS_ASC";
			public const string IndexSkipScanAsc    = "INDEX_SS_ASC";
			public const string IndexSSDesc         = "INDEX_SS_DESC";
			public const string IndexSkipScanDesc   = "INDEX_SS_DESC";
			public const string NoIndex             = "NO_INDEX";
			public const string NoIndexFFS          = "NO_INDEX_FFS";
			public const string NoIndexFastFullScan = "NO_INDEX_FFS";
			public const string NoIndexSS           = "NO_INDEX_SS";
			public const string NoIndexSkipScan     = "NO_INDEX_SS";
			public const string NoParallelIndex     = "NO_PARALLEL_INDEX";
		}

		public static class QueryHint
		{
			public const string AllRows               = "ALL_ROWS";
			public const string Append                = "APPEND";
			public const string CursorSharingExact    = "CURSOR_SHARING_EXACT";
			public const string ModelMinAnalysis      = "MODEL_MIN_ANALYSIS ";
			public const string NoAppend              = "NOAPPEND";
			public const string NoExpand              = "NO_EXPAND";
			public const string NoPushSubQuery        = "NO_PUSH_SUBQ";
			public const string NoRewrite             = "NO_REWRITE";
			public const string NoQueryTransformation = "NO_QUERY_TRANSFORMATION";
			public const string NoStarTransformation  = "NO_STAR_TRANSFORMATION";
			public const string NoUnnest              = "NO_UNNEST";
			public const string NoXmlQueryRewrite     = "NO_XML_QUERY_REWRITE";
			public const string Ordered               = "ORDERED";

			// LEADING
			// MERGE
			// NO_MERGE
			// NO_PUSH_PRED
			// NO_USE_MERGE
			// NO_USE_NL

			[Sql.Expression("FIRST_ROWS(0)")]
			public static string FirstRows(int value)
			{
				return $"FIRST_ROWS({value})";
			}
//
//			[Sql.Expression("MAX_GRANT_PERCENT={0}")]
//			public static string MaxGrantPercent(decimal value)
//			{
//				return $"MAX_GRANT_PERCENT={value}";
//			}
//
//			[Sql.Expression("MIN_GRANT_PERCENT={0}")]
//			public static string MinGrantPercent(decimal value)
//			{
//				return $"MIN_GRANT_PERCENT={value}";
//			}
//
//			[Sql.Expression("MAXDOP {0}")]
//			public static string MaxDop(int value)
//			{
//				return $"MAXDOP {value}";
//			}
//
//			[Sql.Expression("MAXRECURSION {0}")]
//			public static string MaxRecursion(int value)
//			{
//				return $"MAXRECURSION {value}";
//			}
//
//			[Sql.Expression("OPTIMIZE FOR ({0})")]
//			public static string OptimizeFor(string value)
//			{
//				return $"OPTIMIZE FOR ({value})";
//			}
//
//			[Sql.Expression("QUERYTRACEON {0}")]
//			public static string QueryTraceOn(int value)
//			{
//				return $"QUERYTRACEON {value}";
//			}
//
//			[Sql.Expression("USE HINT ({0})")]
//			public static string UseHint(string value)
//			{
//				return $"USE HINT ({value})";
//			}
//
//			[Sql.Expression("USE PLAN ({0})")]
//			public static string UsePlan(string value)
//			{
//				return $"USE PLAN ({value})";
//			}
		}
	}
}
