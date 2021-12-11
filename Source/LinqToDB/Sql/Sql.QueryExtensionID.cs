using System;

namespace LinqToDB
{
	public partial class Sql
	{
		public static class QueryExtensionID
		{
			public const int TableHint               = 1000;
			public const int TableHintWithParameter  = 1010;
			public const int TableHintWithParameters = 1011;
			public const int JoinHint                = 1100;
			public const int QueryHint               = 1200;
			public const int QueryHintWithParameter  = 1201;

			// SqlServer table hint IDs.
			//
			public const int SqlServerForceSeekTableHintID = 2001;
		}
	}
}
