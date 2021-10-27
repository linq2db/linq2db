using System;

namespace LinqToDB
{
	public partial class Sql
	{
		public static class QueryExtensionID
		{
			public const int TableHint = 1000;
			public const int JoinHint  = 1001;
			public const int QueryHint = 1002;

			// SqlServer table hint IDs.
			//
			public const int SqlServerForceSeekTableHintID = 2001;
		}
	}
}
