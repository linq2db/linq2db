using System.Collections.Generic;
using System.Linq;

using LinqToDB.SqlQuery;

namespace LinqToDB.DataProvider.DB2
{
	abstract partial class DB2SqlBuilderBase
	{
		protected override bool IsSqlValuesTableValueTypeRequired(SqlValuesTable source,
			IReadOnlyList<ISqlExpression[]>                                      rows, int row, int column)
		{
			/* DB2 doesn't like NULLs without type information
			 : ERROR [42610] [IBM][DB2/NT64] SQL0418N  The statement was not processed because the statement
			 contains an invalid use of one of the following: an untyped parameter marker, the DEFAULT keyword
			 , or a null value.

			See https://stackoverflow.com/questions/13381898

			Unfortunatelly, just use typed parameter doesn't help

			To fix it we need to cast at least one NULL in column if all column values are null.
			We will do it for last row, when we know that there is no non-null values in column and type hint
			needed.

			One thing I don't like is that in some cases DB2 can process query without type hints
			*/
			if (row == -1)
				return true;

			if (row != 0)
				return false;

			// check if column contains NULL in all rows
			return rows.All(r => r[column] is SqlValue value && value.Value == null);
		}
	}
}
