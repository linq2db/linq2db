using System;

namespace LinqToDB
{
	public partial class Sql
	{
		public static class QueryExtensionID
		{
			public const int None               = 0;
			public const int Hint               = 1000;
			public const int HintWithParameter  = 1001;
			public const int HintWithParameters = 1002;
		}
	}
}
