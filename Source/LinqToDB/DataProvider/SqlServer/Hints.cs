using System;

namespace LinqToDB.DataProvider.SqlServer
{
	public static class Hints
	{
		public static class TableHint
		{
			public const string ForceScan         = "ForceScan";
			public const string ForceSeek         = "ForceSeek";
			public const string HoldLock          = "HoldLock";
			public const string NoLock            = "NoLock";
			public const string NoWait            = "NoWait";
			public const string PagLock           = "PagLock";
			public const string ReadCommitted     = "ReadCommitted";
			public const string ReadCommittedLock = "ReadCommittedLock";
			public const string ReadPast          = "ReadPast";
			public const string ReadUncommitted   = "ReadUncommitted";
			public const string RepeatableRead    = "RepeatableRead";
			public const string RowLock           = "RowLock";
			public const string Serializable      = "Serializable";
			public const string Snapshot          = "Snapshot";
			public const string TabLock           = "TabLock";
			public const string TabLockX          = "TabLockX";
			public const string UpdLock           = "UpdLock";
			public const string XLock             = "XLock";

			[Sql.Expression("SPATIAL_WINDOW_MAX_CELLS={0}")]
			public static string SpatialWindowMaxCells(int value)
			{
				return "SPATIAL_WINDOW_MAX_CELLS=" + value;
			}

			[Sql.Function]
			public static string Index(string value)
			{
				return "Index(" + value + ")";
			}
		}

		public static class JoinHint
		{
			public const string Loop   = "LOOP";
			public const string Hash   = "HASH";
			public const string Merge  = "MERGE";
			public const string Remote = "REMOTE";
		}

		public static class Option
		{
			public const string HashGroup                          = "HASH GROUP";
			public const string OrderGroup                         = "ORDER GROUP";
			public const string ConcatUnion                        = "CONCAT UNION";
			public const string HashUnion                          = "HASH UNION";
			public const string MergeUnion                         = "MERGE UNION";
			public const string LoopJoin                           = "LOOP JOIN";
			public const string HashJoin                           = "HASH JOIN";
			public const string MergeJoin                          = "MERGE JOIN";
			public const string ExpandViews                        = "EXPAND VIEWS";
			public const string ForceOrder                         = "FORCE ORDER";
			public const string ForceExternalPushDown              = "FORCE EXTERNALPUSHDOWN";
			public const string DisableExternalPushDown            = "DISABLE EXTERNALPUSHDOWN";
			public const string ForceScaleOutExecution             = "FORCE SCALEOUTEXECUTION";
			public const string DisableScaleOutExecution           = "DISABLE SCALEOUTEXECUTION";
			public const string IgnoreNonClusteredColumnStoreIndex = "IGNORE_NONCLUSTERED_COLUMNSTORE_INDEX";
			public const string KeepPlan                           = "KEEP PLAN   ";
			public const string KeepFixedPlan                      = "KEEPFIXED PLAN";
			public const string NoPerformanceSpool                 = "NO_PERFORMANCE_SPOOL";

			[Sql.Expression("FAST {0}")]
			public static string Fast(int value)
			{
				return $"FAST {value}";
			}

			[Sql.Expression("MAX_GRANT_PERCENT={0}")]
			public static string MaxGrantPercent(decimal value)
			{
				return $"MAX_GRANT_PERCENT={value}";
			}

			[Sql.Expression("MIN_GRANT_PERCENT={0}")]
			public static string MinGrantPercent(decimal value)
			{
				return $"MIN_GRANT_PERCENT={value}";
			}

			[Sql.Expression("MAXDOP {0}")]
			public static string MaxDop(int value)
			{
				return $"MAXDOP {value}";
			}

			[Sql.Expression("MAXRECURSION {0}")]
			public static string MaxRecursion(int value)
			{
				return $"MAXRECURSION {value}";
			}
		}
	}
}
