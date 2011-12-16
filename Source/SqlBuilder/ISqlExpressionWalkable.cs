using System;

namespace LinqToDB.SqlBuilder
{
	public interface ISqlExpressionWalkable
	{
		ISqlExpression Walk(bool skipColumns, Func<ISqlExpression,ISqlExpression> func);
	}
}
