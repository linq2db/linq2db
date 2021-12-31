using System;

namespace LinqToDB
{
	public partial class Sql
	{
		public enum QueryExtensionScope
		{
			None,
			TableHint,
			IndexHint,
			JoinHint,
			QueryHint,
			TablesInScopeHint
		}
	}
}
