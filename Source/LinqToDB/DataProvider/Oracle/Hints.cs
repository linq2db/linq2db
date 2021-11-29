using System;

namespace LinqToDB.DataProvider.Oracle
{
	public static class Hints
	{
		public static class TableHint
		{
			public const string Cache       = "CACHE";
			public const string Cluster     = "CLUSTER";
			public const string DrivingSite = "DRIVING_SITE";
			public const string Full        = "FULL";
//
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

		public static class JoinHint
		{
//			public const string Loop   = "LOOP";
//			public const string Hash   = "HASH";
//			public const string Merge  = "MERGE";
//			public const string Remote = "REMOTE";
		}

		public static class QueryHint
		{
			public const string AllRows            = "ALL_ROWS";
			public const string Append             = "APPEND";
			public const string CursorSharingExact = "CURSOR_SHARING_EXACT";

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
