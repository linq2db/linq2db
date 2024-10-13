using System;

using LinqToDB.Common;
using LinqToDB.Internal.SqlQuery;

namespace LinqToDB.Internal.Linq.Translation
{
	// Empty, but should be extended
	public interface ISqlExpressionFactory
	{
		DataOptions DataOptions { get; }
		DbDataType  GetDbDataType(ISqlExpression expression);
		DbDataType  GetDbDataType(Type           type);
	}
}
