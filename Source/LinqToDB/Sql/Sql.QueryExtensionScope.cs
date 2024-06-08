using System;

namespace LinqToDB
{
	public partial class Sql
	{
		public enum QueryExtensionScope
		{
			None,
			TableHint,
			TablesInScopeHint,
			IndexHint,
			JoinHint,
			SubQueryHint,
			QueryHint,
			TableNameHint,
		}
	}
}
