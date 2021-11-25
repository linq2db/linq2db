using System;

namespace LinqToDB
{
	public partial class Sql
	{
		public enum QueryExtensionScope
		{
			Table,
			Join,
			Query,
			TablesInScope
		}
	}
}
