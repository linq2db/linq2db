using System;

using LinqToDB.Common;
using LinqToDB.Internal.SqlQuery;

namespace LinqToDB.Internal.Linq.Translation
{
	public interface ISqlExpressionFactory
	{
		DataOptions DataOptions { get; }
		DbDataType  GetDbDataType(ISqlExpression expression);
		DbDataType  GetDbDataType(Type           type);
	}
}
