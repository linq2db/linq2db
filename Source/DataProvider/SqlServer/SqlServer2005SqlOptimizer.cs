using System;

namespace LinqToDB.DataProvider.SqlServer
{
	using SqlQuery;

	class SqlServer2005SqlOptimizer : SqlServerSqlOptimizer
	{
		public override ISqlExpression ConvertExpression(ISqlExpression expr)
		{
			expr = base.ConvertExpression(expr);

			if (expr is SqlFunction)
				return ConvertConvertFunction((SqlFunction)expr);

			return expr;
		}
	}
}
