using System;

namespace LinqToDB.Data.Sql
{
	public interface ISqlExpressionWalkable
	{
		ISqlExpression Walk(bool skipColumns, Func<ISqlExpression,ISqlExpression> func);
	}
}
