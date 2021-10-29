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
			public const string LoopJoin  = "LOOP JOIN";
			public const string HashJoin  = "HASH JOIN";
			public const string MergeJoin = "MERGE JOIN";
		}
	}
}
