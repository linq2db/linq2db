using System;

namespace LinqToDB.SqlQuery
{
	public enum SqlTableType
	{
		Table = 0,
		SystemTable,
		Function,
		Expression,
		Cte
	}
}
