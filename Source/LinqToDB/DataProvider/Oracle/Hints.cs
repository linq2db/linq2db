using System;

namespace LinqToDB.DataProvider.Oracle
{
	public static class Hints
	{
		// https://docs.oracle.com/cd/B19306_01/server.102/b14200/sql_elements006.htm#SQLRF50901
		//
		// Not implemented:
		//
		// LEADING
		// MERGE
		// NO_MERGE
		// NO_PUSH_PRED
		// NO_USE_MERGE
		// NO_USE_NL
		// PQ_DISTRIBUTE
		// PUSH_PRED
		// QB_NAME
		// REWRITE
		// USE_HASH
		// USE_MERGE
		// USE_NL
		//
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
			public const string PxJoinFilter    = "PX_JOIN_FILTER";
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
			public const string ParallelIndex       = "PARALLEL_INDEX";
			public const string UseNlWithIndex      = "USE_NL_WITH_INDEX";
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
			public const string PushSubQueries        = "PUSH_SUBQ";
			public const string StarTransformation    = "STAR_TRANSFORMATION";
			public const string Rule                  = "RULE";
			public const string Unnest                = "UNNEST";
			public const string UseConcat             = "USE_CONCAT";

			[Sql.Expression("FIRST_ROWS(0)")]
			public static string FirstRows(int value)
			{
				return $"FIRST_ROWS({value})";
			}
		}
	}
}
