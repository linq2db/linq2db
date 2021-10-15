using System;

namespace LinqToDB
{
	public partial class Sql
	{
		public enum QueryExtensionScope
		{
			Default,
			Table,
			Join,
			Query
		}
	}
}
