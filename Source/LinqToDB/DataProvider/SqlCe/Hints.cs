using System;

namespace LinqToDB.DataProvider.SqlCe
{
	public static class Hints
	{
		public static class TableHint
		{
			public const string HoldLock = "HoldLock";
			public const string NoLock   = "NoLock";
			public const string PagLock  = "PagLock";
			public const string RowLock  = "RowLock";
			public const string TabLock  = "TabLock";
			public const string UpdLock  = "UpdLock";
			public const string XLock    = "XLock";

			[Sql.Function]
			public static string Index(string value)
			{
				return "Index(" + value + ")";
			}
		}
	}
}
