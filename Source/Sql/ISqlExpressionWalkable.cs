using System;

namespace LinqToDB.Sql
{
	public interface ISqlExpressionWalkable
	{
		ISqlExpression Walk(bool skipColumns, Func<ISqlExpression,ISqlExpression> func);
	}
}
