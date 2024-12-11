using System;

using LinqToDB.Common;
using LinqToDB.SqlQuery;

namespace LinqToDB.Linq.Translation
{
	public interface ISqlExpressionFactory
	{
		DataOptions DataOptions { get; }
		DbDataType  GetDbDataType(ISqlExpression expression);
		DbDataType  GetDbDataType(Type           type);
	}
}
