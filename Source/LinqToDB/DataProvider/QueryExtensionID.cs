using System;

namespace LinqToDB.DataProvider
{
	static class QueryExtensionID
	{
		// SqlServer table hint IDs.
		//
		public const int SqlServerCommonTableHintID    = 1001;
		public const int SqlServerIntValueTableHintID  = 1002;
		public const int SqlServerIndexTableHintID     = 1003;
		public const int SqlServerForceSeekTableHintID = 1004;

		// SqlServer join hint IDs.
		//
		public const int SqlServerJoinHintID           = 1101;
	}
}
